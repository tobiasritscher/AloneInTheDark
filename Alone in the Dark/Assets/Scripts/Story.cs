/// <summary>
/// The whole story. One short line per chapter, shown over live gameplay and gone in two seconds.
/// Nobody on a five-minute bus ride wants to read a paragraph, so there are none.
/// You are a spark of light that woke up at the bottom of the caves and wants out.
/// </summary>
public static class Story
{
    /// <summary>"main|sub" per beat. Played once, ever, and skippable with a tap.</summary>
    public static readonly string[] Intro =
    {
        "At the beginning of time|there was nothing",
        "But out of the darkness|",
        "a wild light appeared|",
    };

    /// <summary>One line before each chapter. Index is the 0-based level number.</summary>
    public static readonly string[] Chapters =
    {
        "the light was alone, so it moved",
        "down here, even light falls",
        "something breathes in the tunnels",
        "the walls came closer",
        "other lights passed this way",
        "far above, something like a sky",
    };

    public static readonly string Ending = "Out|not alone anymore";

    /// <summary>Shown on death. Short, dry, never scolding.</summary>
    public static readonly string[] Deaths =
    {
        "the dark is patient",
        "every light goes out once",
        "rocks do not move, you do",
        "so close",
        "the caves keep score too",
        "a lighter touch",
    };

    public static string ChapterLine(int level)
    {
        if (level < 0 || Chapters.Length == 0)
        {
            return "";
        }

        return Chapters[level % Chapters.Length];
    }
}
