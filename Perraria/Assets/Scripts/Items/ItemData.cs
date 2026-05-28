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
    [SerializeField] private float _miningSpeedMultiplier = 1f;
    [SerializeField] private WeaponSubType _weaponSubType = WeaponSubType.Melee;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private ItemData _ammoItem;
    [SerializeField] private float _projectileSpeed = 12f;
    [SerializeField] private float _projectileLifetime = 5f;
    [SerializeField] private bool _projectileGravity = true;
    [SerializeField] private float _attackCooldown = 0.5f;
    [SerializeField] private GameObject _summonPrefab;
    [SerializeField] private bool _consumeOnUse = true;
    [SerializeField] private float _useCooldown = 1f;

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
    public float MiningSpeedMultiplier => _miningSpeedMultiplier;
    public WeaponSubType WeaponSubType => _weaponSubType;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public ItemData AmmoItem => _ammoItem;
    public float ProjectileSpeed => _projectileSpeed;
    public float ProjectileLifetime => _projectileLifetime;
    public bool ProjectileGravity => _projectileGravity;
    public float AttackCooldown => _attackCooldown;
    public GameObject SummonPrefab => _summonPrefab;
    public bool ConsumeOnUse => _consumeOnUse;
    public float UseCooldown => _useCooldown;
}
