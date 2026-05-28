using System.Collections;
using UnityEngine;

public sealed class EyeOfCorruption : Enemy
{
    private enum BossPhase
    {
        Phase1Hover,
        Phase1Charge,
        Phase2Hover,
        Phase2Charge,
        Defeated
    }

    [Header("Boss")]
    [SerializeField] private float _hoverHeight = 5f;
    [SerializeField] private float _hoverFollowSpeed = 6f;
    [SerializeField] private float _hoverSwayAmplitude = 1.25f;
    [SerializeField] private float _hoverSwayFrequency = 1f;
    [SerializeField] private float _phase1HoverDuration = 2f;
    [SerializeField] private float _phase2HoverDuration = 1.5f;
    [SerializeField] private float _chargeDuration = 1f;
    [SerializeField] private float _phase1ChargeSpeed = 12f;
    [SerializeField] private float _phase2ChargeSpeed = 14f;
    [SerializeField] private GameObject _acidProjectilePrefab;
    [SerializeField] private float _phase2ProjectileInterval = 1f;
    [SerializeField] private float _acidProjectileSpeed = 9f;
    [SerializeField] private int _acidProjectileDamage = 12;
    [SerializeField] private float _acidProjectileKnockback = 5f;
    [SerializeField] private float _acidProjectileLifetime = 6f;
    [SerializeField] private LayerMask _projectileTargetLayer;
    [SerializeField] private LayerMask _terrainLayer;

    [Header("Phase Animations")]
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private Sprite[] _detectFrames;
    [SerializeField] private Sprite[] _dripAcidFrames;
    [SerializeField] private float _idleFps = 4f;
    [SerializeField] private float _detectFps = 8f;
    [SerializeField] private float _dripAcidFps = 10f;

    private BossPhase _phase = BossPhase.Phase1Hover;
    private Vector2 _chargeDirection;
    private float _phaseTimer;
    private float _projectileTimer;
    private float _animationTimer;
    private float _dripAnimationTimer;
    private bool _isPlayingDripAcid;
    private bool _firedProjectileThisHover;
    private Coroutine _defeatCoroutine;

    public string DebugPhaseName => _phase.ToString();

    protected override void Reset()
    {
        base.Reset();
        _maxHealth = 400;
        _contactDamage = 25;
        _detectionRange = 30f;
        _groundLayer = LayerMask.GetMask("Ground");
        _projectileTargetLayer = LayerMask.GetMask("Player");
        _terrainLayer = LayerMask.GetMask("Ground");
    }

    protected override void Awake()
    {
        base.Awake();

        if (_rigidbody2D != null)
        {
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
        }

        if (_projectileTargetLayer.value == 0)
        {
            _projectileTargetLayer = LayerMask.GetMask("Player");
        }

        if (_terrainLayer.value == 0)
        {
            _terrainLayer = LayerMask.GetMask("Ground");
        }
    }

    protected override void UpdateBehavior()
    {
        if (_phase == BossPhase.Defeated)
        {
            Halt();
            return;
        }

        _animationTimer += Time.deltaTime;

        if (_playerTransform == null || _state != EnemyState.Chasing)
        {
            Halt();
            PlayHoverAnimation();
            return;
        }

        TryEnterPhase2();

        _phaseTimer += Time.deltaTime;

        switch (_phase)
        {
            case BossPhase.Phase1Hover:
                UpdateHover(_phase1HoverDuration, BossPhase.Phase1Charge);
                PlayLoop(_idleFrames, _idleFps);
                break;
            case BossPhase.Phase1Charge:
                UpdateCharge(BossPhase.Phase1Hover);
                PlayLoop(_detectFrames, _detectFps);
                break;
            case BossPhase.Phase2Hover:
                UpdatePhase2Hover();
                break;
            case BossPhase.Phase2Charge:
                UpdateCharge(BossPhase.Phase2Hover);
                PlayLoop(_detectFrames, _detectFps);
                break;
        }
    }

    protected override void UpdateGroundedState()
    {
        _isGrounded = true;
    }

    protected override void Die()
    {
        if (_defeatCoroutine != null || !TryEnterDeadState())
        {
            return;
        }

        _phase = BossPhase.Defeated;
        Halt();
        DisableColliders();
        _defeatCoroutine = StartCoroutine(DefeatRoutine());
    }

    private void TryEnterPhase2()
    {
        if (_phase != BossPhase.Phase1Hover && _phase != BossPhase.Phase1Charge)
        {
            return;
        }

        if (_currentHealth > Mathf.FloorToInt(_maxHealth * 0.5f))
        {
            return;
        }

        _phase = BossPhase.Phase2Hover;
        _phaseTimer = 0f;
        _projectileTimer = 0f;
        _animationTimer = 0f;
        _isPlayingDripAcid = false;
        _firedProjectileThisHover = false;
        Halt();
        Debug.Log($"EyeOfCorruption phase switch: HP {_currentHealth}/{_maxHealth}, entering Phase2Hover.", this);
    }

    private void UpdateHover(float hoverDuration, BossPhase nextPhase)
    {
        MoveTowardHoverPoint();

        if (_phaseTimer < hoverDuration)
        {
            return;
        }

        BeginCharge(nextPhase);
    }

    private void UpdatePhase2Hover()
    {
        MoveTowardHoverPoint();
        _projectileTimer += Time.deltaTime;

        if (_isPlayingDripAcid)
        {
            UpdateDripAcidAnimation();
        }
        else
        {
            PlayLoop(_detectFrames, _detectFps);
            TryBeginDripAcid();
        }

        if (_phaseTimer >= _phase2HoverDuration && !_isPlayingDripAcid)
        {
            BeginCharge(BossPhase.Phase2Charge);
        }
    }

    private void MoveTowardHoverPoint()
    {
        float sway = Mathf.Sin(Time.time * _hoverSwayFrequency * Mathf.PI * 2f) * _hoverSwayAmplitude;
        Vector3 target = _playerTransform.position + new Vector3(sway, _hoverHeight, 0f);
        Vector2 toTarget = target - transform.position;
        Vector2 velocity = Vector2.ClampMagnitude(toTarget * _hoverFollowSpeed, _hoverFollowSpeed);
        _rigidbody2D.linearVelocity = velocity;
    }

    private void BeginCharge(BossPhase chargePhase)
    {
        Vector2 toPlayer = _playerTransform.position - transform.position;
        _chargeDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.down;
        _phase = chargePhase;
        _phaseTimer = 0f;
        _animationTimer = 0f;
        _isPlayingDripAcid = false;
    }

    private void UpdateCharge(BossPhase hoverPhase)
    {
        float speed = _phase == BossPhase.Phase2Charge ? _phase2ChargeSpeed : _phase1ChargeSpeed;
        _rigidbody2D.linearVelocity = _chargeDirection * speed;

        if (_phaseTimer < _chargeDuration)
        {
            return;
        }

        _phase = hoverPhase;
        _phaseTimer = 0f;
        _projectileTimer = 0f;
        _animationTimer = 0f;
        _firedProjectileThisHover = false;
        Halt();
    }

    private void TryBeginDripAcid()
    {
        if (_firedProjectileThisHover || _dripAcidFrames == null || _dripAcidFrames.Length == 0)
        {
            return;
        }

        float dripDuration = _dripAcidFrames.Length / Mathf.Max(0.01f, _dripAcidFps);
        float prefireStartTime = Mathf.Max(0f, _phase2ProjectileInterval - dripDuration);
        if (_projectileTimer < prefireStartTime)
        {
            return;
        }

        _projectileTimer = 0f;
        _dripAnimationTimer = 0f;
        _isPlayingDripAcid = true;
    }

    private void UpdateDripAcidAnimation()
    {
        _dripAnimationTimer += Time.deltaTime;
        PlayLoop(_dripAcidFrames, _dripAcidFps, _dripAnimationTimer);

        float duration = _dripAcidFrames.Length / Mathf.Max(0.01f, _dripAcidFps);
        if (_dripAnimationTimer < duration)
        {
            return;
        }

        FireAcidProjectile();
        _isPlayingDripAcid = false;
        _firedProjectileThisHover = true;
    }

    private void FireAcidProjectile()
    {
        if (_acidProjectilePrefab == null || _playerTransform == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * 0.75f);
        GameObject projectileObject = Instantiate(_acidProjectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return;
        }

        ProjectileLaunchParams launchParams = CreateAcidLaunchParams();
        projectile.Launch(direction, _acidProjectileSpeed, launchParams);
    }

    private ProjectileLaunchParams CreateAcidLaunchParams()
    {
        return new ProjectileLaunchParams
        {
            Damage = _acidProjectileDamage,
            Knockback = _acidProjectileKnockback,
            Lifetime = _acidProjectileLifetime,
            UseGravity = false,
            Owner = Projectile.Owner.Enemy,
            TargetLayer = _projectileTargetLayer,
            TerrainLayer = _terrainLayer,
            StickOnTerrain = false,
            PickupItemOnStick = null
        };
    }

    private void PlayHoverAnimation()
    {
        Sprite[] frames = _phase == BossPhase.Phase2Hover ? _detectFrames : _idleFrames;
        float fps = _phase == BossPhase.Phase2Hover ? _detectFps : _idleFps;
        PlayLoop(frames, fps);
    }

    private void PlayLoop(Sprite[] frames, float framesPerSecond)
    {
        PlayLoop(frames, framesPerSecond, _animationTimer);
    }

    private void PlayLoop(Sprite[] frames, float framesPerSecond, float time)
    {
        if (_spriteRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(time * Mathf.Max(0.01f, framesPerSecond)) % frames.Length;
        _spriteRenderer.sprite = frames[frameIndex];
    }

    private void Halt()
    {
        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }
    }

    private void DisableColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private IEnumerator DefeatRoutine()
    {
        const float Duration = 1f;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        SpawnDrop();
        Destroy(gameObject);
    }
}
