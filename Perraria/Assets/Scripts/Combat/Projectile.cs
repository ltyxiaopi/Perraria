using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Projectile : MonoBehaviour
{
    public enum Owner
    {
        Player,
        Enemy
    }

    [SerializeField] private ItemDrop _itemDropPrefab;
    [SerializeField] private bool _spinWhileFlying;
    [SerializeField] private float _spinDegreesPerSecond = 720f;

    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    private int _damage;
    private float _knockbackForce;
    private LayerMask _hitTargetLayer;
    private LayerMask _terrainLayer;
    private Owner _owner;
    private float _lifetimeRemaining;
    private bool _useGravity;
    private bool _stickOnTerrain;
    private ItemData _pickupItemOnStick;
    private bool _hasLaunched;
    private readonly Collider2D[] _overlapResults = new Collider2D[4];

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!_hasLaunched)
        {
            return;
        }

        _lifetimeRemaining -= Time.deltaTime;
        if (_lifetimeRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (TryHitOverlappingTarget())
        {
            return;
        }

        if (_spinWhileFlying)
        {
            transform.Rotate(0f, 0f, _spinDegreesPerSecond * Time.deltaTime);
            return;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.right, velocity.normalized);
        }
    }

    public void Launch(Vector2 direction, float speed, ProjectileLaunchParams launchParams)
    {
        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        Vector2 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _damage = launchParams.Damage;
        _knockbackForce = launchParams.Knockback;
        _hitTargetLayer = launchParams.TargetLayer;
        _terrainLayer = launchParams.TerrainLayer;
        _owner = launchParams.Owner;
        _lifetimeRemaining = Mathf.Max(0.01f, launchParams.Lifetime);
        _useGravity = launchParams.UseGravity;
        _stickOnTerrain = launchParams.StickOnTerrain;
        _pickupItemOnStick = launchParams.PickupItemOnStick;
        _hasLaunched = true;

        _rigidbody2D.gravityScale = _useGravity ? 1f : 0f;
        _rigidbody2D.linearVelocity = launchDirection * Mathf.Max(0f, speed);
        _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (!_spinWhileFlying)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.right, launchDirection);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hasLaunched || collision.collider == null)
        {
            return;
        }

        int collisionLayerMask = 1 << collision.collider.gameObject.layer;
        if ((_hitTargetLayer.value & collisionLayerMask) != 0)
        {
            HitTarget(collision.collider);
            Destroy(gameObject);
            return;
        }

        if ((_terrainLayer.value & collisionLayerMask) != 0)
        {
            HitTerrain();
        }
    }

    private bool TryHitOverlappingTarget()
    {
        if (_collider2D == null || _hitTargetLayer.value == 0)
        {
            return false;
        }

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = _hitTargetLayer,
            useTriggers = true
        };

        int hitCount = _collider2D.Overlap(filter, _overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null || hit == _collider2D)
            {
                continue;
            }

            HitTarget(hit);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private void HitTarget(Collider2D hitCollider)
    {
        Vector2 knockbackDirection = _rigidbody2D.linearVelocity.sqrMagnitude > 0.0001f
            ? _rigidbody2D.linearVelocity.normalized
            : (Vector2)transform.right;

        if (_owner == Owner.Player)
        {
            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage, knockbackDirection, _knockbackForce);
            }

            return;
        }

        PlayerHealth playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(_damage);
        }
    }

    private void HitTerrain()
    {
        if (!_stickOnTerrain)
        {
            Destroy(gameObject);
            return;
        }

        _hasLaunched = false;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.gravityScale = 0f;
        _rigidbody2D.freezeRotation = true;

        if (_itemDropPrefab != null && _pickupItemOnStick != null)
        {
            ItemDrop itemDrop = Instantiate(_itemDropPrefab, transform.position, Quaternion.identity);
            itemDrop.Initialize(_pickupItemOnStick, 1);
        }

        Destroy(this);
        Destroy(gameObject);
    }
}
