using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Perraria/Item Data")]
public sealed class ItemData : ScriptableObject
{
    [SerializeField] private int _itemId;
    [SerializeField] private string _itemName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ItemType _type;
    [SerializeField] private int _maxStackSize = 99;
    [SerializeField] private BlockType _placeBlockType = BlockType.Air;

    public int ItemId => _itemId;
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemType Type => _type;
    public int MaxStackSize => _maxStackSize;
    public BlockType PlaceBlockType => _placeBlockType;
}
