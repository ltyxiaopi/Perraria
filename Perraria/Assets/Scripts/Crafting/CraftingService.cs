using UnityEngine;

public sealed class CraftingService
{
    public bool HasIngredients(Recipe recipe, Inventory inventory)
    {
        if (recipe == null || inventory == null || recipe.Inputs == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.Inputs.Count; i++)
        {
            Recipe.Ingredient ingredient = recipe.Inputs[i];
            if (ingredient.Item == null || ingredient.Count <= 0)
            {
                return false;
            }

            if (inventory.CountItem(ingredient.Item) < ingredient.Count)
            {
                return false;
            }
        }

        return recipe.Output != null && recipe.OutputCount > 0;
    }

    public bool CanCraft(Recipe recipe, Inventory inventory, bool stationAvailable)
    {
        if (recipe == null)
        {
            return false;
        }

        if (recipe.RequiresStation && !stationAvailable)
        {
            return false;
        }

        return HasIngredients(recipe, inventory);
    }

    public bool TryCraft(Recipe recipe, Inventory inventory, bool stationAvailable)
    {
        if (!CanCraft(recipe, inventory, stationAvailable))
        {
            return false;
        }

        int removedInputCount = 0;
        for (int i = 0; i < recipe.Inputs.Count; i++)
        {
            Recipe.Ingredient ingredient = recipe.Inputs[i];
            if (!inventory.RemoveItem(ingredient.Item, ingredient.Count))
            {
                RestoreInputs(recipe, inventory, removedInputCount);
                return false;
            }

            removedInputCount++;
        }

        int remaining = inventory.AddItem(recipe.Output, recipe.OutputCount);
        if (remaining == 0)
        {
            return true;
        }

        int addedOutputCount = recipe.OutputCount - remaining;
        if (addedOutputCount > 0)
        {
            inventory.RemoveItem(recipe.Output, addedOutputCount);
        }

        RestoreInputs(recipe, inventory, removedInputCount);
        return false;
    }

    private void RestoreInputs(Recipe recipe, Inventory inventory, int inputCount)
    {
        for (int i = 0; i < inputCount; i++)
        {
            Recipe.Ingredient ingredient = recipe.Inputs[i];
            int remaining = inventory.AddItem(ingredient.Item, ingredient.Count);
            if (remaining > 0)
            {
                Debug.LogError(
                    $"Failed to restore {remaining} '{ingredient.Item.ItemName}' after crafting rollback.");
            }
        }
    }
}
