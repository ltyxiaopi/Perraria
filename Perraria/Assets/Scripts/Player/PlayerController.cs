using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string MoveActionName = "Move";
    private const string JumpActionName = "Jump";

    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Rigidbody2D _rigidbody2D;
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private float _moveInput;
    private bool _jumpQueued;

    public bool IsGrounded { get; private set; }
    public bool FacingRight => _spriteRenderer == null || !_spriteRenderer.flipX;

    #region Unity Lifecycle

    private void Reset()
    {
        _groundLayer = LayerMask.GetMask("Ground");
        _groundCheckRadius = 0.2f;
        _moveSpeed = 6f;
        _jumpForce = 10f;

        if (_groundCheck == null)
        {
            var groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(transform, false);
                groundCheck.localPosition = new Vector3(0f, -0.85f, 0f);
            }

            _groundCheck = groundCheck;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        CacheInputActions();
    }

    private void OnEnable()
    {
        CacheInputActions();

        if (_playerInput != null && Application.isPlaying)
        {
            _playerInput.ActivateInput();
        }

        _moveAction?.Enable();
        _jumpAction?.Enable();

        if (_jumpAction != null)
        {
            _jumpAction.performed += OnJumpPerformed;
        }
    }

    private void OnDisable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.performed -= OnJumpPerformed;
        }

        _moveAction?.Disable();
        _jumpAction?.Disable();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        UpdateGroundedState();
        _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>().x : 0f;
        UpdateFacingDirection();
    }

    private void FixedUpdate()
    {
        var velocity = _rigidbody2D.linearVelocity;
        velocity.x = _moveInput * _moveSpeed;

        if (_jumpQueued && IsGrounded)
        {
            velocity.y = _jumpForce;
            IsGrounded = false;
        }

        _jumpQueued = false;
        _rigidbody2D.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }

    #endregion

    private void CacheInputActions()
    {
        if (_playerInput == null || _playerInput.actions == null)
        {
            return;
        }

        var actionMap = _playerInput.actions.FindActionMap(PlayerActionMapName, true);
        _moveAction = actionMap.FindAction(MoveActionName, true);
        _jumpAction = actionMap.FindAction(JumpActionName, true);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        _jumpQueued = true;
    }

    public void RestoreState(Vector3 position, bool facingRight)
    {
        transform.position = position;
        _moveInput = 0f;
        _jumpQueued = false;

        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = !facingRight;
        }
    }

    private void UpdateGroundedState()
    {
        if (_groundCheck == null)
        {
            IsGrounded = false;
            return;
        }

        IsGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
    }

    private void UpdateFacingDirection()
    {
        if (_spriteRenderer == null || Mathf.Abs(_moveInput) < 0.01f)
        {
            return;
        }

        _spriteRenderer.flipX = _moveInput < 0f;
    }
}
