using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int CurrentVersion = 1;
    public const string SaveFileName = "save.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        data.Version = CurrentVersion;
        data.SavedAtIso = DateTime.UtcNow.ToString("O");

        string directoryPath = Path.GetDirectoryName(SaveFilePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string temporaryPath = SaveFilePath + ".tmp";
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(temporaryPath, json);

        if (File.Exists(SaveFilePath))
        {
            File.Replace(temporaryPath, SaveFilePath, null);
            return;
        }

        File.Move(temporaryPath, SaveFilePath);
    }

    public static SaveData Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogWarning($"Save file could not be parsed: {SaveFilePath}");
                return null;
            }

            if (data.Version != CurrentVersion)
            {
                Debug.LogWarning(
                    $"Save file version {data.Version} does not match current version {CurrentVersion}; attempting load.");
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load save file '{SaveFilePath}': {exception.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }

        string temporaryPath = SaveFilePath + ".tmp";
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }
}
