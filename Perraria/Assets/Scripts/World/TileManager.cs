using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class TileManager : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileRegistry _tileRegistry;

    private WorldData _worldData;
    private int _halfWidth;
    private int _halfHeight;

    public void Initialize(WorldData worldData, int halfWidth, int halfHeight)
    {
        _worldData = worldData;
        _halfWidth = halfWidth;
        _halfHeight = halfHeight;
    }

    public BlockType GetBlock(Vector3Int tilemapPos)
    {
        if (_worldData == null)
        {
            return BlockType.Air;
        }

        Vector2Int worldDataPos = ToWorldDataPosition(tilemapPos);
        return _worldData.GetBlock(worldDataPos.x, worldDataPos.y);
    }

    public bool SetBlock(Vector3Int tilemapPos, BlockType type)
    {
        if (_worldData == null || _tilemap == null || _tileRegistry == null)
        {
            return false;
        }

        Vector2Int worldDataPos = ToWorldDataPosition(tilemapPos);
        if (!_worldData.InBounds(worldDataPos.x, worldDataPos.y))
        {
            return false;
        }

        _worldData.SetBlock(worldDataPos.x, worldDataPos.y, type);
        _tilemap.SetTile(tilemapPos, _tileRegistry.GetTile(type));
        return true;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return _tilemap != null ? _tilemap.WorldToCell(worldPosition) : Vector3Int.zero;
    }

    public void SetTileColor(Vector3Int tilemapPos, Color color)
    {
        if (_tilemap != null)
        {
            _tilemap.SetTileFlags(tilemapPos, TileFlags.None);
            _tilemap.SetColor(tilemapPos, color);
        }
    }

    public void ResetTileColor(Vector3Int tilemapPos)
    {
        if (_tilemap != null)
        {
            _tilemap.SetColor(tilemapPos, Color.white);
        }
    }

    public Sprite GetBlockSprite(Vector3Int tilemapPos)
    {
        return _tilemap != null ? _tilemap.GetSprite(tilemapPos) : null;
    }

    private Vector2Int ToWorldDataPosition(Vector3Int tilemapPos)
    {
        return new Vector2Int(tilemapPos.x + _halfWidth, tilemapPos.y + _halfHeight);
    }
}
