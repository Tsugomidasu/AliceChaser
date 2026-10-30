using UnityEngine;

public static class ChapterProgress
{
    private const string HighestUnlockedKey =
        "AliceChaser.HighestUnlockedChapter";

    // Chapter I remains available even before any progress has been saved.
    public static int HighestUnlockedChapter =>
        Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedKey, 1));

    public static bool IsUnlocked(int chapterNumber)
    {
        return chapterNumber <= HighestUnlockedChapter;
    }

    public static void UnlockChapter(int chapterNumber)
    {
        // Never overwrite progress with an earlier chapter.
        if (chapterNumber <= HighestUnlockedChapter)
        {
            return;
        }

        PlayerPrefs.SetInt(HighestUnlockedKey, chapterNumber);
        PlayerPrefs.Save();
    }
}
