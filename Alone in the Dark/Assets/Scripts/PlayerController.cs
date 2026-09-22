using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// The player ball and, because it always was, the game flow: title, intro, chapters, endless, pause, game over.
/// Field names below are referenced from GameScene.unity and must not be renamed without re-wiring the scene.
///
/// Flow rules, borrowed from the short-session games that get this right:
/// no tutorial screens, no "press start" gate, one line of story at a time over a live world,
/// and a retry that is one tap away and rebuilds instantly without reloading the scene.
/// </summary>
public class PlayerController : MonoBehaviour
{
    enum State
    {
        Title,
        Intro,
        Ready,
        Playing,
        Paused,
        GameOver,
        Won,
    }

    public float speed = 6.0f;
    public Text scoreText;
    public GameObject restart, exitButton, EndTextObject, SubTextObject, SecretPassage, dustParticles;
    public GameObject[] levels;
    public Light mainLight;
    public bool gravitationOn;

    const float GravityForce = 1.5f;
    const float VelocityCap = 2f;
    const float BrakeFactor = 0.8f;
    const float ScoreStep = 3f;
    const float DeathLockout = 0.45f;

    static readonly Color StoryColor = new Color(0.93f, 0.91f, 0.84f);
    static readonly Color HintColor = new Color(0.55f, 0.55f, 0.55f);
    static readonly Color DangerColor = new Color(0.9f, 0.18f, 0.18f);

    State state;
    GameMode mode;
    int currentLevel;
    int seed;
    int runScore;
    int levelScore;
    float gravity;
    float stateEnteredAt;
    Vector2 moveInput;
    Vector3 lastScorePosition;

    /// <summary>
    /// Every wind zone the ball is currently inside. Zones can overlap (a pickup sitting in a
    /// wind field splits it into two boxes), so leaving one must not cancel the others.
    /// </summary>
    readonly List<Collider> activeWinds = new List<Collider>();

    Rigidbody rb;
    ParticleSystem particles;
    ParticleSystem.EmissionModule emission;
    Light lightHalo;
    GameAudio sfx;

    Text mainText;
    Text subText;
    Text versionText;
    Button playButton;
    Button altButton;
    Button endlessButton;
    Button dailyButton;
    Button pauseButton;
    Button soundButton;

    LevelBuilder.Built generated;
    EndlessCave endless;
    TextAsset[] asciiLevels = new TextAsset[0];
    Coroutine introRoutine;
    Coroutine lineRoutine;

    /// <summary>Scene objects a run consumed. Hidden rather than destroyed so a retry can put them back.</summary>
    readonly List<GameObject> consumed = new List<GameObject>();

    int TotalStoryLevels => levels.Length + asciiLevels.Length;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        particles = GetComponent<ParticleSystem>();
        lightHalo = GetComponent<Light>();
        emission = particles.emission;
        sfx = GameAudio.Get();

        mainText = EndTextObject.GetComponent<Text>();
        subText = SubTextObject.GetComponent<Text>();

        asciiLevels = Resources.LoadAll<TextAsset>("Levels");
        System.Array.Sort(asciiLevels, (a, b) => string.CompareOrdinal(a.name, b.name));

        BuildUi();

        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraFit>() == null)
        {
            cam.gameObject.AddComponent<CameraFit>();
        }
    }

    void Start()
    {
        foreach (var level in levels)
        {
            level.SetActive(false);
        }

        if (SecretPassage != null)
        {
            SecretPassage.SetActive(false);
        }

        ShowTitle();
    }

    // ----- Update loop -----

    void Update()
    {
        Breathe();

        switch (state)
        {
            case State.Title:
                if (GameInput.Back())
                {
                    Quit();
                }

                break;

            case State.Intro:
                if (GameInput.TapDown() || GameInput.Back())
                {
                    SkipIntro();
                }

                break;

            case State.Ready:
                // No gate to read past: the first touch is both "start" and the first steer.
                if (GameInput.Move().sqrMagnitude > 0f || GameInput.TapDown())
                {
                    BeginPlay();
                }

                if (GameInput.Back())
                {
                    GoToMenu();
                }

                break;

            case State.Playing:
                moveInput = GameInput.Move();
                TrackScore();
                if (mode != GameMode.Story)
                {
                    gravity = EndlessCave.GravityAt(transform.position.x);
                    gravitationOn = gravity > 0f;
                }

                if (GameInput.Back())
                {
                    Pause();
                }

                break;

            case State.Paused:
                if (GameInput.Back())
                {
                    Resume();
                }

                break;

            case State.GameOver:
                // A tap anywhere retries. The lockout stops the fatal tap from skipping the score.
                if (Time.unscaledTime - stateEnteredAt > DeathLockout && GameInput.TapDown())
                {
                    Retry();
                }

                break;

            case State.Won:
                break;
        }

        DebugKeys();
    }

    void FixedUpdate()
    {
        if (state != State.Playing)
        {
            return;
        }

        var move = moveInput * speed;
        var velocity = rb.velocity;

        // Cap how fast input can push you, but never take away the ability to steer back.
        // The original killed the whole axis once you were over the cap, which meant wind or a
        // long fall left you a passenger.
        if (velocity.y > VelocityCap && move.y > 0f) move.y = 0f;
        if (velocity.y < -VelocityCap && move.y < 0f) move.y = 0f;
        if (velocity.x > VelocityCap && move.x > 0f) move.x = 0f;
        if (velocity.x < -VelocityCap && move.x < 0f) move.x = 0f;

        rb.AddForce(new Vector3(move.x, move.y, 0f), ForceMode.Acceleration);
        rb.AddForce(-velocity * BrakeFactor, ForceMode.Acceleration);
        var wind = WindForce();
        rb.AddForce(new Vector3(wind.x, wind.y, 0f), ForceMode.Acceleration);
        if (mode != GameMode.Story)
        {
            // Endless caves drift you forward, harder the deeper you get. Sitting still is not a strategy.
            rb.AddForce(new Vector3(EndlessCave.DriftAt(transform.position.x), 0f, 0f), ForceMode.Acceleration);
        }

        if (gravity > 0f)
        {
            rb.AddForce(new Vector3(0f, -gravity, 0f) * rb.mass);
        }

        // Stay on the play plane whatever the physics engine dreams up.
        var p = transform.position;
        if (Mathf.Abs(p.z) > 0.001f)
        {
            p.z = 0f;
            transform.position = p;
        }
    }

    /// <summary>The halo pulse. Unscaled so it keeps breathing while paused, and frame-rate independent.</summary>
    void Breathe()
    {
        if (state == State.Intro)
        {
            return;
        }

        float intensity = 1f + 0.2f * Mathf.Sin(Time.unscaledTime * 3.4f);
        lightHalo.intensity = intensity;
        mainLight.intensity = intensity;
    }

    void TrackScore()
    {
        if ((transform.position - lastScorePosition).magnitude > ScoreStep)
        {
            lastScorePosition = transform.position;
            levelScore += 1;
            UpdateScoreText();
        }
    }

    void UpdateScoreText()
    {
        scoreText.text = (runScore + levelScore).ToString();
    }

    // ----- Title -----

    void ShowTitle()
    {
        StopLine();
        Enter(State.Title);
        Time.timeScale = 1f;
        mode = GameMode.None;
        ClearLevel();
        ParkPlayer();
        LightsOn();
        sfx.StartAmbient();

        int progress = GameSave.StoryLevel;
        bool canContinue = progress > 0 && progress < TotalStoryLevels;

        scoreText.text = "";
        versionText.gameObject.SetActive(true);
        ShowText(Application.productName, StoryColor, BestLine(), HintColor);

        ShowButton(playButton, canContinue ? "Continue" : "Play", () => StartStory(canContinue ? progress : 0));
        if (canContinue)
        {
            ShowButton(altButton, "Chapter 1", () => StartStory(0));
        }
        else
        {
            altButton.gameObject.SetActive(false);
        }

        ShowButton(endlessButton, "Endless", () => StartMode(GameMode.Endless, Random.Range(1, int.MaxValue)));
        ShowButton(dailyButton, "Daily", () => StartMode(GameMode.Daily, int.Parse(GameSave.DailyId)));
        pauseButton.gameObject.SetActive(false);
        ShowButton(soundButton, SoundLabel(), ToggleSound);
    }

    string BestLine()
    {
        int story = GameSave.Best(GameMode.Story);
        int endlessBest = GameSave.Best(GameMode.Endless);
        int daily = GameSave.Best(GameMode.Daily);
        if (story + endlessBest + daily == 0)
        {
            return "";
        }

        return "best   " + story + "   ·   " + endlessBest + "   ·   " + daily;
    }

    string SoundLabel()
    {
        return sfx.Muted ? "sound off" : "sound on";
    }

    void ToggleSound()
    {
        sfx.Muted = !sfx.Muted;
        SetLabel(soundButton, SoundLabel());
        sfx.Play("tap");
    }

    void Quit()
    {
#if !UNITY_IOS && !UNITY_WEBGL
        Application.Quit();
#endif
    }

    void StartStory(int level)
    {
        sfx.Play("tap");
        mode = GameMode.Story;
        runScore = 0;
        if (level == 0)
        {
            GameSave.StoryLevel = 0;
        }

        if (level == 0 && !GameSave.IntroSeen)
        {
            introRoutine = StartCoroutine(PlayIntro());
        }
        else
        {
            StartLevel(level);
        }
    }

    void StartMode(GameMode newMode, int caveSeed)
    {
        sfx.Play("tap");
        mode = newMode;
        seed = caveSeed;
        runScore = 0;
        StartLevel(0);
    }

    // ----- Intro: three lines, about twelve seconds, skippable, shown once ever -----

    IEnumerator PlayIntro()
    {
        Enter(State.Intro);
        StopLine();
        HideButtons();
        versionText.gameObject.SetActive(false);
        scoreText.text = "";
        ClearLevel();
        ParkPlayer();
        lightHalo.intensity = 0f;
        mainLight.intensity = 0f;
        emission.rateOverTime = 0f;
        dustParticles.SetActive(false);

        float[] lightSteps = { 0.12f, 0.3f, 0.55f };
        int[] bursts = { 15, 80, 400 };

        for (int i = 0; i < Story.Intro.Length; i++)
        {
            var parts = Split(Story.Intro[i]);
            ShowText(parts[0], HintColor, parts[1], HintColor);
            SetAlpha(1f);

            int step = Mathf.Min(i, lightSteps.Length - 1);
            particles.Emit(bursts[step]);
            sfx.Play("pickup", 0.4f, 0.7f + 0.25f * step);
            StartCoroutine(RampLight(lightSteps[step], 1.2f));

            yield return FadeText(1f, 0f, 2.6f);
            HideText();
            yield return new WaitForSecondsRealtime(0.45f);
        }

        particles.Emit(1000);
        emission.rateOverTime = 30f;
        sfx.Play("level");
        yield return RampLight(1f, 1.2f);

        GameSave.IntroSeen = true;
        introRoutine = null;
        StartLevel(0);
    }

    void SkipIntro()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        GameSave.IntroSeen = true;
        StartLevel(0);
    }

    IEnumerator RampLight(float target, float seconds)
    {
        float from = lightHalo.intensity;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float v = Mathf.Lerp(from, target, t / seconds);
            lightHalo.intensity = v;
            mainLight.intensity = v;
            yield return null;
        }

        lightHalo.intensity = target;
        mainLight.intensity = target;
    }

    // ----- Levels -----

    void StartLevel(int level)
    {
        currentLevel = level;
        levelScore = 0;
        activeWinds.Clear();
        ClearLevel();
        RestoreConsumed();
        ParkPlayer();
        LightsOn();
        sfx.StartAmbient();

        if (mode == GameMode.Story)
        {
            if (level >= TotalStoryLevels)
            {
                Win();
                return;
            }

            GameSave.StoryLevel = Mathf.Max(GameSave.StoryLevel, level);
            if (level < levels.Length)
            {
                levels[level].SetActive(true);
                gravity = level > 0 ? GravityForce : 0f;
            }
            else
            {
                generated = LevelBuilder.BuildAscii(asciiLevels[level - levels.Length].text);
                gravity = generated.gravity;
            }
        }
        else
        {
            var cave = new GameObject("EndlessCave").AddComponent<EndlessCave>();
            cave.Init(transform, seed);
            endless = cave;
            gravity = 0f;
        }

        gravitationOn = gravity > 0f;

        Enter(State.Ready);
        Time.timeScale = 1f;
        HideButtons();
        versionText.gameObject.SetActive(false);
        UpdateScoreText();
        ShowOpeningLine(level);
    }

    /// <summary>
    /// One line, faded in over the live world, gone by itself. Never blocks the start of play.
    /// The only instruction the game ever gives is on the very first run.
    /// </summary>
    void ShowOpeningLine(int level)
    {
        string line;
        if (mode == GameMode.Story)
        {
            line = Story.ChapterLine(level).Replace("|", "\n");
        }
        else if (mode == GameMode.Daily)
        {
            int best = GameSave.Best(GameMode.Daily);
            line = "Today's cave\n" + (best > 0 ? "best today " + best : "one cave, one day");
        }
        else
        {
            int best = GameSave.Best(GameMode.Endless);
            line = "Endless\n" + (best > 0 ? "best " + best : "how far does a light go?");
        }

        if (GameSave.Runs == 0)
        {
            line += "\n\ndrag anywhere";
        }

        if (lineRoutine != null)
        {
            StopCoroutine(lineRoutine);
        }

        lineRoutine = StartCoroutine(FadeLine(line));
    }

    IEnumerator FadeLine(string line)
    {
        int split = line.IndexOf('\n');
        string head = split < 0 ? line : line.Substring(0, split);
        string tail = split < 0 ? "" : line.Substring(split + 1).TrimStart('\n');
        ShowText(head, StoryColor, tail, HintColor);
        SetAlpha(0f);
        yield return FadeText(0f, 1f, 0.6f);
        yield return new WaitForSecondsRealtime(state == State.Ready ? 1.6f : 1.2f);
        yield return FadeText(1f, 0f, 0.9f);
        HideText();
        SetAlpha(1f);
        lineRoutine = null;
    }

    void BeginPlay()
    {
        Enter(State.Playing);
        Time.timeScale = 1f;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        lastScorePosition = transform.position;
        GameSave.Runs = GameSave.Runs + 1;

        HideButtons();
        ShowButton(pauseButton, "II", Pause);
        sfx.Play("start", 0.6f);
    }

    void NextLevel()
    {
        sfx.Play("level");
        runScore += levelScore;
        levelScore = 0;

        int next = currentLevel + 1;
        if (next >= TotalStoryLevels)
        {
            Win();
        }
        else
        {
            StartLevel(next);
        }
    }

    void ClearLevel()
    {
        foreach (var level in levels)
        {
            level.SetActive(false);
        }

        if (SecretPassage != null)
        {
            SecretPassage.SetActive(false);
        }

        if (generated != null)
        {
            Destroy(generated.root);
            generated = null;
        }

        if (endless != null)
        {
            Destroy(endless.gameObject);
            endless = null;
        }
    }

    void RestoreConsumed()
    {
        foreach (var go in consumed)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        consumed.Clear();
    }

    /// <summary>Hide a pickup instead of destroying it, so an instant retry has it back.</summary>
    void Consume(GameObject go)
    {
        go.SetActive(false);
        consumed.Add(go);
    }

    void ParkPlayer()
    {
        GameInput.ResetDrag();
        moveInput = Vector2.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.position = Vector3.zero;
        lastScorePosition = Vector3.zero;
    }

    void LightsOn()
    {
        lightHalo.intensity = 1f;
        mainLight.intensity = 1f;
        emission.rateOverTime = 30f;
        dustParticles.SetActive(true);
    }

    // ----- Pause, death, win -----

    void Pause()
    {
        if (state != State.Playing)
        {
            return;
        }

        StopLine();
        Enter(State.Paused);
        Time.timeScale = 0f;
        GameInput.ResetDrag();
        sfx.Play("tap");
        ShowText("Paused", StoryColor, "", HintColor);
        ShowButton(playButton, "Resume", Resume);
        ShowButton(altButton, "Menu", GoToMenu);
        pauseButton.gameObject.SetActive(false);
        ShowButton(soundButton, SoundLabel(), ToggleSound);
    }

    void Resume()
    {
        if (state != State.Paused)
        {
            return;
        }

        Enter(State.Playing);
        Time.timeScale = 1f;
        HideText();
        HideButtons();
        ShowButton(pauseButton, "II", Pause);
    }

    void Die()
    {
        if (state != State.Playing)
        {
            return;
        }

        StopLine();
        Enter(State.GameOver);
        Time.timeScale = 0f;
        GameInput.ResetDrag();
        sfx.Play("crash");
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        int total = runScore + levelScore;
        bool record = GameSave.SubmitScore(mode, total);
        string taunt = record ? "New best" : Story.Deaths[Random.Range(0, Story.Deaths.Length)];

        ShowText(total.ToString(), record ? StoryColor : DangerColor, taunt + "\n\ntap to try again", HintColor);
        ShowButton(altButton, "Menu", GoToMenu);
        playButton.gameObject.SetActive(false);
        pauseButton.gameObject.SetActive(false);
    }

    void Win()
    {
        StopLine();
        Enter(State.Won);
        Time.timeScale = 1f;
        ClearLevel();
        ParkPlayer();
        sfx.Play("win");

        int total = runScore;
        bool record = GameSave.SubmitScore(GameMode.Story, total);
        GameSave.StoryLevel = 0;

        var parts = Split(Story.Ending);
        ShowText(parts[0], StoryColor, parts[1] + "\n\n" + total + (record ? "   ·   new best" : ""), HintColor);
        ShowButton(playButton, "Endless", () => StartMode(GameMode.Endless, Random.Range(1, int.MaxValue)));
        ShowButton(altButton, "Menu", GoToMenu);
        pauseButton.gameObject.SetActive(false);
    }

    /// <summary>Referenced by name from GameScene.unity. Kept so the scene stays resolvable.</summary>
    public void Restart()
    {
        Retry();
    }

    /// <summary>Referenced by name from GameScene.unity. Kept so the scene stays resolvable.</summary>
    public void exitGame()
    {
        Quit();
    }

    /// <summary>One tap, no scene reload. Should feel like it never left.</summary>
    void Retry()
    {
        sfx.Play("tap");
        if (mode == GameMode.Endless)
        {
            seed = Random.Range(1, int.MaxValue);
        }

        StartLevel(currentLevel);
    }

    void GoToMenu()
    {
        sfx.Play("tap");
        ShowTitle();
    }

    // ----- Collisions -----

    void OnCollisionEnter(Collision collision)
    {
        Die();
    }

    void OnTriggerEnter(Collider other)
    {
        if (state != State.Playing)
        {
            return;
        }

        if (other.CompareTag(LevelBuilder.TagTarget) || other.CompareTag(LevelBuilder.TagFinish))
        {
            if (mode == GameMode.Story)
            {
                NextLevel();
            }

            return;
        }

        if (other.CompareTag("SecretPassage"))
        {
            if (SecretPassage != null)
            {
                SecretPassage.SetActive(true);
                foreach (var t in SecretPassage.GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag(LevelBuilder.TagBonus))
                    {
                        LevelBuilder.EnsurePickupCollider(t.gameObject);
                        if (t.GetComponent<Spinner>() == null)
                        {
                            t.gameObject.AddComponent<Spinner>();
                        }
                    }
                }
            }

            sfx.Play("secret");
            Consume(other.gameObject);
            return;
        }

        if (other.CompareTag(LevelBuilder.TagBonus))
        {
            levelScore += 100;
            UpdateScoreText();
            sfx.Play("pickup");
            Consume(other.gameObject);
            return;
        }

        if (IsWind(other) && !activeWinds.Contains(other))
        {
            activeWinds.Add(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        activeWinds.Remove(other);
    }

    static bool IsWind(Collider other)
    {
        return other.GetComponent<CaveWind>() != null
            || other.CompareTag("WindRight")
            || other.CompareTag("WindLeft");
    }

    /// <summary>Sum of every wind zone the ball is in. Zones are cleaned up if their level was destroyed.</summary>
    Vector2 WindForce()
    {
        var total = Vector2.zero;
        for (int i = activeWinds.Count - 1; i >= 0; i--)
        {
            var col = activeWinds[i];
            if (col == null || !col.gameObject.activeInHierarchy)
            {
                activeWinds.RemoveAt(i);
                continue;
            }

            var zone = col.GetComponent<CaveWind>();
            if (zone != null)
            {
                total += zone.force;
            }
            else if (col.CompareTag("WindRight"))
            {
                total += new Vector2(1.5f, 0f);
            }
            else if (col.CompareTag("WindLeft"))
            {
                total += new Vector2(-1.5f, 0f);
            }
        }

        return total;
    }

    // ----- UI -----

    /// <summary>
    /// Lays the existing scene widgets out for a portrait phone and clones the few extra buttons,
    /// so the whole menu is one screen with nothing to navigate.
    /// </summary>
    void BuildUi()
    {
        playButton = restart.GetComponent<Button>();
        altButton = exitButton.GetComponent<Button>();
        DisableSceneListeners(playButton);
        DisableSceneListeners(altButton);

        Place(playButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -360f), new Vector2(520f, 150f));
        Place(altButton, new Vector2(0.5f, 0.5f), new Vector2(0f, -540f), new Vector2(520f, 110f));

        endlessButton = Clone(altButton, "Endless");
        dailyButton = Clone(altButton, "Daily");
        Place(endlessButton, new Vector2(0.5f, 0f), new Vector2(-145f, 320f), new Vector2(270f, 110f));
        Place(dailyButton, new Vector2(0.5f, 0f), new Vector2(145f, 320f), new Vector2(270f, 110f));

        pauseButton = Clone(altButton, "Pause");
        Place(pauseButton, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(140f, 140f));

        soundButton = Clone(altButton, "Sound");
        Place(soundButton, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(300f, 100f));

        versionText = Instantiate(scoreText, scoreText.transform.parent);
        versionText.name = "Version";
        var version = versionText.GetComponent<RectTransform>();
        version.anchorMin = version.anchorMax = new Vector2(1f, 0f);
        version.pivot = new Vector2(1f, 0f);
        version.anchoredPosition = new Vector2(-40f, 40f);
        version.sizeDelta = new Vector2(400f, 60f);
        versionText.fontSize = 30;
        versionText.resizeTextForBestFit = false;
        versionText.alignment = TextAnchor.LowerRight;
        versionText.color = HintColor;
        versionText.text = "v" + Application.version + " beta";

        var main = EndTextObject.GetComponent<RectTransform>();
        main.anchorMin = new Vector2(0f, 0.5f);
        main.anchorMax = new Vector2(1f, 0.5f);
        main.pivot = new Vector2(0.5f, 0.5f);
        main.anchoredPosition = new Vector2(0f, 300f);
        main.sizeDelta = new Vector2(-80f, 400f);
        mainText.alignment = TextAnchor.MiddleCenter;
        mainText.resizeTextForBestFit = true;
        mainText.resizeTextMinSize = 40;
        mainText.resizeTextMaxSize = 190;

        var sub = SubTextObject.GetComponent<RectTransform>();
        sub.anchorMin = new Vector2(0f, 0.5f);
        sub.anchorMax = new Vector2(1f, 0.5f);
        sub.pivot = new Vector2(0.5f, 1f);
        sub.anchoredPosition = new Vector2(0f, 80f);
        sub.sizeDelta = new Vector2(-140f, 300f);
        subText.alignment = TextAnchor.UpperCenter;
        subText.resizeTextForBestFit = true;
        subText.resizeTextMinSize = 24;
        subText.resizeTextMaxSize = 56;

        var score = scoreText.GetComponent<RectTransform>();
        score.anchorMin = score.anchorMax = new Vector2(0.5f, 1f);
        score.pivot = new Vector2(0.5f, 1f);
        score.anchoredPosition = new Vector2(0f, -50f);
        score.sizeDelta = new Vector2(600f, 110f);
        scoreText.alignment = TextAnchor.UpperCenter;
        scoreText.fontSize = 80;
    }

    static void Place(Button button, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(Pivot(anchor.x), Pivot(anchor.y));
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    /// <summary>An edge-anchored widget pivots on that edge, so it stays fully on screen.</summary>
    static float Pivot(float anchor)
    {
        if (anchor <= 0f)
        {
            return 0f;
        }

        return anchor >= 1f ? 1f : 0.5f;
    }

    Button Clone(Button template, string name)
    {
        var go = Instantiate(template.gameObject, template.transform.parent);
        go.name = name;
        var button = go.GetComponent<Button>();
        DisableSceneListeners(button);
        return button;
    }

    static void DisableSceneListeners(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
    }

    void Enter(State next)
    {
        state = next;
        stateEnteredAt = Time.unscaledTime;
    }

    void ShowText(string main, Color mainColor, string sub, Color subColor)
    {
        EndTextObject.SetActive(true);
        SubTextObject.SetActive(true);
        mainText.text = main;
        mainText.color = mainColor;
        subText.text = sub;
        subText.color = subColor;
        SetAlpha(1f);
    }

    /// <summary>Cancels a story line that is still fading, before showing something else.</summary>
    void StopLine()
    {
        if (lineRoutine != null)
        {
            StopCoroutine(lineRoutine);
            lineRoutine = null;
        }
    }

    void HideText()
    {
        EndTextObject.SetActive(false);
        SubTextObject.SetActive(false);
    }

    IEnumerator FadeText(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }

        SetAlpha(to);
    }

    void SetAlpha(float alpha)
    {
        var c = mainText.color;
        c.a = alpha;
        mainText.color = c;
        c = subText.color;
        c.a = alpha;
        subText.color = c;
    }

    void ShowButton(Button button, string label, UnityAction action)
    {
        button.gameObject.SetActive(true);
        SetLabel(button, label);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    static void SetLabel(Button button, string label)
    {
        var text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 48;
        }
    }

    void HideButtons()
    {
        playButton.gameObject.SetActive(false);
        altButton.gameObject.SetActive(false);
        endlessButton.gameObject.SetActive(false);
        dailyButton.gameObject.SetActive(false);
        pauseButton.gameObject.SetActive(false);
        soundButton.gameObject.SetActive(false);
    }

    static string[] Split(string line)
    {
        int idx = line.IndexOf('|');
        if (idx < 0)
        {
            return new[] { line, "" };
        }

        return new[] { line.Substring(0, idx), line.Substring(idx + 1) };
    }

    // ----- Editor and development-build shortcuts, compiled out of a release build -----

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    void DebugKeys()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            gravity = gravity > 0f ? 0f : GravityForce;
            gravitationOn = gravity > 0f;
        }

        if (Input.GetKeyDown(KeyCode.N) && mode == GameMode.Story && (state == State.Playing || state == State.Ready))
        {
            NextLevel();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("screenshot.png");
        }

        for (int i = 0; i < 9 && i < TotalStoryLevels; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && mode == GameMode.Story && (state == State.Playing || state == State.Ready))
            {
                StartLevel(i);
            }
        }
    }
}
