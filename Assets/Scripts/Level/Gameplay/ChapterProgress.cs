using UnityEngine;

public static class ChapterProgress
{
    private const string HighestUnlockedKey =
        "AliceChaser.HighestUnlockedChapter";

    // Keep Chapter I available when no progress has been saved.
    public static int HighestUnlockedChapter =>
        Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedKey, 1));

    public static bool IsUnlocked(int chapterNumber)
    {
        // Check the chapter against the highest saved unlock.
        return chapterNumber <= HighestUnlockedChapter;
    }

    public static void UnlockChapter(int chapterNumber)
    {
        // Only save forward progression.
        if (chapterNumber <= HighestUnlockedChapter)
        {
            return;
        }

        PlayerPrefs.SetInt(HighestUnlockedKey, chapterNumber);
        PlayerPrefs.Save();
    }
}
