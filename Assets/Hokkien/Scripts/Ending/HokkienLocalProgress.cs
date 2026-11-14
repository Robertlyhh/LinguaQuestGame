using System;
using System.IO;
using UnityEngine;

public static class HokkienLocalProgress
{
    public const string DefaultSaveFileName = "hokkien_save.json";

    public static bool TryReset(string saveFileName, out string error)
    {
        error = null;

        if (!IsSafeFileName(saveFileName))
        {
            error = "The configured save file name is invalid.";
            return false;
        }

        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            VoucherAdventureNavigation.ClearSavedNavigationState();
            SessionManager.PreventSavingDeletedProgress();
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            Debug.LogError($"[HokkienLocalProgress] Could not clear local progress: {exception}");
            return false;
        }
    }

    private static bool IsSafeFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
               && fileName == Path.GetFileName(fileName)
               && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}
