# 任务 005 - 方块挖掘耗时系统

## 目标
将方块挖掘从"左键瞬间破坏"改为"长按持续挖掘"。每种方块有硬度属性，玩家有挖掘速度属性，两者共同决定挖掘所需时间。挖掘过程中方块从顶部到底部逐渐变为完全透明，松手则进度归零、方块恢复原样。

## 设计概要

### 核心公式
```
miningDuration = hardness / miningSpeed
```
- `hardness`: 方块硬度（秒），由 `BlockDataRegistry` 按 BlockType 配置
- `miningSpeed`: 玩家挖掘速度，v1.0 默认为 1.0，后续随等级/武器变化

### 默认硬度值
| BlockType | Hardness (秒) | 说明 |
|-----------|--------------|------|
| Air       | 0            | 不可挖掘 |
| Dirt      | 0.5          | 松软泥土，容易挖 |
| Grass     | 0.6          | 表层草地，略高于泥土 |
| Stone     | 2.0          | 坚硬岩石，需要较长时间 |

### 挖掘流程
1. 玩家在交互范围内**按住**左键指向非 Air 方块，开始挖掘
2. 每帧累加进度: `progress += (miningSpeed / hardness) * Time.deltaTime`
3. 进度到达 1.0 时方块被破坏（变为 Air）
4. 挖掘过程中如果发生以下任一情况，进度**归零**，方块**保持原样**:
   - 玩家松开左键
   - 鼠标移到了另一个格子
   - 目标格子移出交互范围
5. 不保留部分进度——中断就完全重置

### 视觉进度效果
挖掘过程中，目标方块本身从顶部到底部逐渐变为**完全透明**:
- 使用独立的 `MiningProgressOverlay` 覆盖层显示目标方块当前仍然可见的部分
- 覆盖层使用目标方块自己的 sprite，而不是白色遮罩
- 开始挖掘时，将地形 Tilemap 上目标格子的颜色临时设为透明，隐藏原始 tile
- 覆盖层 Shader 根据 `_Progress` 裁掉 sprite 顶部区域:
  - `_Progress = 0` 时整个 sprite 完全可见
  - `_Progress = 1` 时整个 sprite 完全透明
- 这样视觉上表现为: 方块从**顶部到底部**逐渐消失
- 挖掘中断时恢复原 tile 颜色并隐藏覆盖层
- 挖掘完成时先恢复原 tile 颜色，再调用 `SetBlock(cell, Air)`

#### 实现要点
- `TileManager` 需要暴露:
  - `SetTileColor(Vector3Int tilemapPos, Color color)`
  - `ResetTileColor(Vector3Int tilemapPos)`
  - `GetBlockSprite(Vector3Int tilemapPos)`
- 覆盖层使用 `SpriteRenderer + MaterialPropertyBlock + URP 兼容 Shader`
- Shader 按 `UV.y > (1 - _Progress)` 输出透明，否则输出采样到的原始 sprite 像素

## 接口签名

```csharp
// === World/BlockDataRegistry.cs === 新增
// 存储每种 BlockType 的游戏属性（硬度等），后续可扩展更多属性
[CreateAssetMenu(fileName = "BlockDataRegistry", menuName = "Perraria/Block Data Registry")]
public sealed class BlockDataRegistry : ScriptableObject
{
    [System.Serializable]
    public struct BlockData
    {
        public BlockType Type;
        public float Hardness;
    }

    [SerializeField] private BlockData[] _blocks;

    /// <summary>返回指定方块类型的硬度值。Air 返回 0。</summary>
    public float GetHardness(BlockType type);
}

// === World/TileManager.cs === 修改
    /// <summary>设置指定格子的颜色（用于挖掘透明度动画）</summary>
    public void SetTileColor(Vector3Int tilemapPos, Color color);

    /// <summary>重置指定格子的颜色为默认白色</summary>
    public void ResetTileColor(Vector3Int tilemapPos);

    /// <summary>获取指定格子的 sprite，用于挖掘覆盖层显示</summary>
    public Sprite GetBlockSprite(Vector3Int tilemapPos);

// === Player/MiningProgressOverlay.cs === 新增
[DisallowMultipleComponent]
public sealed class MiningProgressOverlay : MonoBehaviour
{
    /// <summary>设置当前显示的方块 sprite</summary>
    public void SetSprite(Sprite sprite);

    /// <summary>显示覆盖层并对齐到目标格子中心</summary>
    public void Show(Vector3 cellCenterWorld);

    /// <summary>隐藏覆盖层并重置挖掘进度</summary>
    public void Hide();

    /// <summary>更新从上到下透明的进度</summary>
    public void SetProgress(float progress);
}

// === Player/PlayerBlockInteraction.cs === 修改
// 新增字段:
//   [SerializeField] private BlockDataRegistry _blockDataRegistry;
//   [SerializeField] private float _miningSpeed = 1f;
//   [SerializeField] private MiningProgressOverlay _miningOverlay;
//
// 新增私有状态:
//   private bool _isMining;
//   private Vector3Int _miningCell;
//   private float _miningProgress;   // 0~1
//   private float _miningDuration;   // 秒
//
// Update 逻辑变更:
//   - 左键由 wasPressedThisFrame 改为 isPressed（持续检测）
//   - 按住左键且目标格子不变且在范围内: 累加进度
//   - 开始挖掘时:
//       1. TileManager.SetTileColor(cell, transparent) 隐藏原 tile
//       2. _miningOverlay.SetSprite(TileManager.GetBlockSprite(cell))
//       3. _miningOverlay.Show(cellCenter)
//   - 每帧更新 _miningOverlay.SetProgress(progress)
//   - 进度 >= 1: 先 ResetTileColor，再 SetBlock(cell, Air)，然后隐藏覆盖层
//   - 松开左键 / 格子变化 / 超出范围: ResetTileColor 恢复方块，隐藏覆盖层，重置进度
//   - 右键逻辑不变（仍为单击放置）
```

## 依赖
- `BlockType` (enum) — 方块类型 ✅ 已实现
- `TileManager` — 方块读写 ✅ 已实现 (task 004)
- `PlayerBlockInteraction` — 现有交互逻辑 ✅ 已实现 (task 004)，本任务修改
- `TileRegistry` — BlockType → TileBase 映射 ✅ 已实现
- URP 2D Renderer — Shader 需兼容

## 文件清单
- `Assets/Scripts/World/BlockDataRegistry.cs` — **新增**，方块属性注册表 (ScriptableObject)
- `Assets/Scripts/World/TileManager.cs` — **修改**，新增 `SetTileColor` / `ResetTileColor` / `GetBlockSprite`
- `Assets/Scripts/Player/MiningProgressOverlay.cs` — **新增**，挖掘视觉覆盖层
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — **修改**，长按挖掘逻辑 + 覆盖层进度管理
- `Assets/Art/Shaders/MiningProgress.shader` — **新增**，URP 兼容透明裁切 Shader
- `Assets/Art/Shaders/MiningProgress.mat` — **新增**，覆盖层材质
- `Assets/Data/BlockDataRegistry.asset` — **新增**，配置各方块硬度值的 SO 实例

## 场景配置
1. 创建 `BlockDataRegistry` ScriptableObject 资产，放入 `Assets/Data/`，配置硬度值:
   - Air: 0, Dirt: 0.5, Grass: 0.6, Stone: 2.0
2. 在 `Player` 下创建子对象 `MiningProgressOverlay`:
   - 添加 `SpriteRenderer`
   - 挂载 `MiningProgressOverlay`
   - 材质使用 `MiningProgress.mat`
   - 默认隐藏 `SpriteRenderer`
   - `Sorting Order` 高于地形 Tilemap 与高亮 Tilemap
3. Player GameObject 上的 `PlayerBlockInteraction`:
   - 绑定 `BlockDataRegistry` 引用
   - 绑定 `MiningProgressOverlay` 引用
   - `_miningSpeed` 保持默认值 1.0

## 验收标准
- [ ] `BlockDataRegistry` 能正确查询每种方块的硬度值
- [ ] 左键单击不再瞬间破坏方块
- [ ] 长按左键持续挖掘，挖掘时间 = hardness / miningSpeed
- [ ] Dirt 挖掘耗时约 0.5 秒，Stone 挖掘耗时约 2.0 秒（miningSpeed=1 时）
- [ ] 挖掘过程中方块从顶部到底部逐渐变透明
- [ ] 覆盖层显示目标方块自己的 sprite，不是白色遮罩
- [ ] 挖掘完成时原 tile 颜色恢复后正确变为 Air，Tilemap 实时更新
- [ ] 松开左键 → 进度归零，原方块恢复完整显示
- [ ] 鼠标移到其他格子 → 进度归零，原格子恢复完整显示
- [ ] 目标格子移出交互范围 → 进度归零，格子恢复完整显示
- [ ] 不能对 Air 方块发起挖掘
- [ ] 右键放置逻辑不受影响（仍为单击放置）
- [ ] 覆盖层与高亮层可同时显示且排序正确
- [ ] 不引入新的控制台错误或警告
- [ ] 符合 coding-conventions.md 规范

## 注意事项
- **隐藏原 tile**: 挖掘开始时必须将原 tile 临时隐藏，否则 overlay 顶部透明后会透出完整原 tile，视觉上不会“消失”
- **颜色恢复时机**: 挖掘完成和中断时都必须先 `ResetTileColor` 恢复为 `Color.white`；否则格子后续可能残留透明状态
- **覆盖层 sprite**: 覆盖层必须使用目标格子当前的 sprite，而不是固定白方块
- **中断恢复**: 任何挖掘中断（松手、移格、超距）都必须调用 `ResetTileColor` 并隐藏 overlay
- **Time.deltaTime**: 进度累加必须基于 `Time.deltaTime`，确保帧率无关
- **挖掘透明与高亮共存**: 原 tile 被临时隐藏时，高亮仍在独立的 HighlightTilemap 上显示；overlay 需排序高于二者
- **_miningSpeed 字段**: 虽然 v1.0 默认值为 1，但字段必须为 `[SerializeField]` 暴露在 Inspector，方便后续任务（等级/武器系统）直接修改
- **CompositeCollider2D**: 方块被破坏后碰撞体会自动更新（Tilemap Collider 2D 的 Used by Composite 机制），无需手动处理
