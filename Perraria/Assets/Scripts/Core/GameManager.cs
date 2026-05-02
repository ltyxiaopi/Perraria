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

    private void Start()
    {
        if (PendingLaunchMode != GameLaunchMode.ContinueSave)
        {
            return;
        }

        SaveData loaded = SaveSystem.Load();
        if (loaded == null)
        {
            Debug.LogWarning("Continue requested, but no valid save data was loaded. Starting a new game.");
            PendingLaunchMode = GameLaunchMode.NewGame;
            return;
        }

        GameStateSnapshot.Apply(loaded);
    }
}
