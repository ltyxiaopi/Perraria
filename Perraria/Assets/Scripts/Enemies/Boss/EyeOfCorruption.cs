using System.Collections;
using UnityEngine;
using UnityHFSM;

public sealed class EyeOfCorruption : Enemy
{
    private const string AliveState = "Alive";
    private const string DefeatedState = "Defeated";
    private const string Phase1State = "Phase1";
    private const string Phase2State = "Phase2";
    private const string HoverState = "Hover";
    private const string ChargeState = "Charge";
    private const string DripAcidState = "DripAcid";
    private const string Phase2HoverPath = "/Alive/Phase2/Hover";
    private const string Phase2DripAcidPath = "/Alive/Phase2/DripAcid";

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

    private StateMachine _fsm;
    private Vector2 _chargeDirection;
    private float _phaseTimer;
    private float _chargeTimer;
    private float _projectileTimer;
    private float _dripTimer;
    private bool _firedThisHover;
    private Coroutine _defeatCoroutine;

    public string DebugPhaseName => _fsm?.GetActiveHierarchyPath() ?? "<uninit>";

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

        BuildStateMachine();
    }

    protected override void UpdateBehavior()
    {
        if (_playerTransform == null || _state != EnemyState.Chasing)
        {
            Halt();
            PlayHoverAnimation();
            return;
        }

        _fsm.OnLogic();
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

        _fsm.RequestStateChange(DefeatedState, forceInstantly: true);
    }

    private void BuildStateMachine()
    {
        _fsm = new StateMachine();
        HybridStateMachine alive = new HybridStateMachine();
        HybridStateMachine phase1 = new HybridStateMachine();
        HybridStateMachine phase2 = new HybridStateMachine();

        ConfigurePhase1(phase1);
        ConfigurePhase2(phase2);
        ConfigureAlive(alive, phase1, phase2);

        _fsm.AddState(AliveState, alive);
        _fsm.AddState(DefeatedState, new State(onEnter: state => EnterDefeated()));
        _fsm.SetStartState(AliveState);
        _fsm.Init();
    }

    private void ConfigurePhase1(HybridStateMachine phase1)
    {
        phase1.AddState(HoverState, new State(
            onEnter: state => _phaseTimer = 0f,
            onLogic: state =>
            {
                MoveTowardHoverPoint();
                _phaseTimer += Time.deltaTime;
                PlayLoop(_idleFrames, _idleFps, _phaseTimer);
            }));
        phase1.AddState(ChargeState, new State(
            onEnter: state => EnterCharge(),
            onLogic: state => UpdateCharge(_phase1ChargeSpeed),
            onExit: state => Halt()));
        phase1.AddTransition(HoverState, ChargeState, state => _phaseTimer >= _phase1HoverDuration);
        phase1.AddTransition(ChargeState, HoverState, state => _chargeTimer >= _chargeDuration);
        phase1.SetStartState(HoverState);
    }

    private void ConfigurePhase2(HybridStateMachine phase2)
    {
        phase2.AddState(HoverState, new State(onLogic: state =>
        {
            MoveTowardHoverPoint();
            _phaseTimer += Time.deltaTime;
            _projectileTimer += Time.deltaTime;
            PlayLoop(_detectFrames, _detectFps, _phaseTimer);
        }));
        phase2.AddState(DripAcidState, new State(
            onEnter: state => EnterDripAcid(),
            onLogic: state => UpdateDripAcid(),
            onExit: state => ExitDripAcid()));
        phase2.AddState(ChargeState, new State(
            onEnter: state => EnterCharge(),
            onLogic: state => UpdateCharge(_phase2ChargeSpeed),
            onExit: state => ExitPhase2Charge()));
        phase2.AddTransition(HoverState, DripAcidState, state => CanStartDripAcid());
        phase2.AddTransition(DripAcidState, HoverState, state => _dripTimer >= GetDripDuration());
        phase2.AddTransition(HoverState, ChargeState, state => _phaseTimer >= _phase2HoverDuration);
        phase2.AddTransition(ChargeState, HoverState, state => _chargeTimer >= _chargeDuration);
        phase2.SetStartState(HoverState);
    }

    private void ConfigureAlive(HybridStateMachine alive, HybridStateMachine phase1, HybridStateMachine phase2)
    {
        alive.AddState(Phase1State, phase1);
        alive.AddState(Phase2State, phase2);
        alive.AddTransition(
            Phase1State,
            Phase2State,
            state => _currentHealth <= Mathf.FloorToInt(_maxHealth * 0.5f),
            onTransition: state => EnterPhase2());
        alive.SetStartState(Phase1State);
    }

    private void MoveTowardHoverPoint()
    {
        float sway = Mathf.Sin(Time.time * _hoverSwayFrequency * Mathf.PI * 2f) * _hoverSwayAmplitude;
        Vector3 target = _playerTransform.position + new Vector3(sway, _hoverHeight, 0f);
        Vector2 toTarget = target - transform.position;
        Vector2 velocity = Vector2.ClampMagnitude(toTarget * _hoverFollowSpeed, _hoverFollowSpeed);
        _rigidbody2D.linearVelocity = velocity;
    }

    private void EnterCharge()
    {
        Vector2 toPlayer = _playerTransform.position - transform.position;
        _chargeDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.down;
        _chargeTimer = 0f;
    }

    private void UpdateCharge(float speed)
    {
        _rigidbody2D.linearVelocity = _chargeDirection * speed;
        _chargeTimer += Time.deltaTime;
        PlayLoop(_detectFrames, _detectFps, _chargeTimer);
    }

    private void EnterPhase2()
    {
        _firedThisHover = false;
        _projectileTimer = 0f;
        _phaseTimer = 0f;
        Halt();
        Debug.Log($"EyeOfCorruption phase switch: HP {_currentHealth}/{_maxHealth}", this);
    }

    private void EnterDripAcid()
    {
        _projectileTimer = 0f;
        _dripTimer = 0f;
    }

    private void UpdateDripAcid()
    {
        _phaseTimer += Time.deltaTime;
        _dripTimer += Time.deltaTime;
        PlayLoop(_dripAcidFrames, _dripAcidFps, _dripTimer);
    }

    private void ExitDripAcid()
    {
        if (!IsDead && _dripTimer >= GetDripDuration())
        {
            FireAcidProjectile();
            _firedThisHover = true;
        }
    }

    private void ExitPhase2Charge()
    {
        _firedThisHover = false;
        _projectileTimer = 0f;
        _phaseTimer = 0f;
        Halt();
    }

    private void EnterDefeated()
    {
        Halt();
        DisableColliders();
        if (_defeatCoroutine == null)
        {
            _defeatCoroutine = StartCoroutine(DefeatRoutine());
        }
    }

    private bool CanStartDripAcid()
    {
        if (_firedThisHover || _dripAcidFrames == null || _dripAcidFrames.Length == 0)
        {
            return false;
        }

        float prefireStartTime = Mathf.Max(0f, _phase2ProjectileInterval - GetDripDuration());
        return _projectileTimer >= prefireStartTime;
    }

    private float GetDripDuration()
    {
        if (_dripAcidFrames == null || _dripAcidFrames.Length == 0)
        {
            return 0f;
        }

        return _dripAcidFrames.Length / Mathf.Max(0.01f, _dripAcidFps);
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
        bool phase2HoverFlow = DebugPhaseName == Phase2HoverPath || DebugPhaseName == Phase2DripAcidPath;
        Sprite[] frames = phase2HoverFlow ? _detectFrames : _idleFrames;
        float fps = phase2HoverFlow ? _detectFps : _idleFps;
        PlayLoop(frames, fps, Time.time);
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
