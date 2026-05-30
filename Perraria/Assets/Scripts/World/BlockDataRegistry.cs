using UnityEngine;

[CreateAssetMenu(fileName = "BlockDataRegistry", menuName = "Perraria/Block Data Registry")]
public sealed class BlockDataRegistry : ScriptableObject
{
    [System.Serializable]
    public struct BlockData
    {
        public BlockType Type;
        public float Hardness;
        public ItemData DropItem;
        [Range(0f, 1f)] public float DropChance;
    }

    [SerializeField] private BlockData[] _blocks;

    public float GetHardness(BlockType type)
    {
        if (type == BlockType.Air)
        {
            return 0f;
        }

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i].Type == type)
            {
                return _blocks[i].Hardness;
            }
        }

        return 0f;
    }

    public ItemData GetDropItem(BlockType type)
    {
        if (type == BlockType.Air)
        {
            return null;
        }

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i].Type == type)
            {
                return _blocks[i].DropItem;
            }
        }

        return null;
    }

    public float GetDropChance(BlockType type)
    {
        if (type == BlockType.Air)
        {
            return 1f;
        }

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i].Type == type)
            {
                float chance = _blocks[i].DropChance;
                return chance <= 0f ? 1f : Mathf.Clamp01(chance);
            }
        }

        return 1f;
    }
}
