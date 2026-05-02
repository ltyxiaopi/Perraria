using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GameObject _panel;
    [SerializeField] private SlotUI[] _slots;
    [SerializeField] private Image _cursorIcon;
    [SerializeField] private TMP_Text _cursorCountText;
    [SerializeField] private HotbarUI _hotbarUI;

    private ItemStack _cursorStack;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        InitializeSlots();
        ConfigureCursorGraphics();
        SetPanelActive(false);
        UpdateCursorVisuals();
    }

    private void OnEnable()
    {
        SubscribeInventoryEvents();
        SubscribeSlotClicks(_slots);
        SubscribeSlotClicks(GetHotbarSlots());
        RefreshAllSlots();
        UpdateCursorVisuals();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
        UnsubscribeSlotClicks(_slots);
        UnsubscribeSlotClicks(GetHotbarSlots());
        ClosePanel();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TogglePanel();
        }

        if (_isOpen)
        {
            UpdateCursorPosition();
        }

        UpdateCursorVisuals();
    }

    private void InitializeSlots()
    {
        if (_slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i]?.Initialize(i);
        }
    }

    private void ConfigureCursorGraphics()
    {
        if (_cursorIcon != null)
        {
            _cursorIcon.raycastTarget = false;
        }

        if (_cursorCountText != null)
        {
            _cursorCountText.raycastTarget = false;
        }
    }

    private void TogglePanel()
    {
        if (_isOpen)
        {
            ClosePanel();
            return;
        }

        OpenPanel();
    }

    private void OpenPanel()
    {
        _isOpen = true;
        SetPanelActive(true);
        RefreshAllSlots();
        UpdateCursorVisuals();
    }

    private void ClosePanel()
    {
        ReturnCursorToInventory();
        _isOpen = false;
        SetPanelActive(false);
        UpdateCursorVisuals();
    }

    private void SetPanelActive(bool active)
    {
        if (_panel != null)
        {
            _panel.SetActive(active);
        }
    }

    private void SubscribeInventoryEvents()
    {
        if (_inventory != null)
        {
            _inventory.OnSlotChanged += HandleSlotChanged;
        }
    }

    private void UnsubscribeInventoryEvents()
    {
        if (_inventory != null)
        {
            _inventory.OnSlotChanged -= HandleSlotChanged;
        }
    }

    private void SubscribeSlotClicks(SlotUI[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].OnClicked += HandleSlotClicked;
            }
        }
    }

    private void UnsubscribeSlotClicks(SlotUI[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].OnClicked -= HandleSlotClicked;
            }
        }
    }

    private void HandleSlotChanged(int index)
    {
        if (_slots == null)
        {
            return;
        }

        if (index >= 0 && index < _slots.Length)
        {
            _slots[index]?.UpdateDisplay(_inventory.GetSlot(index));
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (!_isOpen || _inventory == null)
        {
            return;
        }

        ItemStack slotStack = _inventory.GetSlot(slotIndex);
        if (_cursorStack.IsEmpty)
        {
            PickUpSlot(slotIndex, slotStack);
            return;
        }

        if (slotStack.IsEmpty)
        {
            PlaceCursorStack(slotIndex);
            return;
        }

        if (AreSameItem(_cursorStack, slotStack))
        {
            MergeCursorStack(slotIndex, slotStack);
            return;
        }

        SwapWithSlot(slotIndex, slotStack);
    }

    private void PickUpSlot(int slotIndex, ItemStack slotStack)
    {
        if (slotStack.IsEmpty)
        {
            return;
        }

        _cursorStack = slotStack;
        _inventory.SetSlot(slotIndex, ItemStack.Empty);
    }

    private void PlaceCursorStack(int slotIndex)
    {
        _inventory.SetSlot(slotIndex, _cursorStack);
        _cursorStack = ItemStack.Empty;
    }

    private void MergeCursorStack(int slotIndex, ItemStack slotStack)
    {
        int maxStackSize = Mathf.Max(1, slotStack.Item.MaxStackSize);
        int remainingSpace = maxStackSize - slotStack.Count;
        int transferCount = Mathf.Min(remainingSpace, _cursorStack.Count);

        if (transferCount <= 0)
        {
            return;
        }

        _inventory.SetSlot(
            slotIndex,
            new ItemStack(slotStack.Item, slotStack.Count + transferCount));
        _cursorStack = CreateCursorRemainder(transferCount);
    }

    private void SwapWithSlot(int slotIndex, ItemStack slotStack)
    {
        _inventory.SetSlot(slotIndex, _cursorStack);
        _cursorStack = slotStack;
    }

    private ItemStack CreateCursorRemainder(int transferCount)
    {
        int remainingCount = _cursorStack.Count - transferCount;
        return remainingCount > 0
            ? new ItemStack(_cursorStack.Item, remainingCount)
            : ItemStack.Empty;
    }

    private void ReturnCursorToInventory()
    {
        if (_inventory == null || _cursorStack.IsEmpty)
        {
            _cursorStack = ItemStack.Empty;
            return;
        }

        int remaining = _inventory.AddItem(_cursorStack.Item, _cursorStack.Count);
        _cursorStack = remaining > 0
            ? new ItemStack(_cursorStack.Item, remaining)
            : ItemStack.Empty;
    }

    private void RefreshAllSlots()
    {
        if (_inventory == null || _slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i]?.UpdateDisplay(_inventory.GetSlot(i));
        }
    }

    private void UpdateCursorPosition()
    {
        Vector2 mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        SetCursorPosition(_cursorIcon, mousePosition);
        SetCursorPosition(_cursorCountText, mousePosition + new Vector2(16f, -16f));
    }

    private void SetCursorPosition(Component component, Vector2 screenPosition)
    {
        if (component == null)
        {
            return;
        }

        ((RectTransform)component.transform).position = screenPosition;
    }

    private void UpdateCursorVisuals()
    {
        UpdateCursorIcon();
        UpdateCursorCountText();
    }

    private void UpdateCursorIcon()
    {
        if (_cursorIcon == null)
        {
            return;
        }

        bool shouldShow = _isOpen && !_cursorStack.IsEmpty && _cursorStack.Item.Icon != null;
        _cursorIcon.enabled = shouldShow;
        _cursorIcon.sprite = shouldShow ? _cursorStack.Item.Icon : null;
    }

    private void UpdateCursorCountText()
    {
        if (_cursorCountText == null)
        {
            return;
        }

        bool shouldShow = _isOpen && !_cursorStack.IsEmpty && _cursorStack.Count > 1;
        _cursorCountText.gameObject.SetActive(shouldShow);
        if (shouldShow)
        {
            _cursorCountText.text = _cursorStack.Count.ToString();
        }
    }

    private SlotUI[] GetHotbarSlots()
    {
        return _hotbarUI != null ? _hotbarUI.Slots : Array.Empty<SlotUI>();
    }

    private bool AreSameItem(ItemStack left, ItemStack right)
    {
        return !left.IsEmpty
            && !right.IsEmpty
            && left.Item.ItemId == right.Item.ItemId;
    }
}
