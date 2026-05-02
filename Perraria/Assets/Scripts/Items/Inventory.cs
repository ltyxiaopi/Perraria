using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Inventory : MonoBehaviour
{
    public const int TotalSlots = 40;
    public const int HotbarSlots = 10;

    [SerializeField] private ItemStack[] _slots;
    [SerializeField] private ItemDatabase _itemDatabase;

    private int _selectedHotbarIndex;

    public event Action<int> OnSlotChanged;
    public event Action<int> OnSelectedHotbarChanged;

    public int SelectedHotbarIndex => _selectedHotbarIndex;

    #region Unity Lifecycle

    private void Reset()
    {
        EnsureSlotsInitialized();
    }

    private void Awake()
    {
        EnsureSlotsInitialized();
        _selectedHotbarIndex = Mathf.Clamp(_selectedHotbarIndex, 0, HotbarSlots - 1);
    }

    private void OnValidate()
    {
        EnsureSlotsInitialized();
        _selectedHotbarIndex = Mathf.Clamp(_selectedHotbarIndex, 0, HotbarSlots - 1);
    }

    #endregion

    public ItemStack GetSlot(int index)
    {
        return IsValidSlotIndex(index) ? _slots[index] : ItemStack.Empty;
    }

    public ItemStack GetSelectedItem()
    {
        return GetSlot(_selectedHotbarIndex);
    }

    public void SetSlot(int index, ItemStack stack)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlotIndex(index))
        {
            return;
        }

        _slots[index] = stack.IsEmpty ? ItemStack.Empty : stack;
        OnSlotChanged?.Invoke(index);
    }

    public int AddItem(ItemData item, int count = 1)
    {
        EnsureSlotsInitialized();

        if (item == null || count <= 0)
        {
            return count;
        }

        int remaining = MergeIntoExistingStacks(item, count);
        return remaining > 0 ? FillEmptySlots(item, remaining) : 0;
    }

    public bool RemoveFromSlot(int slotIndex, int count = 1)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlotIndex(slotIndex) || count <= 0)
        {
            return false;
        }

        ItemStack stack = _slots[slotIndex];
        if (stack.IsEmpty || stack.Count < count)
        {
            return false;
        }

        int newCount = stack.Count - count;
        _slots[slotIndex] = newCount > 0
            ? new ItemStack(stack.Item, newCount)
            : ItemStack.Empty;
        OnSlotChanged?.Invoke(slotIndex);
        return true;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlotIndex(indexA) || !IsValidSlotIndex(indexB) || indexA == indexB)
        {
            return;
        }

        (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]);
        OnSlotChanged?.Invoke(indexA);
        OnSlotChanged?.Invoke(indexB);
    }

    public void SelectHotbar(int index)
    {
        _selectedHotbarIndex = Mathf.Clamp(index, 0, HotbarSlots - 1);
        OnSelectedHotbarChanged?.Invoke(_selectedHotbarIndex);
    }

    public InventorySaveData CreateSnapshot()
    {
        EnsureSlotsInitialized();

        var slots = new List<InventorySlotSaveData>(TotalSlots);
        for (int i = 0; i < _slots.Length; i++)
        {
            ItemStack stack = _slots[i];
            slots.Add(new InventorySlotSaveData
            {
                ItemId = stack.IsEmpty ? string.Empty : stack.Item.ItemId.ToString(),
                Count = stack.IsEmpty ? 0 : stack.Count
            });
        }

        return new InventorySaveData
        {
            Slots = slots,
            SelectedHotbarIndex = _selectedHotbarIndex
        };
    }

    public void RestoreFromSnapshot(InventorySaveData data)
    {
        EnsureSlotsInitialized();
        ClearSlotsWithoutEvents();

        ItemDatabase itemDatabase = ResolveItemDatabase();
        if (data?.Slots != null)
        {
            RestoreSlots(data.Slots, itemDatabase);
        }

        NotifyAllSlotsChanged();
        SelectHotbar(data != null ? data.SelectedHotbarIndex : 0);
    }

    private int MergeIntoExistingStacks(ItemData item, int count)
    {
        int remaining = count;

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            ItemStack stack = _slots[i];
            if (stack.IsEmpty || stack.Item.ItemId != item.ItemId)
            {
                continue;
            }

            remaining = AddToSlot(i, item, remaining);
        }

        return remaining;
    }

    private int FillEmptySlots(ItemData item, int count)
    {
        int remaining = count;

        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                continue;
            }

            remaining = AddToSlot(i, item, remaining);
        }

        return remaining;
    }

    private int AddToSlot(int index, ItemData item, int count)
    {
        int maxStackSize = Mathf.Max(1, item.MaxStackSize);
        ItemStack stack = _slots[index];
        int currentCount = stack.IsEmpty ? 0 : stack.Count;
        int addCount = Mathf.Min(maxStackSize - currentCount, count);

        if (addCount <= 0)
        {
            return count;
        }

        _slots[index] = new ItemStack(item, currentCount + addCount);
        OnSlotChanged?.Invoke(index);
        return count - addCount;
    }

    private void RestoreSlots(IReadOnlyList<InventorySlotSaveData> slots, ItemDatabase itemDatabase)
    {
        int slotCount = Mathf.Min(slots.Count, TotalSlots);
        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotSaveData slot = slots[i];
            if (slot == null || string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
            {
                continue;
            }

            if (!int.TryParse(slot.ItemId, out int itemId))
            {
                Debug.LogWarning($"Invalid saved item id '{slot.ItemId}' in inventory slot {i}.");
                continue;
            }

            ItemData item = itemDatabase != null ? itemDatabase.GetItemById(itemId) : null;
            if (item == null)
            {
                Debug.LogWarning($"Saved item id '{slot.ItemId}' was not found in ItemDatabase.");
                continue;
            }

            _slots[i] = new ItemStack(item, slot.Count);
        }
    }

    private void ClearSlotsWithoutEvents()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = ItemStack.Empty;
        }
    }

    private void NotifyAllSlotsChanged()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            OnSlotChanged?.Invoke(i);
        }
    }

    private ItemDatabase ResolveItemDatabase()
    {
        if (_itemDatabase != null)
        {
            return _itemDatabase;
        }

        ItemDatabase[] loadedDatabases = Resources.FindObjectsOfTypeAll<ItemDatabase>();
        if (loadedDatabases.Length > 0)
        {
            _itemDatabase = loadedDatabases[0];
        }

        return _itemDatabase;
    }

    private void EnsureSlotsInitialized()
    {
        if (_slots != null && _slots.Length == TotalSlots)
        {
            return;
        }

        ItemStack[] slots = new ItemStack[TotalSlots];
        if (_slots != null)
        {
            int copyLength = Mathf.Min(_slots.Length, TotalSlots);
            Array.Copy(_slots, slots, copyLength);
        }

        _slots = slots;
    }

    private bool IsValidSlotIndex(int index)
    {
        return _slots != null && index >= 0 && index < _slots.Length;
    }
}
