using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class PlayerBlockInteraction : MonoBehaviour
{
    private const float CellBoundsPadding = 0.01f;

    [SerializeField] private TileManager _tileManager;
    [SerializeField] private BlockDataRegistry _blockDataRegistry;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemDrop _itemDropPrefab;
    [SerializeField] private float _interactionRange = 5f;
    [SerializeField] private float _miningSpeed = 1f;
    [SerializeField] private Tilemap _highlightTilemap;
    [SerializeField] private TileBase _highlightTile;

    private Camera _mainCamera;
    private Collider2D _playerCollider;
    private bool _isMining;
    private Vector3Int _miningCell;
    private float _miningProgress;
    private float _miningDuration;
    private Vector3Int _highlightedCell;
    private bool _hasHighlight;

    #region Unity Lifecycle

    private void Awake()
    {
        _mainCamera = Camera.main;
        _playerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        ResetMining();
        ClearHighlight();
    }

    private void Update()
    {
        if (_tileManager == null || _blockDataRegistry == null || _mainCamera == null)
        {
            ResetMining();
            ClearHighlight();
            return;
        }

        HandleHotbarInput();

        bool isPointerOverUI = EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
        if (isPointerOverUI)
        {
            ResetMining();
            ClearHighlight();
            return;
        }

        Vector3Int targetCell = GetMouseTargetCell();
        bool isInRange = IsCellInInteractionRange(targetCell);
        UpdateHighlight(targetCell, isInRange);
        UpdateMining(targetCell, isInRange);

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && isInRange)
        {
            PlaceBlock(targetCell);
        }
    }

    #endregion

    #region Private Methods

    private Vector3Int GetMouseTargetCell()
    {
        Vector2 mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        return _tileManager.WorldToCell(worldPosition);
    }

    private bool IsCellInInteractionRange(Vector3Int cellPosition)
    {
        Vector3 cellCenter = GetCellCenter(cellPosition);
        return Vector2.Distance(transform.position, cellCenter) <= _interactionRange;
    }

    private Vector3 GetCellCenter(Vector3Int cellPosition)
    {
        if (_highlightTilemap != null)
        {
            return _highlightTilemap.GetCellCenterWorld(cellPosition);
        }

        return new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, 0f);
    }

    private void UpdateMining(Vector3Int cellPosition, bool isInRange)
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            ResetMining();
            return;
        }

        BlockType blockType = _tileManager.GetBlock(cellPosition);
        if (!isInRange || blockType == BlockType.Air)
        {
            ResetMining();
            return;
        }

        if (!_isMining || _miningCell != cellPosition)
        {
            StartMining(cellPosition, blockType);
            return;
        }

        ContinueMining();
    }

    private void StartMining(Vector3Int cellPosition, BlockType blockType)
    {
        ResetMining();

        float hardness = _blockDataRegistry.GetHardness(blockType);
        if (hardness <= 0f || _miningSpeed <= 0f)
        {
            return;
        }

        _isMining = true;
        _miningCell = cellPosition;
        _miningProgress = 0f;
        _miningDuration = hardness / _miningSpeed;
        ClearHighlight();
    }

    private void ContinueMining()
    {
        if (_miningDuration <= 0f)
        {
            ResetMining();
            return;
        }

        _miningProgress += Time.deltaTime / _miningDuration;
        float alpha = 1f - _miningProgress;
        _tileManager.SetTileColor(_miningCell, new Color(1f, 1f, 1f, alpha));

        if (_miningProgress >= 1f)
        {
            Vector3Int minedCell = _miningCell;
            ResetMining();
            SpawnItemDrop(minedCell);
            _tileManager.SetBlock(minedCell, BlockType.Air);
        }
    }

    private void ResetMining()
    {
        if (_isMining)
        {
            _tileManager.ResetTileColor(_miningCell);
        }

        _isMining = false;
        _miningCell = Vector3Int.zero;
        _miningProgress = 0f;
        _miningDuration = 0f;
    }

    private void PlaceBlock(Vector3Int cellPosition)
    {
        if (_inventory == null || _tileManager.GetBlock(cellPosition) != BlockType.Air)
        {
            return;
        }

        ItemStack selected = _inventory.GetSelectedItem();
        if (selected.IsEmpty
            || selected.Item.Type != ItemType.Block
            || selected.Item.PlaceBlockType == BlockType.Air)
        {
            return;
        }

        if (IsPlayerOccupyingCell(cellPosition))
        {
            return;
        }

        if (!_tileManager.SetBlock(cellPosition, selected.Item.PlaceBlockType))
        {
            return;
        }

        _inventory.RemoveFromSlot(_inventory.SelectedHotbarIndex, 1);
    }

    private void HandleHotbarInput()
    {
        if (_inventory == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && TrySelectHotbarFromKeyboard(keyboard))
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scrollY = mouse.scroll.ReadValue().y;
        if (scrollY > 0f)
        {
            CycleHotbar(-1);
        }
        else if (scrollY < 0f)
        {
            CycleHotbar(1);
        }
    }

    private bool TrySelectHotbarFromKeyboard(Keyboard keyboard)
    {
        return TrySelectHotbarKey(keyboard.digit1Key, 0)
            || TrySelectHotbarKey(keyboard.digit2Key, 1)
            || TrySelectHotbarKey(keyboard.digit3Key, 2)
            || TrySelectHotbarKey(keyboard.digit4Key, 3)
            || TrySelectHotbarKey(keyboard.digit5Key, 4)
            || TrySelectHotbarKey(keyboard.digit6Key, 5)
            || TrySelectHotbarKey(keyboard.digit7Key, 6)
            || TrySelectHotbarKey(keyboard.digit8Key, 7)
            || TrySelectHotbarKey(keyboard.digit9Key, 8)
            || TrySelectHotbarKey(keyboard.digit0Key, 9);
    }

    private bool TrySelectHotbarKey(KeyControl key, int hotbarIndex)
    {
        if (key == null || !key.wasPressedThisFrame)
        {
            return false;
        }

        _inventory.SelectHotbar(hotbarIndex);
        return true;
    }

    private void CycleHotbar(int offset)
    {
        int nextIndex = _inventory.SelectedHotbarIndex + offset;
        if (nextIndex < 0)
        {
            nextIndex = Inventory.HotbarSlots - 1;
        }
        else if (nextIndex >= Inventory.HotbarSlots)
        {
            nextIndex = 0;
        }

        _inventory.SelectHotbar(nextIndex);
    }

    private void SpawnItemDrop(Vector3Int minedCell)
    {
        if (_itemDropPrefab == null)
        {
            return;
        }

        BlockType minedType = _tileManager.GetBlock(minedCell);
        ItemData dropItem = _blockDataRegistry.GetDropItem(minedType);
        if (dropItem == null)
        {
            return;
        }

        Vector3 spawnPosition = GetCellCenter(minedCell);
        ItemDrop drop = Instantiate(_itemDropPrefab, spawnPosition, Quaternion.identity);
        drop.Initialize(dropItem, 1);
    }

    private bool IsPlayerOccupyingCell(Vector3Int cellPosition)
    {
        if (_playerCollider == null)
        {
            return false;
        }

        Bounds bounds = _playerCollider.bounds;
        Vector3 min = new Vector3(
            bounds.min.x + CellBoundsPadding,
            bounds.min.y + CellBoundsPadding,
            0f);
        Vector3 max = new Vector3(
            bounds.max.x - CellBoundsPadding,
            bounds.max.y - CellBoundsPadding,
            0f);

        Vector3Int minCell = _tileManager.WorldToCell(min);
        Vector3Int maxCell = _tileManager.WorldToCell(max);

        return cellPosition.x >= minCell.x
            && cellPosition.x <= maxCell.x
            && cellPosition.y >= minCell.y
            && cellPosition.y <= maxCell.y;
    }

    private void UpdateHighlight(Vector3Int cellPosition, bool shouldShow)
    {
        if (_highlightTilemap == null || _highlightTile == null)
        {
            return;
        }

        bool shouldHideForMining = _isMining && _miningCell == cellPosition;
        if (!shouldShow || shouldHideForMining)
        {
            ClearHighlight();
            return;
        }

        if (_hasHighlight && _highlightedCell == cellPosition)
        {
            return;
        }

        ClearHighlight();
        _highlightTilemap.SetTile(cellPosition, _highlightTile);
        _highlightedCell = cellPosition;
        _hasHighlight = true;
    }

    private void ClearHighlight()
    {
        if (_highlightTilemap == null || !_hasHighlight)
        {
            return;
        }

        _highlightTilemap.SetTile(_highlightedCell, null);
        _hasHighlight = false;
    }

    #endregion
}
