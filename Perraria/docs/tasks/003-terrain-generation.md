# 任务 003 - 地形生成系统 + 摄像机跟随

## 目标
实现 Perlin Noise 程序化地形生成，用 Unity Tilemap 渲染，并添加平滑摄像机跟随。

## 接口签名
```csharp
// 方块类型
public enum BlockType : byte { Air = 0, Dirt = 1, Grass = 2, Stone = 3 }

// 世界数据（纯 C# 类）
public sealed class WorldData
{
    public int Width { get; }
    public int Height { get; }
    public WorldData(int width, int height);
    public BlockType GetBlock(int x, int y);
    public void SetBlock(int x, int y, BlockType type);
    public bool InBounds(int x, int y);
}

// 方块→瓦片映射（ScriptableObject）
[CreateAssetMenu(fileName = "TileRegistry", menuName = "Perraria/Tile Registry")]
public sealed class TileRegistry : ScriptableObject
{
    public TileBase GetTile(BlockType blockType);
}

// 世界生成器（MonoBehaviour）
[DisallowMultipleComponent]
public sealed class WorldGenerator : MonoBehaviour
{
    // Inspector: _worldWidth, _worldHeight, _surfacePerlinScale,
    //   _surfaceHeightMin/Max, _dirtLayerDepth, _cavePerlinScale,
    //   _caveThreshold, _tilemap, _tileRegistry, _playerTransform
    public WorldData WorldData { get; }
}

// 摄像机跟随（MonoBehaviour）
[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    // Inspector: _target, _smoothSpeed, _offset
}
```

## 依赖
- PlayerController（已完成，任务 001/002）
- Ground 层（Layer 8，已配置）
- Unity Tilemap 包（已安装）

## 文件清单
- `Assets/Scripts/World/BlockType.cs` — 方块类型枚举
- `Assets/Scripts/World/WorldData.cs` — 世界数据结构
- `Assets/Scripts/World/TileRegistry.cs` — ScriptableObject 方块→瓦片映射
- `Assets/Scripts/World/WorldGenerator.cs` — Perlin Noise 地形生成 + Tilemap 渲染
- `Assets/Scripts/Core/CameraFollow.cs` — 平滑摄像机跟随
- `Assets/Art/Tilesets/terrain_tiles.png` — 程序化生成的纯色瓦片图集
- `Assets/Art/Tilesets/Tiles/GrassTile.asset` — 草地瓦片资产
- `Assets/Art/Tilesets/Tiles/DirtTile.asset` — 泥土瓦片资产
- `Assets/Art/Tilesets/Tiles/StoneTile.asset` — 石头瓦片资产
- `Assets/Data/TileRegistry.asset` — TileRegistry 实例
- `Assets/Physics/PlayerNoFriction.asset` — 零摩擦物理材质
- `Assets/Scenes/SampleScene.unity` — 场景配置更新

## 验收标准
- [x] 按 Play 后自动生成 400x200 地形，表面起伏自然
- [x] 地表为草地层、下方为泥土层（约10格）、再下为石头层
- [x] 洞穴通过第二层 Perlin Noise 生成，不破坏地表顶部3格
- [x] 玩家自动出生在地表中央上方
- [x] 摄像机平滑跟随玩家
- [x] 地形使用 CompositeCollider2D 合并碰撞体，Ground 层正确
- [x] PlayerController 地面检测与新 Tilemap 碰撞体兼容
- [x] 瓦片之间无缝拼接（无透明缝隙）
- [x] 玩家跳跃时不会卡在方块边缘（零摩擦材质）
- [x] 0 编译错误，0 运行时错误

## 注意事项
- 原始 `world_tileset.png` 的瓦片有圆角透明边缘，不适合无缝拼接。改用程序化生成的 `terrain_tiles.png`（纯色方块）
- 使用 `Tilemap.SetTiles()` 批量 API 而非逐格设置，避免 80K 次 Tilemap rebuild
- Pixel Perfect Camera 用于像素对齐（PPU=16, 参考分辨率 480x270）
- TestGround 已停用，由 Tilemap 地形替代
