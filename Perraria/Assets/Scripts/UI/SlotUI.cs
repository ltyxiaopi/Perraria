using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private GameObject _selectionHighlight;

    public event Action<int> OnClicked;

    public int SlotIndex { get; private set; }

    private void Awake()
    {
        ConfigureRaycastTargets();
    }

    private void OnValidate()
    {
        ConfigureRaycastTargets();
    }

    public void Initialize(int slotIndex)
    {
        SlotIndex = slotIndex;
        ConfigureRaycastTargets();
    }

    public void UpdateDisplay(ItemStack stack)
    {
        UpdateIcon(stack);
        UpdateCountText(stack);
    }

    public void SetSelected(bool selected)
    {
        if (_selectionHighlight != null)
        {
            _selectionHighlight.SetActive(selected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClicked?.Invoke(SlotIndex);
    }

    private void UpdateIcon(ItemStack stack)
    {
        if (_icon == null)
        {
            return;
        }

        bool hasIcon = !stack.IsEmpty && stack.Item.Icon != null;
        _icon.enabled = hasIcon;
        _icon.sprite = hasIcon ? stack.Item.Icon : null;
    }

    private void UpdateCountText(ItemStack stack)
    {
        if (_countText == null)
        {
            return;
        }

        bool shouldShow = !stack.IsEmpty && stack.Count > 1;
        _countText.gameObject.SetActive(shouldShow);
        if (shouldShow)
        {
            _countText.text = stack.Count.ToString();
        }
    }

    private void ConfigureRaycastTargets()
    {
        SetRaycastTarget(_background, true);
        SetRaycastTarget(_icon, false);
        SetRaycastTarget(_countText, false);
        SetSelectionRaycastTarget();
    }

    private void SetSelectionRaycastTarget()
    {
        if (_selectionHighlight == null)
        {
            return;
        }

        Graphic graphic = _selectionHighlight.GetComponent<Graphic>();
        SetRaycastTarget(graphic, false);
    }

    private void SetRaycastTarget(Graphic graphic, bool raycastTarget)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }
}
