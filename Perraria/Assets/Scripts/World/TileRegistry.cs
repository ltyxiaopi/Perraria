using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileRegistry", menuName = "Perraria/Tile Registry")]
public sealed class TileRegistry : ScriptableObject
{
    [SerializeField] private TileBase _dirtTile;
    [SerializeField] private TileBase _grassTile;
    [SerializeField] private TileBase _stoneTile;

    public TileBase GetTile(BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Dirt => _dirtTile,
            BlockType.Grass => _grassTile,
            BlockType.Stone => _stoneTile,
            _ => null
        };
    }
}
