using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    private const float FlashAlpha = 0.3f;
    private const float OpaqueAlpha = 1f;

    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _invincibilityDuration = 1f;
    [SerializeField] private float _flashInterval = 0.1f;
    [SerializeField] private float _regenDelay = 5f;
    [SerializeField] private int _regenPerSecond = 2;
    [SerializeField] private float _respawnDelay = 1f;
    [SerializeField] private Vector3 _respawnPoint;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private int _currentHealth;
    private bool _isInvincible;
    private bool _isDead;
    private float _invincibilityTimer;
    private float _flashTimer;
    private float _regenTimer;
    private float _regenAccumulator;
    private PlayerController _playerController;
    private PlayerBlockInteraction _blockInteraction;

    public int CurrentHealth => _currentHealth;

    public int MaxHealth => _maxHealth;

    public bool IsInvincible => _isInvincible;

    public bool IsDead => _isDead;

    public event Action<int, int> OnHealthChanged;

    public event Action OnDied;

    public event Action OnRespawned;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _blockInteraction = GetComponent<PlayerBlockInteraction>();
        _maxHealth = Mathf.Max(1, _maxHealth);
        _currentHealth = _maxHealth;
        _regenTimer = 0f;
        _regenAccumulator = 0f;
        RestoreSpriteAlpha();
        StartCoroutine(CaptureRespawnPointAfterDelay());
    }

    private IEnumerator CaptureRespawnPointAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);

        if (_respawnPoint == Vector3.zero)
        {
            _respawnPoint = transform.position;
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        HandleDebugInput();
        UpdateInvincibility();
        UpdateRegeneration();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || _isInvincible || _isDead)
        {
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        _regenTimer = _regenDelay;
        _regenAccumulator = 0f;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        StartInvincibility();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || _isDead || _currentHealth >= _maxHealth)
        {
            return;
        }

        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void HandleDebugInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TakeDamage(10);
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            TakeDamage(_currentHealth);
        }
    }

    private void UpdateInvincibility()
    {
        if (!_isInvincible)
        {
            return;
        }

        _invincibilityTimer -= Time.deltaTime;
        if (_invincibilityTimer <= 0f)
        {
            EndInvincibility();
            return;
        }

        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _flashTimer += Mathf.Max(0.01f, _flashInterval);
            ToggleSpriteAlpha();
        }
    }

    private void UpdateRegeneration()
    {
        if (_currentHealth >= _maxHealth)
        {
            return;
        }

        _regenTimer -= Time.deltaTime;
        if (_regenTimer > 0f)
        {
            return;
        }

        _regenAccumulator += _regenPerSecond * Time.deltaTime;
        int healAmount = Mathf.FloorToInt(_regenAccumulator);
        if (healAmount <= 0)
        {
            return;
        }

        _regenAccumulator -= healAmount;
        Heal(healAmount);
    }

    private void StartInvincibility()
    {
        _isInvincible = true;
        _invincibilityTimer = Mathf.Max(0f, _invincibilityDuration);
        _flashTimer = 0f;
        RestoreSpriteAlpha();
    }

    private void EndInvincibility()
    {
        _isInvincible = false;
        _invincibilityTimer = 0f;
        _flashTimer = 0f;
        RestoreSpriteAlpha();
    }

    private void Die()
    {
        _isDead = true;
        _isInvincible = false;
        _invincibilityTimer = 0f;
        _flashTimer = 0f;
        _regenTimer = 0f;
        _regenAccumulator = 0f;
        RestoreSpriteAlpha();
        OnDied?.Invoke();
        SetPlayerControlsEnabled(false);
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        transform.position = _respawnPoint;
        _currentHealth = _maxHealth;
        _isDead = false;
        _isInvincible = false;
        _invincibilityTimer = 0f;
        _flashTimer = 0f;
        _regenTimer = 0f;
        _regenAccumulator = 0f;
        RestoreSpriteAlpha();
        SetPlayerControlsEnabled(true);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnRespawned?.Invoke();
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (_playerController != null)
        {
            _playerController.enabled = enabled;
        }

        if (_blockInteraction != null)
        {
            _blockInteraction.enabled = enabled;
        }
    }

    private void ToggleSpriteAlpha()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        Color color = _spriteRenderer.color;
        color.a = Mathf.Approximately(color.a, OpaqueAlpha) ? FlashAlpha : OpaqueAlpha;
        _spriteRenderer.color = color;
    }

    private void RestoreSpriteAlpha()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        Color color = _spriteRenderer.color;
        color.a = OpaqueAlpha;
        _spriteRenderer.color = color;
    }
}
