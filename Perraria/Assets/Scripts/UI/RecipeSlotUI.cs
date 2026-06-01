using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RecipeSlotUI : MonoBehaviour, IPointerClickHandler
{
    private static readonly Color AvailableColor = Color.white;
    private static readonly Color UnavailableColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color AvailableBackgroundColor = new Color(0.25f, 0.22f, 0.18f, 0.92f);
    private static readonly Color UnavailableBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.82f);

    [SerializeField] private Image _background;
    [SerializeField] private Image _outputIcon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TMP_Text[] _ingredientCountTexts;
    [SerializeField] private TMP_Text _stationText;

    private Recipe _recipe;
    private bool _canCraft;

    public event Action<Recipe> OnClicked;

    private void Awake()
    {
        ConfigureRaycastTargets();
    }

    private void OnValidate()
    {
        ConfigureRaycastTargets();
    }

    public void UpdateDisplay(Recipe recipe, Inventory inventory, bool hasIngredients, bool stationAvailable)
    {
        _recipe = recipe;
        _canCraft = recipe != null && hasIngredients && stationAvailable;

        UpdateOutput(recipe);
        UpdateIngredients(recipe, inventory);
        UpdateStation(recipe, stationAvailable);
        ApplyAvailabilityState(_canCraft);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null
            || eventData.button != PointerEventData.InputButton.Left
            || !_canCraft
            || _recipe == null)
        {
            return;
        }

        OnClicked?.Invoke(_recipe);
    }

    private void UpdateOutput(Recipe recipe)
    {
        ItemData output = recipe != null ? recipe.Output : null;

        if (_outputIcon != null)
        {
            bool hasIcon = output != null && output.Icon != null;
            _outputIcon.enabled = hasIcon;
            _outputIcon.sprite = hasIcon ? output.Icon : null;
        }

        if (_nameText != null)
        {
            if (output == null)
            {
                _nameText.text = string.Empty;
                return;
            }

            _nameText.text = recipe.OutputCount > 1
                ? $"{output.ItemName} x{recipe.OutputCount}"
                : output.ItemName;
        }
    }

    private void UpdateIngredients(Recipe recipe, Inventory inventory)
    {
        int ingredientCount = recipe != null && recipe.Inputs != null ? recipe.Inputs.Count : 0;
        int visualCount = _ingredientIcons != null ? _ingredientIcons.Length : 0;

        for (int i = 0; i < visualCount; i++)
        {
            Recipe.Ingredient ingredient = i < ingredientCount
                ? recipe.Inputs[i]
                : default;
            bool hasIngredient = ingredient.Item != null && ingredient.Count > 0;

            if (_ingredientIcons[i] != null)
            {
                _ingredientIcons[i].gameObject.SetActive(hasIngredient);
                _ingredientIcons[i].sprite = hasIngredient ? ingredient.Item.Icon : null;
            }

            TMP_Text countText = GetIngredientCountText(i);
            if (countText == null)
            {
                continue;
            }

            countText.gameObject.SetActive(hasIngredient);
            if (hasIngredient)
            {
                int ownedCount = inventory != null ? inventory.CountItem(ingredient.Item) : 0;
                countText.text = $"{ownedCount}/{ingredient.Count}";
            }
        }
    }

    private void UpdateStation(Recipe recipe, bool stationAvailable)
    {
        if (_stationText == null)
        {
            return;
        }

        if (recipe == null || !recipe.RequiresStation)
        {
            _stationText.text = string.Empty;
            return;
        }

        _stationText.text = stationAvailable ? "Workbench" : "Need Workbench";
    }

    private void ApplyAvailabilityState(bool canCraft)
    {
        Color color = canCraft ? AvailableColor : UnavailableColor;

        SetGraphicColor(_outputIcon, color);
        SetTextColor(_nameText, color);
        SetTextColor(_stationText, color);

        if (_ingredientIcons != null)
        {
            for (int i = 0; i < _ingredientIcons.Length; i++)
            {
                SetGraphicColor(_ingredientIcons[i], color);
                SetTextColor(GetIngredientCountText(i), color);
            }
        }

        if (_background != null)
        {
            _background.color = canCraft ? AvailableBackgroundColor : UnavailableBackgroundColor;
        }
    }

    private TMP_Text GetIngredientCountText(int index)
    {
        return _ingredientCountTexts != null && index >= 0 && index < _ingredientCountTexts.Length
            ? _ingredientCountTexts[index]
            : null;
    }

    private void ConfigureRaycastTargets()
    {
        SetRaycastTarget(_background, true);
        SetRaycastTarget(_outputIcon, false);
        SetRaycastTargets(_ingredientIcons, false);
        SetRaycastTarget(_nameText, false);
        SetRaycastTargets(_ingredientCountTexts, false);
        SetRaycastTarget(_stationText, false);
    }

    private void SetRaycastTargets(Graphic[] graphics, bool raycastTarget)
    {
        if (graphics == null)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            SetRaycastTarget(graphics[i], raycastTarget);
        }
    }

    private void SetGraphicColor(Graphic graphic, Color color)
    {
        if (graphic != null)
        {
            graphic.color = color;
        }
    }

    private void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
        {
            text.color = color;
        }
    }

    private void SetRaycastTarget(Graphic graphic, bool raycastTarget)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }
}
