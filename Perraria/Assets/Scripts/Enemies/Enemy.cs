using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected int _maxHealth = 20;

    [Header("Combat")]
    [SerializeField] protected float _detectionRange = 10f;
    [SerializeField] protected int _contactDamage = 10;

    [Header("Drop")]
    [SerializeField] protected ItemDrop _itemDropPrefab;
    [SerializeField] protected ItemData _dropItem;
    [SerializeField] protected int _dropCount = 1;

    [Header("Ground Check")]
    [SerializeField] protected Transform _groundCheck;
    [SerializeField] protected float _groundCheckRadius = 0.15f;
    [SerializeField] protected LayerMask _groundLayer;

    [Header("Visual")]
    [SerializeField] protected SpriteRenderer _spriteRenderer;

    protected Rigidbody2D _rigidbody2D;
    protected Transform _playerTransform;
    protected int _currentHealth;
    protected bool _isGrounded;
    protected EnemyState _state = EnemyState.Idle;

    private Coroutine _hitFlashCoroutine;
    private Color _defaultSpriteColor = Color.white;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public EnemyState State => _state;
    public bool IsDead => _state == EnemyState.Dead;
    public bool IsGrounded => _isGrounded;

    public event Action<int, int> OnDamaged;
    public event Action OnDied;

    protected virtual void Reset()
    {
        _groundLayer = LayerMask.GetMask("Ground");

        if (_groundCheck == null)
        {
            Transform groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.6f, 0f);
            }

            _groundCheck = groundCheck;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    protected virtual void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _maxHealth = Mathf.Max(1, _maxHealth);
        _currentHealth = _maxHealth;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _defaultSpriteColor = _spriteRenderer.color;
        }

        TryCachePlayerTransform();
    }

    protected virtual void Update()
    {
        if (IsDead)
        {
            return;
        }

        UpdateGroundedState();
        UpdateDetection();
        UpdateBehavior();
        UpdateFacing();
    }

    protected abstract void UpdateBehavior();

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    public void TakeDamage(int damage, Vector2 knockbackDir, float force)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        ApplyKnockback(knockbackDir, force);
        PlayHitFlash();

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnDamaged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (IsDead)
        {
            return;
        }

        _state = EnemyState.Dead;
        OnDied?.Invoke();
        SpawnDrop();
        Destroy(gameObject);
    }

    protected virtual void SpawnDrop()
    {
        if (_itemDropPrefab == null || _dropItem == null || _dropCount <= 0)
        {
            return;
        }

        ItemDrop itemDrop = Instantiate(_itemDropPrefab, transform.position, Quaternion.identity);
        itemDrop.Initialize(_dropItem, _dropCount);
    }

    protected virtual void UpdateGroundedState()
    {
        _isGrounded = _groundCheck != null
            && Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
    }

    protected virtual void OnDisable()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _defaultSpriteColor;
        }
    }

    protected virtual void UpdateDetection()
    {
        if (_playerTransform == null)
        {
            TryCachePlayerTransform();
        }

        if (_playerTransform == null)
        {
            _state = EnemyState.Idle;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        _state = distanceToPlayer <= _detectionRange ? EnemyState.Chasing : EnemyState.Idle;
    }

    private void TryCachePlayerTransform()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    protected virtual void UpdateFacing()
    {
        if (_spriteRenderer == null || Mathf.Abs(_rigidbody2D.linearVelocity.x) < 0.01f)
        {
            return;
        }

        _spriteRenderer.flipX = _rigidbody2D.linearVelocity.x < 0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead)
        {
            return;
        }

        PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(_contactDamage);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }

    private void ApplyKnockback(Vector2 knockbackDir, float force)
    {
        if (_rigidbody2D == null || force <= 0f || knockbackDir.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _rigidbody2D.AddForce(knockbackDir.normalized * force, ForceMode2D.Impulse);
    }

    private void PlayHitFlash()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
            _spriteRenderer.color = _defaultSpriteColor;
        }
        else
        {
            _defaultSpriteColor = _spriteRenderer.color;
        }

        _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        _spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _defaultSpriteColor;
        }

        _hitFlashCoroutine = null;
    }
}
