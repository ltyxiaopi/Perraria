using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class PlayerBlockInteraction : MonoBehaviour
{
    private const float CellBoundsPadding = 0.01f;

    [SerializeField] private TileManager _tileManager;
    [SerializeField] private BlockDataRegistry _blockDataRegistry;
    [SerializeField] private float _interactionRange = 5f;
    [SerializeField] private float _miningSpeed = 1f;
    [SerializeField] private Tilemap _highlightTilemap;
    [SerializeField] private TileBase _highlightTile;
    [SerializeField] private BlockType _placeBlockType = BlockType.Dirt;

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
        if (_tileManager.GetBlock(cellPosition) != BlockType.Air)
        {
            return;
        }

        if (IsPlayerOccupyingCell(cellPosition))
        {
            return;
        }

        _tileManager.SetBlock(cellPosition, _placeBlockType);
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
