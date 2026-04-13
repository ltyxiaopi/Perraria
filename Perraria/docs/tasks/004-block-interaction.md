# 任务 004 - 方块交互系统（挖掘 + 放置）

## 目标
实现玩家通过鼠标与世界方块的交互：鼠标指向方块进行挖掘（左键）和放置（右键）。
这是 v1.0 核心玩法循环的关键一环——"能挖、能放"。

## 设计概要

### 坐标系统说明
WorldGenerator 使用偏移坐标渲染 Tilemap：
- WorldData 坐标: `(0,0)` 到 `(width-1, height-1)`
- Tilemap 坐标: `(x - halfWidth, y - halfHeight)` 其中 `halfWidth = worldWidth/2`, `halfHeight = worldHeight/2`
- 转换: `worldDataX = tilemapX + halfWidth`, `worldDataY = tilemapY + halfHeight`

### 挖掘规则（v1.0 简化版）
- 左键单击立即破坏方块（不做挖掘耗时）
- 被破坏的方块变为 Air
- 暂不掉落物品（物品系统在 Task 005）
- 不能挖掘 Air

### 放置规则（v1.0 简化版）
- 右键单击放置 Dirt 方块（v1.0 固定为 Dirt，物品/背包系统在后续任务）
- 只能放置在 Air 格子上
- 不能放置在玩家当前占据的格子上（防止卡住）

### 交互范围
- 以玩家位置为圆心，半径 **5 格**的圆形区域内的方块才可交互
- 判定方式：目标格子中心与玩家位置的欧几里得距离 ≤ `_interactionRange`
- 超出范围的方块不响应点击，高亮光标不显示（或显示为不可交互状态）

## 接口签名

```csharp
// === World/TileManager.cs ===
// 运行时方块读写服务，WorldGenerator 生成完毕后由此类管理所有方块变更
[DisallowMultipleComponent]
public sealed class TileManager : MonoBehaviour
{
    // Inspector: Tilemap 引用、TileRegistry 引用
    // 需要从 WorldGenerator 获取 WorldData 和坐标偏移量

    /// <summary>初始化，接收 WorldGenerator 的数据</summary>
    public void Initialize(WorldData worldData, int halfWidth, int halfHeight);

    /// <summary>查询指定 Tilemap 坐标处的方块类型</summary>
    public BlockType GetBlock(Vector3Int tilemapPos);

    /// <summary>设置指定 Tilemap 坐标处的方块，同步更新 WorldData 和 Tilemap 渲染</summary>
    public bool SetBlock(Vector3Int tilemapPos, BlockType type);

    /// <summary>世界坐标转 Tilemap 格子坐标</summary>
    public Vector3Int WorldToCell(Vector3 worldPosition);
}

// === Player/PlayerBlockInteraction.cs ===
// 处理鼠标输入，实现挖掘和放置逻辑
[DisallowMultipleComponent]
public sealed class PlayerBlockInteraction : MonoBehaviour
{
    // Inspector:
    //   TileManager 引用
    //   float _interactionRange = 5f（交互半径，单位：格）
    //   Tilemap _highlightTilemap（高亮层 Tilemap，可选）
    //   TileBase _highlightTile（高亮用 Tile，可选）
    //   BlockType _placeBlockType = BlockType.Dirt（v1.0 固定放置类型）

    // 在 Update 中：
    //   1. 鼠标位置 → Camera.main.ScreenToWorldPoint → TileManager.WorldToCell → targetCell
    //   2. 计算目标格子中心与玩家 transform.position 的距离
    //   3. 距离 ≤ _interactionRange 则在范围内，更新高亮；否则隐藏高亮
    //   4. 左键点击（范围内）→ 挖掘（SetBlock → Air）
    //   5. 右键点击（范围内）→ 放置（SetBlock → _placeBlockType）
}
```

## WorldGenerator 改动
WorldGenerator 需要暴露坐标偏移量，以便 TileManager 初始化：
- 新增公共属性 `public int HalfWidth => _worldWidth / 2;`
- 新增公共属性 `public int HalfHeight => _worldHeight / 2;`
- 在 `Start()` 最后调用 `TileManager.Initialize()`（TileManager 可通过 Inspector 或 GetComponent 引用）

## 依赖
- `WorldData` — 方块数据读写 ✅ 已实现
- `BlockType` — 方块类型枚举 ✅ 已实现
- `TileRegistry` — BlockType → TileBase 映射 ✅ 已实现
- `WorldGenerator` — 需要小幅修改，暴露偏移量并初始化 TileManager
- `Camera.main` — 鼠标位置转世界坐标

## 文件清单
- `Assets/Scripts/World/TileManager.cs` — 新增，方块读写服务
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — 新增，玩家挖掘/放置逻辑
- `Assets/Scripts/World/WorldGenerator.cs` — 修改，暴露偏移量 + 初始化 TileManager

## 场景配置
1. 在场景的 World/Grid 下新增一个 Tilemap 子对象命名为 `HighlightTilemap`
   - Sorting Layer 设为比地形 Tilemap 更高（或 Order in Layer +1）
   - 用于显示鼠标悬停的方块高亮
2. 创建一个半透明白色高亮用 Tile 资产（或使用代码动态创建）
3. 将 TileManager 组件挂载在 WorldGenerator 同一 GameObject 上（或场景根对象）
4. 将 PlayerBlockInteraction 组件挂载在 Player GameObject 上
5. 在 Inspector 中连接引用

## 验收标准
- [ ] TileManager 正确转换世界坐标与 Tilemap 坐标
- [ ] 左键点击范围内的方块，方块被破坏（变为 Air），Tilemap 实时更新
- [ ] 右键点击范围内的空气格，放置 Dirt 方块，Tilemap 实时更新
- [ ] 不能挖掘 Air 方块
- [ ] 不能在非 Air 格子上放置方块
- [ ] 不能在玩家当前站立的格子上放置方块（防止卡住）
- [ ] 超出交互半径的方块不响应点击
- [ ] 鼠标悬停时有高亮光标指示目标方块（可选但建议实现）
- [ ] WorldData 与 Tilemap 保持同步（挖掘/放置后数据和显示一致）
- [ ] 不引入新的控制台错误或警告
- [ ] 符合 coding-conventions.md 规范

## 注意事项
- **坐标转换是核心难点**：务必正确处理 WorldData 坐标和 Tilemap 坐标之间的偏移
- **CompositeCollider2D**：地形使用了 CompositeCollider2D 合并碰撞体，方块变更后碰撞体应自动更新（Tilemap Collider 2D 的 Used by Composite 会自动处理）
- **性能**：单个方块的 SetTile 调用开销很小，不需要批量处理
- **Input System**：当前项目使用 New Input System，鼠标输入建议用 `Mouse.current.leftButton` / `Mouse.current.rightButton`，或在 Input Actions 中添加 Mine/Place 动作
- **摄像机引用**：避免在 Update 中每帧调用 `Camera.main`（有 GC 开销），应在 Awake 中缓存
- **放置碰撞检测**：检查玩家占据格子时，需考虑玩家 collider 实际占据的格子范围（可能跨越多格）
