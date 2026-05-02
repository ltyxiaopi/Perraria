using System;
using System.Linq;
using UnityEngine;

public static class GameStateSnapshot
{
    public static SaveData Capture()
    {
        return new SaveData
        {
            Version = SaveSystem.CurrentVersion,
            SavedAtIso = DateTime.UtcNow.ToString("O"),
            World = CaptureWorld(),
            Player = CapturePlayer(),
            Inventory = CaptureInventory(),
            Spawner = CaptureSpawner()
        };
    }

    public static void Apply(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        ApplyWorld(data.World);
        ApplyPlayer(data.Player);
        ApplyInventory(data.Inventory);
        ApplySpawner(data.Spawner);
    }

    private static WorldSaveData CaptureWorld()
    {
        WorldGenerator generator = UnityEngine.Object.FindFirstObjectByType<WorldGenerator>();
        TileManager tileManager = UnityEngine.Object.FindFirstObjectByType<TileManager>();

        return new WorldSaveData
        {
            Seed = generator != null ? generator.Seed : 0,
            PlayerEdits = tileManager != null ? tileManager.EnumerateChanges().ToList() : null
        };
    }

    private static PlayerSaveData CapturePlayer()
    {
        PlayerController controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        PlayerHealth health = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        Transform playerTransform = controller != null
            ? controller.transform
            : health != null ? health.transform : null;

        return new PlayerSaveData
        {
            Position = playerTransform != null ? playerTransform.position : Vector3.zero,
            CurrentHealth = health != null ? health.CurrentHealth : 0,
            MaxHealth = health != null ? health.MaxHealth : 1,
            FacingRight = controller == null || controller.FacingRight
        };
    }

    private static InventorySaveData CaptureInventory()
    {
        Inventory inventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        return inventory != null ? inventory.CreateSnapshot() : null;
    }

    private static SpawnerSaveData CaptureSpawner()
    {
        EnemySpawner spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return new SpawnerSaveData
        {
            SpawnTimer = spawner != null ? spawner.SpawnTimer : 0f
        };
    }

    private static void ApplyWorld(WorldSaveData data)
    {
        if (data == null)
        {
            return;
        }

        WorldGenerator generator = UnityEngine.Object.FindFirstObjectByType<WorldGenerator>();
        TileManager tileManager = UnityEngine.Object.FindFirstObjectByType<TileManager>();

        if (generator != null)
        {
            generator.GenerateWorldWithSeed(data.Seed);
            tileManager = UnityEngine.Object.FindFirstObjectByType<TileManager>();
        }

        if (tileManager == null)
        {
            return;
        }

        tileManager.ClearChangeTracking();
        tileManager.ApplyTileChanges(data.PlayerEdits);
    }

    private static void ApplyPlayer(PlayerSaveData data)
    {
        if (data == null)
        {
            return;
        }

        PlayerController controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        PlayerHealth health = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();

        if (controller != null)
        {
            controller.RestoreState(data.Position, data.FacingRight);
        }

        if (health != null)
        {
            health.RestoreState(data.CurrentHealth, data.MaxHealth);
        }
    }

    private static void ApplyInventory(InventorySaveData data)
    {
        Inventory inventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        if (inventory != null)
        {
            inventory.RestoreFromSnapshot(data);
        }
    }

    private static void ApplySpawner(SpawnerSaveData data)
    {
        EnemySpawner spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null && data != null)
        {
            spawner.SpawnTimer = data.SpawnTimer;
        }
    }
}
