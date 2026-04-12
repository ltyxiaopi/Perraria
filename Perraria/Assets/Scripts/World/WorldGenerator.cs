using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class WorldGenerator : MonoBehaviour
{
    #region Inspector Fields

    [Header("World Size")]
    [SerializeField] private int _worldWidth = 400;
    [SerializeField] private int _worldHeight = 200;

    [Header("Terrain Shape")]
    [SerializeField] private float _surfacePerlinScale = 0.05f;
    [SerializeField] private int _surfaceHeightMin = 120;
    [SerializeField] private int _surfaceHeightMax = 160;
    [SerializeField] private int _dirtLayerDepth = 10;

    [Header("Caves")]
    [SerializeField] private float _cavePerlinScale = 0.08f;
    [SerializeField] private float _caveThreshold = 0.42f;
    [SerializeField] private int _caveSurfaceGuard = 3;

    [Header("References")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileRegistry _tileRegistry;
    [SerializeField] private Transform _playerTransform;

    #endregion

    private WorldData _worldData;
    private float _seedX;
    private float _seedY;

    public WorldData WorldData => _worldData;

    #region Unity Lifecycle

    private void Start()
    {
        GenerateWorld();
    }

    #endregion

    #region Generation Pipeline

    private void GenerateWorld()
    {
        _seedX = Random.Range(0f, 10000f);
        _seedY = Random.Range(0f, 10000f);

        _worldData = new WorldData(_worldWidth, _worldHeight);

        int[] surfaceHeights = GenerateSurfaceHeights();
        FillTerrain(surfaceHeights);
        CarveCaves(surfaceHeights);
        RenderToTilemap();
        SpawnPlayer(surfaceHeights);
    }

    private int[] GenerateSurfaceHeights()
    {
        int[] heights = new int[_worldWidth];

        for (int x = 0; x < _worldWidth; x++)
        {
            float noise = Mathf.PerlinNoise(x * _surfacePerlinScale + _seedX, _seedY);
            heights[x] = Mathf.RoundToInt(Mathf.Lerp(_surfaceHeightMin, _surfaceHeightMax, noise));
        }

        return heights;
    }

    private void FillTerrain(int[] surfaceHeights)
    {
        for (int x = 0; x < _worldWidth; x++)
        {
            int surfaceY = surfaceHeights[x];

            for (int y = 0; y < _worldHeight; y++)
            {
                BlockType block = DetermineBlockType(y, surfaceY);
                _worldData.SetBlock(x, y, block);
            }
        }
    }

    private BlockType DetermineBlockType(int y, int surfaceY)
    {
        if (y > surfaceY)
            return BlockType.Air;
        if (y == surfaceY)
            return BlockType.Grass;
        if (y > surfaceY - _dirtLayerDepth)
            return BlockType.Dirt;

        return BlockType.Stone;
    }

    private void CarveCaves(int[] surfaceHeights)
    {
        for (int x = 0; x < _worldWidth; x++)
        {
            int caveTop = surfaceHeights[x] - _caveSurfaceGuard;

            for (int y = 0; y < caveTop; y++)
            {
                if (_worldData.GetBlock(x, y) == BlockType.Air)
                    continue;

                float noise = Mathf.PerlinNoise(
                    x * _cavePerlinScale + _seedX + 500f,
                    y * _cavePerlinScale + _seedY + 500f);

                if (noise < _caveThreshold)
                {
                    _worldData.SetBlock(x, y, BlockType.Air);
                }
            }
        }
    }

    #endregion

    #region Tilemap Rendering

    private void RenderToTilemap()
    {
        int totalTiles = _worldWidth * _worldHeight;
        var positions = new Vector3Int[totalTiles];
        var tiles = new TileBase[totalTiles];

        int halfWidth = _worldWidth / 2;
        int halfHeight = _worldHeight / 2;
        int index = 0;

        for (int x = 0; x < _worldWidth; x++)
        {
            for (int y = 0; y < _worldHeight; y++)
            {
                positions[index] = new Vector3Int(x - halfWidth, y - halfHeight, 0);
                tiles[index] = _tileRegistry.GetTile(_worldData.GetBlock(x, y));
                index++;
            }
        }

        _tilemap.SetTiles(positions, tiles);
    }

    #endregion

    #region Player Spawn

    private void SpawnPlayer(int[] surfaceHeights)
    {
        if (_playerTransform == null)
            return;

        int spawnX = _worldWidth / 2;
        int surfaceY = surfaceHeights[spawnX];

        int halfWidth = _worldWidth / 2;
        int halfHeight = _worldHeight / 2;

        float worldX = spawnX - halfWidth + 0.5f;
        float worldY = surfaceY - halfHeight + 1.5f;

        _playerTransform.position = new Vector3(worldX, worldY, 0f);
    }

    #endregion
}
