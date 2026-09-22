using System;
using UnityEngine;

/// <summary>Everything that survives closing the app. Thin wrapper over PlayerPrefs.</summary>
public static class GameSave
{
    const string KeyIntroSeen = "intro_seen";
    const string KeyStoryLevel = "story_level";
    const string KeyBestStory = "best_story";
    const string KeyBestEndless = "best_endless";
    const string KeyBestDaily = "best_daily_";
    const string KeyMuted = "muted";
    const string KeyRuns = "runs";

    public static bool IntroSeen
    {
        get => PlayerPrefs.GetInt(KeyIntroSeen, 0) == 1;
        set => Set(KeyIntroSeen, value ? 1 : 0);
    }

    /// <summary>Highest story level reached (0-based). Continue starts here.</summary>
    public static int StoryLevel
    {
        get => PlayerPrefs.GetInt(KeyStoryLevel, 0);
        set => Set(KeyStoryLevel, value);
    }

    public static bool Muted
    {
        get => PlayerPrefs.GetInt(KeyMuted, 0) == 1;
        set => Set(KeyMuted, value ? 1 : 0);
    }

    /// <summary>How many runs were started in total. Shown nowhere yet, useful for playtest feedback.</summary>
    public static int Runs
    {
        get => PlayerPrefs.GetInt(KeyRuns, 0);
        set => Set(KeyRuns, value);
    }

    public static string DailyId => DateTime.Now.ToString("yyyyMMdd");

    public static int Best(GameMode mode)
    {
        return PlayerPrefs.GetInt(BestKey(mode), 0);
    }

    /// <summary>Stores the score if it beats the old best. Returns true on a new record.</summary>
    public static bool SubmitScore(GameMode mode, int score)
    {
        if (score <= Best(mode))
        {
            return false;
        }

        Set(BestKey(mode), score);
        return true;
    }

    static string BestKey(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Endless: return KeyBestEndless;
            case GameMode.Daily: return KeyBestDaily + DailyId;
            default: return KeyBestStory;
        }
    }

    static void Set(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }
}

public enum GameMode
{
    None,
    Story,
    Endless,
    Daily,
}
