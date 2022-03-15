using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float speed = 6.0f;
    public Text scoreText;
    public GameObject restart, exitButton, EndTextObject, SubTextObject, SecretPassage, dustParticles;
    public GameObject[] levels;
    public Light mainLight;
    public bool gravitationOn;

    private Rigidbody rb;
    private Vector3 moveDirection, tempPosition = Vector3.zero;
    private float gravitationalForce = -1.5f;
    private int bonusScore, score, counter = 0;
    private bool hasWaited, waiting, gameOver, pause, startScene = false;
    private float tempScore, timeCount, lightBreathingParam;
    private int currentLevel;
    private float windForce;
    private ParticleSystem particles;
    private Light lightHalo;
    private ParticleSystem.EmissionModule emission;
    private Coroutine coMain, coSub;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        particles = GetComponent<ParticleSystem>();
        lightHalo = GetComponent<Light>();
        startScene = true;

        //preparations for startscene
        foreach (var levelObject in levels)
        {
            levelObject.SetActive(false);
        }

        levels[0].SetActive(true);
        restart.SetActive(false);
        exitButton.SetActive(false);
        SubTextObject.SetActive(false);

        emission = particles.emission;
        lightHalo.intensity = 0f; //1 final
        mainLight.intensity = 0f; //1 final
        emission.rateOverTime = 0; //30 final
        dustParticles.SetActive(false);
        scoreText.text = "";
        EndTextObject.SetActive(true);
        SubTextObject.SetActive(true);
        EndTextObject.GetComponent<Text>().color = Color.grey;
        SubTextObject.GetComponent<Text>().color = Color.grey;
        EndTextObject.GetComponent<Text>().text = "At the beginning of time,";
        SubTextObject.GetComponent<Text>().text = "there was nothing";
        coMain = StartCoroutine(DimText(EndTextObject.GetComponent<Text>()));
        coSub = StartCoroutine(DimText(SubTextObject.GetComponent<Text>()));
    }


    void Update()
    {
        if (startScene)
        {
            timeCount += Time.deltaTime;
            var mainText = EndTextObject.GetComponent<Text>();
            if (timeCount > 3)
            {
                switch (counter)
                {
                    case 0:
                        SubTextObject.SetActive(false);
                        EndTextObject.SetActive(false);
                        break;
                    case 1:
                        particles.Emit(20);
                        lightHalo.intensity = 0.15f;
                        mainLight.intensity = 0.15f;
                        break;
                    case 2:
                        EndTextObject.SetActive(true);
                        coMain = StartCoroutine(DimText(mainText));
                        mainText.text = "But out of the darkness";
                        break;
                    case 3:
                        EndTextObject.SetActive(false);
                        break;
                    case 4:
                        particles.Emit(100);
                        lightHalo.intensity = 0.3f;
                        mainLight.intensity = 0.3f;
                        break;
                    case 5:
                        EndTextObject.SetActive(true);
                        coMain = StartCoroutine(DimText(mainText));
                        mainText.text = "Suddenly a wild light appeared!";
                        break;
                    case 6:
                        EndTextObject.SetActive(false);
                        particles.Emit(1000);
                        lightHalo.intensity = 0.4f;
                        mainLight.intensity = 0.4f;
                        emission.rateOverTime = 30;
                        break;
                    case 8:
                        LevelChange(0);
                        break;
                }

                counter++;
                timeCount = 0;
            }

            if (lightHalo.intensity >= 0.4f && lightHalo.intensity <= 1f)
            {
                mainLight.intensity += 0.005f;
                lightHalo.intensity += 0.005f;
            }
            else if (lightHalo.intensity < 0.4f)
            {
                mainLight.intensity -= 0.005f;
                lightHalo.intensity -= 0.005f;
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopCoroutine(coMain);
                StopCoroutine(coSub);
                LevelChange(0);
            }
        }
        else
        {
            //breathing effect
            if (lightHalo.intensity > 1.2f)
            {
                lightBreathingParam = -0.01f;
            }
            else if (lightHalo.intensity < 0.8f)
            {
                lightBreathingParam = 0.01f;
            }

            lightHalo.intensity += lightBreathingParam;


            moveDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
            moveDirection *= speed;
            Cursor.visible = waiting || pause || gameOver;

            if (Mathf.Abs((transform.position - tempPosition).magnitude) > 3f)
            {
                tempPosition = transform.position;
                score += 1;
            }

            scoreText.text = "Score: " + (score + bonusScore);

            if (Input.GetKeyDown(KeyCode.G))
            {
                gravitationOn = !gravitationOn;
            }

            if (waiting)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    LevelChange(0);
                }

                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    LevelChange(1);
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    LevelChange(2);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    waiting = false;
                    EndTextObject.SetActive(false);
                    SubTextObject.SetActive(false);
                    Time.timeScale = 1;
                }
            }

            if (gameOver && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                Restart();
            }
        }

        if (!gameOver && !waiting && Input.GetKeyDown(KeyCode.Escape))
        {
            if (pause)
            {
                Time.timeScale = 1;
                EndTextObject.SetActive(false);
                exitButton.SetActive(false);
                pause = false;
            }
            else
            {
                Time.timeScale = 0;
                EndTextObject.SetActive(true);
                EndTextObject.GetComponent<UnityEngine.UI.Text>().text = "Pause";
                exitButton.SetActive(true);
                pause = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("screenshot.png");
        }
    }

    IEnumerator DimText(Text textToDim)
    {
        Color c = textToDim.color;
        for (float alpha = 1f; alpha >= 0; alpha -= 1 / (3 / Time.deltaTime))
        {
            c.a = alpha;
            textToDim.color = c;
            yield return null;
        }
    }

    void FixedUpdate()
    {
        if (Mathf.Abs(rb.velocity.y) > 2)
        {
            moveDirection.y = 0;
        }

        if (Mathf.Abs(rb.velocity.x) > 2)
        {
            moveDirection.x = 0;
        }

        rb.AddForce(moveDirection, ForceMode.Acceleration);
        rb.AddForce(-rb.velocity * 0.8f, ForceMode.Acceleration); //breaking force
        rb.AddForce(new Vector3(windForce, 0f, 0f), ForceMode.Acceleration);

        if (gravitationOn)
        {
            rb.AddForce(new Vector3(0f, gravitationalForce, 0f) * rb.mass);
        }
    }

    public void exitGame()
    {
        Application.Quit();
    }

    public void Restart()
    {
        gameOver = false;
        LevelChange(currentLevel);
    }

    void OnCollisionEnter(Collision collision)
    {
        EndGame("Game Over!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish"))
        {
            EndGame("You have Won!");
        }

        if (other.CompareTag("Target"))
        {
            SecretPassage.SetActive(false);
            LevelChange(currentLevel + 1);
        }

        if (other.CompareTag("SecretPassage"))
        {
            SecretPassage.SetActive(true);
            Destroy(other.gameObject);
        }

        if (other.CompareTag("BonusPoints"))
        {
            bonusScore = 100;
            Destroy(other.gameObject);
        }

        if (other.CompareTag("WindRight"))
        {
            windForce = 1.5f;
        }

        if (other.CompareTag("WindLeft"))
        {
            windForce = -1.5f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WindRight") || other.CompareTag("WindRight"))
        {
            windForce = 0;
        }
    }

    private void EndGame(string text)
    {
        Time.timeScale = 0;
        EndTextObject.SetActive(true);
        restart.SetActive(true);
        EndTextObject.GetComponent<UnityEngine.UI.Text>().text = text;
        gameOver = true;
    }

    private void LevelChange(int level)
    {
        foreach (var levelObject in levels)
        {
            levelObject.SetActive(false);
        }

        levels[level].SetActive(true);
        restart.SetActive(false);
        exitButton.SetActive(false);
        
        startScene = false;
        lightHalo.intensity = 1f;
        mainLight.intensity = 1f;
        EndTextObject.GetComponent<Text>().color = Color.red;
        SubTextObject.GetComponent<Text>().color = Color.grey;
        dustParticles.SetActive(true);
        emission.rateOverTime = 30;

        gravitationOn = level > 0;

        Time.timeScale = 0;
        currentLevel = level;
        score = 0;
        windForce = 0;
        transform.position = Vector3.zero;
        tempPosition = transform.position;
        rb.velocity = Vector3.zero;

        EndTextObject.SetActive(true);
        SubTextObject.SetActive(true);
        EndTextObject.GetComponent<UnityEngine.UI.Text>().text = "Level " + (level + 1);
        SubTextObject.GetComponent<UnityEngine.UI.Text>().text = "Press Space to continue";
        waiting = true;
    }
}