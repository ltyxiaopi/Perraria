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
