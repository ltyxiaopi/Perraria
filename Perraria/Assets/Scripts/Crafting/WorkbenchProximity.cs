using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorkbenchProximity : MonoBehaviour
{
    [SerializeField] private TileManager _tileManager;
    [SerializeField] private Transform _player;
    [SerializeField] private int _radius = 4;

    public bool IsNearWorkbench()
    {
        return IsNearBlock(BlockType.Workbench);
    }

    public bool IsNearStation(ItemData stationItem)
    {
        if (stationItem == null || stationItem.PlaceBlockType == BlockType.Air)
        {
            return false;
        }

        return IsNearBlock(stationItem.PlaceBlockType);
    }

    private bool IsNearBlock(BlockType blockType)
    {
        if (_tileManager == null || _player == null || blockType == BlockType.Air)
        {
            return false;
        }

        Vector3Int center = _tileManager.WorldToCell(_player.position);
        int radius = Mathf.Max(0, _radius);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector3Int cell = new Vector3Int(center.x + x, center.y + y, center.z);
                if (_tileManager.GetBlock(cell) == blockType)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
