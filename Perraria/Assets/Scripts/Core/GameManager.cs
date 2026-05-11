using UnityEngine;

public enum GameLaunchMode
{
    NewGame,
    ContinueSave
}

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameLaunchMode PendingLaunchMode = GameLaunchMode.NewGame;

    private static bool _shouldLoadNewGameSave;

    public static void QueueNewGameFromInitialSave()
    {
        PendingLaunchMode = GameLaunchMode.NewGame;
        _shouldLoadNewGameSave = true;
    }

    private void Start()
    {
        if (PendingLaunchMode != GameLaunchMode.ContinueSave && !_shouldLoadNewGameSave)
        {
            return;
        }

        _shouldLoadNewGameSave = false;

        SaveData loaded = SaveSystem.Load();
        if (loaded == null)
        {
            Debug.LogWarning("Save load requested, but no valid save data was loaded. Starting a new game.");
            PendingLaunchMode = GameLaunchMode.NewGame;
            return;
        }

        GameStateSnapshot.Apply(loaded);
    }
}
