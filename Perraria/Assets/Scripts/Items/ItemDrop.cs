using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemDrop : MonoBehaviour
{
    private static readonly Collider2D[] PickupResults = new Collider2D[8];

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _pickupDelay = 0.5f;
    [SerializeField] private float _bobAmplitude = 0.1f;
    [SerializeField] private float _bobFrequency = 2f;

    private ItemData _itemData;
    private int _count;
    private float _spawnTime;
    private bool _isPickedUp;
    private Vector3 _spawnPosition;
    private Collider2D _pickupCollider;

    public ItemData ItemData => _itemData;

    public int Count => _count;

    private void Awake()
    {
        _pickupCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_isPickedUp)
        {
            return;
        }

        float yOffset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
        transform.position = _spawnPosition + new Vector3(0f, yOffset, 0f);
        TryPickupNearbyInventory();
    }

    public void Initialize(ItemData itemData, int count)
    {
        _itemData = itemData;
        _count = Mathf.Max(0, count);
        _spawnTime = Time.time;
        _spawnPosition = transform.position;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = itemData != null ? itemData.Icon : null;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryPickup(other);
    }

    private void TryPickupNearbyInventory()
    {
        if (!CanBePickedUp() || _pickupCollider == null)
        {
            return;
        }

        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };

        int overlapCount = _pickupCollider.Overlap(contactFilter, PickupResults);
        for (int i = 0; i < overlapCount; i++)
        {
            TryPickup(PickupResults[i]);
            if (_isPickedUp)
            {
                return;
            }
        }
    }

    private void TryPickup(Collider2D other)
    {
        if (!CanBePickedUp() || other == null)
        {
            return;
        }

        Inventory inventory = other.GetComponent<Inventory>();
        if (inventory == null)
        {
            return;
        }

        int remaining = inventory.AddItem(_itemData, _count);
        if (remaining >= _count)
        {
            return;
        }

        _count = remaining;
        if (_count > 0)
        {
            return;
        }

        _isPickedUp = true;
        Destroy(gameObject);
    }

    private bool CanBePickedUp()
    {
        return !_isPickedUp
            && _itemData != null
            && _count > 0
            && Time.time - _spawnTime >= _pickupDelay;
    }
}
