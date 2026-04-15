using System;

[Serializable]
public struct ItemStack
{
    public ItemData Item;
    public int Count;

    public bool IsEmpty => Item == null || Count <= 0;

    public ItemStack(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    public static ItemStack Empty => default;
}
