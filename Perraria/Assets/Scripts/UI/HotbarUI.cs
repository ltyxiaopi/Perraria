using UnityEngine;

[DisallowMultipleComponent]
public sealed class HotbarUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private SlotUI[] _slots;

    public SlotUI[] Slots => _slots;

    private void Awake()
    {
        InitializeSlots();
        RefreshAll();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void InitializeSlots()
    {
        if (_slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length && i < Inventory.HotbarSlots; i++)
        {
            _slots[i]?.Initialize(i);
        }
    }

    private void SubscribeEvents()
    {
        if (_inventory == null)
        {
            return;
        }

        _inventory.OnSlotChanged += HandleSlotChanged;
        _inventory.OnSelectedHotbarChanged += HandleSelectedHotbarChanged;
    }

    private void UnsubscribeEvents()
    {
        if (_inventory == null)
        {
            return;
        }

        _inventory.OnSlotChanged -= HandleSlotChanged;
        _inventory.OnSelectedHotbarChanged -= HandleSelectedHotbarChanged;
    }

    private void HandleSlotChanged(int index)
    {
        if (!IsValidHotbarIndex(index))
        {
            return;
        }

        RefreshSlot(index);
    }

    private void HandleSelectedHotbarChanged(int index)
    {
        RefreshSelection(index);
    }

    private void RefreshAll()
    {
        if (_inventory == null || _slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length && i < Inventory.HotbarSlots; i++)
        {
            RefreshSlot(i);
        }

        RefreshSelection(_inventory.SelectedHotbarIndex);
    }

    private void RefreshSlot(int index)
    {
        if (_inventory == null || !IsValidHotbarIndex(index))
        {
            return;
        }

        _slots[index]?.UpdateDisplay(_inventory.GetSlot(index));
    }

    private void RefreshSelection(int selectedIndex)
    {
        if (_slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length && i < Inventory.HotbarSlots; i++)
        {
            _slots[i]?.SetSelected(i == selectedIndex);
        }
    }

    private bool IsValidHotbarIndex(int index)
    {
        return _slots != null
            && index >= 0
            && index < Inventory.HotbarSlots
            && index < _slots.Length;
    }
}
