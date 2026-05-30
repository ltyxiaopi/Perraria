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

    [Header("Trees")]
    [SerializeField] private float _treeSpawnChance = 0.08f;
    [SerializeField] private int _minTreeSpacing = 4;
    [SerializeField] private int _trunkHeightMin = 4;
    [SerializeField] private int _trunkHeightMax = 7;

    [Header("References")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileRegistry _tileRegistry;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private TileManager _tileManager;

    #endregion

    private WorldData _worldData;
    private float _seedX;
    private float _seedY;

    public WorldData WorldData => _worldData;
    public int HalfWidth => _worldWidth / 2;
    public int HalfHeight => _worldHeight / 2;
    public int Seed { get; private set; }

    #region Unity Lifecycle

    private void Start()
    {
        GenerateWorld();
    }

    #endregion

    #region Generation Pipeline

    private void GenerateWorld()
    {
        GenerateWorldWithSeed(Random.Range(int.MinValue, int.MaxValue));
    }

    public void GenerateWorldWithSeed(int seed)
    {
        Seed = seed;
        Random.InitState(Seed);
        _seedX = Random.Range(0f, 10000f);
        _seedY = Random.Range(0f, 10000f);

        if (_tilemap != null)
        {
            _tilemap.ClearAllTiles();
        }

        _worldData = new WorldData(_worldWidth, _worldHeight);

        int[] surfaceHeights = GenerateSurfaceHeights();
        FillTerrain(surfaceHeights);
        CarveCaves(surfaceHeights);
        PlantTrees(surfaceHeights);
        RenderToTilemap();
        SpawnPlayer(surfaceHeights);
        InitializeTileManager();
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

    private void PlantTrees(int[] surfaceHeights)
    {
        int lastTreeX = -_minTreeSpacing;

        for (int x = 0; x < _worldWidth; x++)
        {
            if (x - lastTreeX < _minTreeSpacing)
            {
                continue;
            }

            if (Random.value >= _treeSpawnChance)
            {
                continue;
            }

            int trunkHeight = Random.Range(_trunkHeightMin, _trunkHeightMax + 1);
            if (!CanPlantTree(x, surfaceHeights[x], trunkHeight))
            {
                continue;
            }

            PlantTree(x, surfaceHeights[x], trunkHeight);
            lastTreeX = x;
        }
    }

    private bool CanPlantTree(int x, int surfaceY, int trunkHeight)
    {
        if (_worldData.GetBlock(x, surfaceY) != BlockType.Grass)
        {
            return false;
        }

        int topY = surfaceY + trunkHeight;
        if (x <= 0 || x >= _worldWidth - 1 || surfaceY + 1 >= _worldHeight || topY + 1 >= _worldHeight)
        {
            return false;
        }

        for (int y = surfaceY + 1; y <= topY; y++)
        {
            if (_worldData.GetBlock(x, y) != BlockType.Air)
            {
                return false;
            }
        }

        for (int leafY = topY - 2; leafY <= topY; leafY++)
        {
            for (int leafX = x - 1; leafX <= x + 1; leafX++)
            {
                if (leafX == x && leafY <= topY)
                {
                    continue;
                }

                if (_worldData.GetBlock(leafX, leafY) != BlockType.Air)
                {
                    return false;
                }
            }
        }

        return _worldData.GetBlock(x, topY + 1) == BlockType.Air;
    }

    private void PlantTree(int x, int surfaceY, int trunkHeight)
    {
        int topY = surfaceY + trunkHeight;

        for (int y = surfaceY + 1; y <= topY; y++)
        {
            _worldData.SetBlock(x, y, BlockType.Wood);
        }

        for (int leafY = topY - 2; leafY <= topY; leafY++)
        {
            for (int leafX = x - 1; leafX <= x + 1; leafX++)
            {
                if (_worldData.GetBlock(leafX, leafY) == BlockType.Air)
                {
                    _worldData.SetBlock(leafX, leafY, BlockType.Leaves);
                }
            }
        }

        _worldData.SetBlock(x, topY + 1, BlockType.Leaves);
    }

    #endregion

    #region Runtime Initialization

    private void InitializeTileManager()
    {
        if (_tileManager == null)
        {
            _tileManager = GetComponent<TileManager>();
        }

        if (_tileManager != null)
        {
            _tileManager.Initialize(_worldData, HalfWidth, HalfHeight);
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
        float worldY = surfaceY - halfHeight + 3.5f;

        _playerTransform.position = new Vector3(worldX, worldY, 0f);
    }

    #endregion
}
