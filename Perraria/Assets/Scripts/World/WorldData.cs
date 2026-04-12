public sealed class WorldData
{
    private readonly BlockType[,] _blocks;

    public int Width { get; }
    public int Height { get; }

    public WorldData(int width, int height)
    {
        Width = width;
        Height = height;
        _blocks = new BlockType[width, height];
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public BlockType GetBlock(int x, int y)
    {
        return InBounds(x, y) ? _blocks[x, y] : BlockType.Air;
    }

    public void SetBlock(int x, int y, BlockType type)
    {
        if (InBounds(x, y))
        {
            _blocks[x, y] = type;
        }
    }
}
