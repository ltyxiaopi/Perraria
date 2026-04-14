using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Perraria/Item Database")]
public sealed class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemData[] _items;

    public ItemData GetItemById(int itemId)
    {
        if (_items == null)
        {
            return null;
        }

        ItemData foundItem = null;

        for (int i = 0; i < _items.Length; i++)
        {
            ItemData item = _items[i];
            if (item == null || item.ItemId != itemId)
            {
                continue;
            }

            if (foundItem == null)
            {
                foundItem = item;
                continue;
            }

#if UNITY_EDITOR
            Debug.LogWarning(
                $"Duplicate ItemData ID found: {itemId}. Using first match '{foundItem.name}'. Duplicate is '{item.name}'.",
                this);
#endif
            return foundItem;
        }

        return foundItem;
    }

    public IReadOnlyList<ItemData> GetAllItems()
    {
        return _items ?? Array.Empty<ItemData>();
    }
}
