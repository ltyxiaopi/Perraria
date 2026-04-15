using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Inventory : MonoBehaviour
{
    public const int TotalSlots = 40;
    public const int HotbarSlots = 10;

    [SerializeField] private ItemStack[] _slots;

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
