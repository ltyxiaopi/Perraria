using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SaveData
{
    public int Version = 1;
    public string SavedAtIso;
    public PlayerSaveData Player;
    public InventorySaveData Inventory;
    public WorldSaveData World;
    public SpawnerSaveData Spawner;

    private const string ItemDatabaseResourcePath = "ItemDatabase";
    private const string InitialPickaxeAssetName = "Item_Pickaxe_Wood";
    private const string InitialSwordAssetName = "Item_KnightSword";

    public static SaveData CreateNewGameDefault()
    {
        return new SaveData
        {
            Version = SaveSystem.CurrentVersion,
            SavedAtIso = DateTime.UtcNow.ToString("O"),
            Player = new PlayerSaveData
            {
                Position = Vector3.zero,
                CurrentHealth = 100,
                MaxHealth = 100,
                FacingRight = true
            },
            Inventory = new InventorySaveData
            {
                SelectedHotbarIndex = 0,
                Slots = BuildInitialSlots()
            },
            World = new WorldSaveData
            {
                Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                PlayerEdits = new List<WorldTileChange>()
            },
            Spawner = new SpawnerSaveData
            {
                SpawnTimer = 3f
            }
        };
    }

    private static List<InventorySlotSaveData> BuildInitialSlots()
    {
        ItemDatabase itemDatabase = Resources.Load<ItemDatabase>(ItemDatabaseResourcePath);
        if (itemDatabase == null)
        {
            throw new InvalidOperationException(
                $"Could not load ItemDatabase from Resources path '{ItemDatabaseResourcePath}'.");
        }

        ItemData pickaxe = FindItemByAssetName(itemDatabase, InitialPickaxeAssetName);
        ItemData sword = FindItemByAssetName(itemDatabase, InitialSwordAssetName);

        var slots = new List<InventorySlotSaveData>(global::Inventory.TotalSlots);
        for (int i = 0; i < global::Inventory.TotalSlots; i++)
        {
            slots.Add(new InventorySlotSaveData
            {
                ItemId = string.Empty,
                Count = 0
            });
        }

        SetSlot(slots, 0, pickaxe, 1);
        SetSlot(slots, 1, sword, 1);
        return slots;
    }

    private static ItemData FindItemByAssetName(ItemDatabase itemDatabase, string assetName)
    {
        IReadOnlyList<ItemData> items = itemDatabase.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item != null && item.name == assetName)
            {
                return item;
            }
        }

        throw new InvalidOperationException($"Item '{assetName}' was not found in ItemDatabase.");
    }

    private static void SetSlot(List<InventorySlotSaveData> slots, int index, ItemData item, int count)
    {
        slots[index] = new InventorySlotSaveData
        {
            ItemId = item.ItemId.ToString(),
            Count = count
        };
    }
}

[Serializable]
public sealed class PlayerSaveData
{
    public Vector3 Position;
    public int CurrentHealth;
    public int MaxHealth;
    public bool FacingRight;
}

[Serializable]
public sealed class InventorySaveData
{
    public List<InventorySlotSaveData> Slots;
    public int SelectedHotbarIndex;
}

[Serializable]
public sealed class InventorySlotSaveData
{
    public string ItemId;
    public int Count;
}

[Serializable]
public sealed class WorldSaveData
{
    public int Seed;
    public List<WorldTileChange> PlayerEdits;
}

[Serializable]
public sealed class WorldTileChange
{
    public int X;
    public int Y;
    public int BlockType;
}

[Serializable]
public sealed class SpawnerSaveData
{
    public float SpawnTimer;
}
