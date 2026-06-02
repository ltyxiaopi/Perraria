using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorkbenchUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Recipe[] _recipes;
    [SerializeField] private RecipeSlotUI[] _recipeSlots;
    [SerializeField] private WorkbenchProximity _workbenchProximity;
    [SerializeField] private TMP_Text _craftingFeedbackText;
    [SerializeField] private Button _closeButton;

    private readonly CraftingService _craftingService = new();
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        SetPanelActive(false);
        ClearCraftingFeedback();
    }

    private void OnEnable()
    {
        SubscribeInventoryEvents();
        SubscribeRecipeClicks();
        SubscribeCloseButton();
        RefreshRecipes();
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvents();
        UnsubscribeRecipeClicks();
        UnsubscribeCloseButton();
        Close();
    }

    private void Update()
    {
        if (!_isOpen || Time.timeScale <= 0f)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (_workbenchProximity != null && !_workbenchProximity.IsNearWorkbench())
        {
            Close();
        }
    }

    public void Open()
    {
        _isOpen = true;
        SetPanelActive(true);
        ClearCraftingFeedback();
        RefreshRecipes();
    }

    public void Close()
    {
        _isOpen = false;
        SetPanelActive(false);
        ClearCraftingFeedback();
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

    private void SubscribeRecipeClicks()
    {
        if (_recipeSlots == null)
        {
            return;
        }

        for (int i = 0; i < _recipeSlots.Length; i++)
        {
            if (_recipeSlots[i] != null)
            {
                _recipeSlots[i].OnClicked += HandleRecipeClicked;
            }
        }
    }

    private void UnsubscribeRecipeClicks()
    {
        if (_recipeSlots == null)
        {
            return;
        }

        for (int i = 0; i < _recipeSlots.Length; i++)
        {
            if (_recipeSlots[i] != null)
            {
                _recipeSlots[i].OnClicked -= HandleRecipeClicked;
            }
        }
    }

    private void SubscribeCloseButton()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }
    }

    private void UnsubscribeCloseButton()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }
    }

    private void HandleSlotChanged(int index)
    {
        if (_isOpen)
        {
            RefreshRecipes();
        }
    }

    private void HandleRecipeClicked(Recipe recipe)
    {
        if (!_isOpen || recipe == null || _inventory == null)
        {
            return;
        }

        const bool stationAvailable = true;
        if (!_craftingService.CanCraft(recipe, _inventory, stationAvailable))
        {
            SetCraftingFeedback("Missing materials");
            RefreshRecipes();
            return;
        }

        if (!_craftingService.TryCraft(recipe, _inventory, stationAvailable))
        {
            SetCraftingFeedback("Inventory full");
            RefreshRecipes();
            return;
        }

        ClearCraftingFeedback();
        RefreshRecipes();
    }

    private void RefreshRecipes()
    {
        if (_recipeSlots == null)
        {
            return;
        }

        for (int i = 0; i < _recipeSlots.Length; i++)
        {
            Recipe recipe = _recipes != null && i < _recipes.Length ? _recipes[i] : null;
            bool hasIngredients = _craftingService.HasIngredients(recipe, _inventory);
            _recipeSlots[i]?.UpdateDisplay(recipe, _inventory, hasIngredients, true);
        }
    }

    private void SetPanelActive(bool active)
    {
        if (_panel != null)
        {
            _panel.SetActive(active);
        }
    }

    private void SetCraftingFeedback(string message)
    {
        if (_craftingFeedbackText == null)
        {
            return;
        }

        _craftingFeedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        _craftingFeedbackText.text = message;
    }

    private void ClearCraftingFeedback()
    {
        SetCraftingFeedback(string.Empty);
    }
}
