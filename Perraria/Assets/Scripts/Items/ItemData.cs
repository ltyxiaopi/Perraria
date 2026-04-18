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
    [SerializeField] private int _weaponDamage;
    [SerializeField] private float _weaponRange;
    [SerializeField] private float _swingArcDegrees = 100f;
    [SerializeField] private float _swingDuration = 0.35f;
    [SerializeField] private float _knockbackForce;

    public int ItemId => _itemId;
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemType Type => _type;
    public int MaxStackSize => _maxStackSize;
    public BlockType PlaceBlockType => _placeBlockType;
    public int WeaponDamage => _weaponDamage;
    public float WeaponRange => _weaponRange;
    public float SwingArcDegrees => _swingArcDegrees;
    public float SwingDuration => _swingDuration;
    public float KnockbackForce => _knockbackForce;
}
