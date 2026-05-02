using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerCombat : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string AttackActionName = "Attack";
    private const float WeaponFacingOffsetDegrees = -90f;
    private const float AngleEpsilon = 0.1f;

    [Header("Refs")]
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Inventory _inventory;

    [Header("Combat")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _attackSpeedMultiplier = 1f;
    [SerializeField] private float _damageMultiplier = 1f;

    [Header("Pivot")]
    [SerializeField] private float _pivotOffsetX = 0.2f;

    private readonly HashSet<Enemy> _hitEnemiesThisSwing = new();

    private PlayerInput _playerInput;
    private InputAction _attackAction;
    private SpriteRenderer _playerSpriteRenderer;
    private Coroutine _swingCoroutine;
    private bool _isPointerOverUi;

    public bool IsSwinging => _swingCoroutine != null;

    private void Reset()
    {
        _inventory = GetComponent<Inventory>();
        _playerInput = GetComponent<PlayerInput>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
        _enemyLayer = LayerMask.GetMask("Enemy");

        if (_weaponPivot == null)
        {
            _weaponPivot = transform.Find("WeaponPivot");
        }

        if (_weaponRenderer == null && _weaponPivot != null)
        {
            Transform weapon = _weaponPivot.Find("Weapon");
            if (weapon != null)
            {
                _weaponRenderer = weapon.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void Awake()
    {
        _inventory = _inventory != null ? _inventory : GetComponent<Inventory>();
        _playerInput = GetComponent<PlayerInput>();
        _playerSpriteRenderer = _playerSpriteRenderer != null
            ? _playerSpriteRenderer
            : GetComponent<SpriteRenderer>();

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        CacheInputAction();
        RefreshWeaponRenderer();
    }

    private void OnEnable()
    {
        CacheInputAction();

        if (_playerInput != null)
        {
            _playerInput.onActionTriggered += HandleActionTriggered;
        }

        if (_inventory != null)
        {
            _inventory.OnSelectedHotbarChanged += HandleSelectedHotbarChanged;
            _inventory.OnSlotChanged += HandleSlotChanged;
        }

        RefreshWeaponRenderer();
    }

    private void OnDisable()
    {
        if (_playerInput != null)
        {
            _playerInput.onActionTriggered -= HandleActionTriggered;
        }

        if (_inventory != null)
        {
            _inventory.OnSelectedHotbarChanged -= HandleSelectedHotbarChanged;
            _inventory.OnSlotChanged -= HandleSlotChanged;
        }

        if (_swingCoroutine != null)
        {
            StopCoroutine(_swingCoroutine);
            _swingCoroutine = null;
        }

        _hitEnemiesThisSwing.Clear();

        if (_weaponPivot != null)
        {
            _weaponPivot.localRotation = Quaternion.identity;
        }

        if (_weaponRenderer != null)
        {
            _weaponRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        _isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        UpdatePivotPosition();
        RefreshWeaponSorting();
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || IsSwinging || _isPointerOverUi)
        {
            return;
        }

        if (!TryGetSelectedWeapon(out ItemData weaponItem))
        {
            return;
        }

        if (_weaponPivot == null || _weaponRenderer == null || _mainCamera == null)
        {
            return;
        }

        _swingCoroutine = StartCoroutine(SwingRoutine(weaponItem));
    }

    private IEnumerator SwingRoutine(ItemData weaponItem)
    {
        _hitEnemiesThisSwing.Clear();
        _weaponRenderer.enabled = true;
        _weaponRenderer.sprite = weaponItem.Icon;

        float attackSpeed = Mathf.Max(0.01f, _attackSpeedMultiplier);
        float duration = Mathf.Max(0.01f, weaponItem.SwingDuration / attackSpeed);
        float halfArc = Mathf.Max(0f, weaponItem.SwingArcDegrees * 0.5f);

        Vector2 aimDirection = GetAimDirection();
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        bool facingLeft = _playerSpriteRenderer != null && _playerSpriteRenderer.flipX;
        float startAngleOffset = facingLeft ? -halfArc : halfArc;
        float endAngleOffset = facingLeft ? halfArc : -halfArc;
        Quaternion startRotation = Quaternion.AngleAxis(aimAngle + startAngleOffset + WeaponFacingOffsetDegrees, Vector3.forward);
        Quaternion endRotation = Quaternion.AngleAxis(aimAngle + endAngleOffset + WeaponFacingOffsetDegrees, Vector3.forward);

        _weaponPivot.localRotation = startRotation;

        float previousAngle = startAngleOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentAngle = Mathf.Lerp(startAngleOffset, endAngleOffset, t);
            _weaponPivot.localRotation = Quaternion.Slerp(startRotation, endRotation, t);

            EvaluateHits(weaponItem, aimDirection, previousAngle, currentAngle);
            previousAngle = currentAngle;

            yield return null;
        }

        _weaponPivot.localRotation = Quaternion.identity;
        _hitEnemiesThisSwing.Clear();
        _swingCoroutine = null;
        RefreshWeaponRenderer();
    }

    private void EvaluateHits(ItemData weaponItem, Vector2 aimDirection, float previousAngle, float currentAngle)
    {
        float range = Mathf.Max(0f, weaponItem.WeaponRange);
        if (range <= 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, _enemyLayer);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        float minAngle = Mathf.Min(previousAngle, currentAngle) - AngleEpsilon;
        float maxAngle = Mathf.Max(previousAngle, currentAngle) + AngleEpsilon;
        float halfArc = Mathf.Max(0f, weaponItem.SwingArcDegrees * 0.5f) + AngleEpsilon;
        int damage = Mathf.RoundToInt(weaponItem.WeaponDamage * Mathf.Max(0f, _damageMultiplier));

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (_hitEnemiesThisSwing.Contains(enemy))
            {
                continue;
            }

            Vector2 toEnemy = enemy.transform.position - transform.position;
            if (toEnemy.sqrMagnitude <= 0.0001f)
            {
                toEnemy = aimDirection;
            }

            float signedAngle = Vector2.SignedAngle(aimDirection, toEnemy.normalized);
            if (Mathf.Abs(signedAngle) > halfArc || signedAngle < minAngle || signedAngle > maxAngle)
            {
                continue;
            }

            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - transform.position)).normalized;
            if (knockbackDirection.sqrMagnitude <= 0.0001f)
            {
                knockbackDirection = aimDirection;
            }

            enemy.TakeDamage(damage, knockbackDirection, weaponItem.KnockbackForce);
            _hitEnemiesThisSwing.Add(enemy);
        }
    }

    private Vector2 GetAimDirection()
    {
        if (_mainCamera == null || Mouse.current == null)
        {
            return GetFacingDirection();
        }

        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
        Vector2 direction = worldPosition - transform.position;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFacingDirection();
    }

    private Vector2 GetFacingDirection()
    {
        bool facingLeft = _playerSpriteRenderer != null && _playerSpriteRenderer.flipX;
        return facingLeft ? Vector2.left : Vector2.right;
    }

    private void CacheInputAction()
    {
        if (_playerInput == null || _playerInput.actions == null)
        {
            return;
        }

        InputActionMap playerMap = _playerInput.actions.FindActionMap(PlayerActionMapName, true);
        _attackAction = playerMap.FindAction(AttackActionName, true);
    }

    private void HandleActionTriggered(InputAction.CallbackContext context)
    {
        if (_attackAction == null || context.action != _attackAction)
        {
            return;
        }

        OnAttack(context);
    }

    private void HandleSelectedHotbarChanged(int _)
    {
        if (!IsSwinging)
        {
            RefreshWeaponRenderer();
        }
    }

    private void HandleSlotChanged(int slotIndex)
    {
        if (_inventory == null || slotIndex != _inventory.SelectedHotbarIndex || IsSwinging)
        {
            return;
        }

        RefreshWeaponRenderer();
    }

    private void RefreshWeaponRenderer()
    {
        if (_weaponRenderer == null)
        {
            return;
        }

        RefreshWeaponSorting();

        if (TryGetSelectedWeapon(out ItemData weaponItem))
        {
            _weaponRenderer.sprite = weaponItem.Icon;
            _weaponRenderer.enabled = true;
            return;
        }

        _weaponRenderer.sprite = null;
        _weaponRenderer.enabled = false;
    }

    private void RefreshWeaponSorting()
    {
        if (_weaponRenderer == null || _playerSpriteRenderer == null)
        {
            return;
        }

        _weaponRenderer.sortingLayerID = _playerSpriteRenderer.sortingLayerID;
        _weaponRenderer.sortingOrder = _playerSpriteRenderer.sortingOrder + 1;
    }

    private void UpdatePivotPosition()
    {
        if (_weaponPivot == null)
        {
            return;
        }

        float direction = _playerSpriteRenderer != null && _playerSpriteRenderer.flipX ? -1f : 1f;
        _weaponPivot.localPosition = new Vector3(_pivotOffsetX * direction, 0f, 0f);
    }

    private bool TryGetSelectedWeapon(out ItemData weaponItem)
    {
        weaponItem = null;
        if (_inventory == null)
        {
            return false;
        }

        ItemStack selected = _inventory.GetSelectedItem();
        if (selected.IsEmpty || selected.Item.Type != ItemType.Weapon)
        {
            return false;
        }

        weaponItem = selected.Item;
        return true;
    }
}
