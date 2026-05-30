# 任务 029 - 砍树系统（方块式树 + 木头采集）

## 目标
为方块世界引入**可砍伐的树**,作为 026 合成系统的基础材料来源。

树**完全由方块组成**（类 Minecraft）：竖直的**木头方块**（树干）+ 顶部一团**树叶方块**（树冠）。
玩家挖掉木头块掉落 `Item_Wood`；挖掉树叶块有**小概率**掉 `Item_Sapling`。

本任务**复用现有挖掘 / 掉落 / 存档全套机制**，核心改动是：
1. 新增两种 `BlockType`（Wood / Leaves）+ 两个 Tile 资产
2. 新建 `Item_Wood` / `Item_Sapling` 两个 ItemData
3. `BlockDataRegistry` 增加一个 `DropChance` 字段（支持"树叶小概率掉树苗"）
4. `WorldGenerator` 增加**种子化**刷树步骤

> **范围边界（本期只做"砍树出木材"）**：不做铁锭 / 矿石 / 冶炼、不做树木砍倒物理（floating 块允许）、
> 不做树叶衰减（leaf decay）、不做斧子工具克制、不做种树 / 生长。详见末尾"不做的事"。

---

## 美术资源前置检查（已完成 ✅）

- **是否需要新素材**: 是 —— **已就位**
- **来源**: OpenGameArt《16x16 Block Texture Set》，用户已下载到 `Assets/Art/Tilesets/blocks/`
- **授权与署名**: ⚠️ **Codex 实现前请打开素材包页面 / 附带的 LICENSE 文件确认授权**
  （CC0 则无需署名；CC-BY 需在 `docs/credits.md`（没有则新建）登记作者与链接）。Claude 未能在本地确认许可证文本，**此项 Codex 必须落实**。
- **选定文件**（均为独立 PNG，原生 16×16，**无需切片**）:

  | 用途 | 文件 | 尺寸 |
  |---|---|---|
  | 木头方块（树干）Tile | `Assets/Art/Tilesets/blocks/oak_log_side.png` | 16×16 |
  | 树叶方块（树冠）Tile | `Assets/Art/Tilesets/blocks/oak_leaves.png` | 16×16 |
  | `Item_Sapling` 图标 | `Assets/Art/Tilesets/blocks/plants/sapling_oak.png` | 16×16 |
  | `Item_Wood` 图标 | `Assets/Art/Tilesets/blocks/oak_log_side.png`（复用木块图） | 16×16 |

- **Sprite 导入设置**（每个 PNG，对齐现有 `terrain_tiles.png`）:
  - Texture Type: Sprite (2D and UI)，Sprite Mode: **Single**（每张就是一块）
  - Pixels Per Unit: **16**（1 块 = 1 world unit，与现有 tile 一致）
  - Filter Mode: **Point (no filter)**，Compression: **None**，Generate Mip Maps: 关闭
  - Wrap Mode: Clamp

> 本任务**只用 oak 一种树**。包里 beech/maple/pine/eucalyptus 同结构素材都在，未来加树种几乎零成本（多挑几张 tile + 随机选），本期不做。

---

## 设计概要

### 1. 新方块类型
```csharp
// World/BlockType.cs —— 追加
public enum BlockType : byte
{
    Air = 0,
    Dirt = 1,
    Grass = 2,
    Stone = 3,
    Wood = 4,     // 新增：原木 / 树干
    Leaves = 5    // 新增：树叶 / 树冠
}
```

### 2. Tile 注册
```csharp
// World/TileRegistry.cs —— 追加两个字段 + switch 分支
[SerializeField] private TileBase _woodTile;
[SerializeField] private TileBase _leavesTile;

public TileBase GetTile(BlockType blockType) => blockType switch
{
    BlockType.Dirt => _dirtTile,
    BlockType.Grass => _grassTile,
    BlockType.Stone => _stoneTile,
    BlockType.Wood => _woodTile,       // 新增
    BlockType.Leaves => _leavesTile,   // 新增
    _ => null
};
```
- 需新建 2 个 `Tile` 资产（`WoodTile.asset` / `LeavesTile.asset`），分别引用上面两张 sprite，
  放到 `Assets/Art/Tilesets/Tiles/`（与 DirtTile/GrassTile/StoneTile 同目录），并拖进 `TileRegistry.asset`。

### 3. BlockDataRegistry 增加掉落概率
当前 `SpawnItemDrop` 固定掉 1 个、无概率，无法表达"树叶小概率掉树苗"。给 `BlockData` 加一个字段：

```csharp
// World/BlockDataRegistry.cs
[System.Serializable]
public struct BlockData
{
    public BlockType Type;
    public float Hardness;
    public ItemData DropItem;
    [Range(0f, 1f)] public float DropChance;   // 新增；0 当作 1 处理（向后兼容现有 Dirt/Grass/Stone）
}

// 新增查询方法
public float GetDropChance(BlockType type)
{
    // 找到对应 BlockData：返回 DropChance；约定 DropChance <= 0 视为 1（必掉），保证旧数据行为不变
}
```

数值配置（写进 `BlockDataRegistry.asset`）：

| 方块 | Hardness | DropItem | DropChance |
|---|---|---|---|
| Wood | **0.4** | Item_Wood | 1.0（必掉） |
| Leaves | **0.15** | Item_Sapling | **0.1**（10% 掉树苗） |

> Hardness 选低值是为了"徒手也能较快砍倒"（对比 Grass 0.6 / Stone 2.0）。

### 4. 挖掘掉落接入（PlayerBlockInteraction 小改）
`SpawnItemDrop` 当前无条件掉落，需加一次概率判定：

```csharp
private void SpawnItemDrop(Vector3Int minedCell)
{
    if (_itemDropPrefab == null) return;

    BlockType minedType = _tileManager.GetBlock(minedCell);
    ItemData dropItem = _blockDataRegistry.GetDropItem(minedType);
    if (dropItem == null) return;

    float chance = _blockDataRegistry.GetDropChance(minedType);
    if (chance < 1f && Random.value > chance) return;   // 新增：概率不命中则不掉

    Vector3 spawnPosition = GetCellCenter(minedCell);
    ItemDrop drop = Instantiate(_itemDropPrefab, spawnPosition, Quaternion.identity);
    drop.Initialize(dropItem, 1);
}
```
> 木块/泥草石 DropChance=1（或 0 视为 1），行为与现状一致。仅树叶走概率分支。

### 5. 物品定义
两个新 ItemData（仿 `Item_Stone.asset` 结构，**itemId 取当前未占用的下一个号**，并注册进 `ItemDatabase.asset`）：

| 资产 | Type | PlaceBlockType | 说明 |
|---|---|---|---|
| `Item_Wood` | **Block** | Wood | 既是合成材料，也**可放置**回木块（沿用 PlaceBlock 逻辑，利于 027 建造） |
| `Item_Sapling` | **Material** | Air | 仅作掉落物 / 未来种树材料；本期不可放置、无用途（进背包即可） |

- `Item_Wood`: icon = oak_log_side，maxStackSize = 99
- `Item_Sapling`: icon = sapling_oak，maxStackSize = 99

### 6. 世界生成刷树（WorldGenerator）
在生成管线里新增 `PlantTrees(surfaceHeights)`，**插在 `CarveCaves` 之后、`RenderToTilemap` 之前**：

```csharp
private void GenerateWorldWithSeed(int seed)
{
    ...
    FillTerrain(surfaceHeights);
    CarveCaves(surfaceHeights);
    PlantTrees(surfaceHeights);   // 新增
    RenderToTilemap();
    SpawnPlayer(surfaceHeights);
    InitializeTileManager();
}
```

新增 Inspector 参数（`[Header("Trees")]`）：
```csharp
[SerializeField] private float _treeSpawnChance = 0.08f;   // 每个合格列生成概率
[SerializeField] private int _minTreeSpacing = 4;          // 相邻树干最小列间隔
[SerializeField] private int _trunkHeightMin = 4;
[SerializeField] private int _trunkHeightMax = 7;
```

`PlantTrees` 规则：
1. 从左到右遍历列 `x`，维护"上一棵树的 x"，保证间隔 ≥ `_minTreeSpacing`。
2. **种子化随机**：只用已 `Random.InitState(seed)` 初始化的 `UnityEngine.Random`
   （`Random.value` / `Random.Range`）。**不得**引入 `System.Random` 或 `Time`、不得读未种子化的随机源——
   否则读档按同种子重生成时树的位置会错位。
3. 合格列判定：
   - `surfaceY = surfaceHeights[x]`，且 `_worldData.GetBlock(x, surfaceY) == BlockType.Grass`
     （被洞穴削掉表层的列不种）
   - 树干 + 树冠所需格子都在世界范围内（特别是顶部不超出 `_worldHeight`）
   - 这些格子当前都是 Air（不覆盖已有方块）
4. `Random.value < _treeSpawnChance` 命中则种树：
   - **树干**：从 `surfaceY + 1` 起向上 `trunkH = Random.Range(_trunkHeightMin, _trunkHeightMax+1)` 格，全设 `Wood`
   - **树冠**：以树干顶端 `topY = surfaceY + trunkH` 为中心铺树叶（只覆盖 Air 格）：
     ```
        . L .       y = topY+1   （顶冠）
        L L L       y = topY      （顶层三连）
        L L L       y = topY-1
        L W L       y = topY-2     （W=树干顶段已是 Wood，两侧补叶）
     ```
     即：`x-1..x+1` 横向、`topY-2..topY` 三层树叶（树干所在中心列保留 Wood，不覆盖），
     再在 `(x, topY+1)` 加 1 格顶冠。具体形状 Codex 可微调，保证"看起来像一棵树"即可。
5. 写入用 `_worldData.SetBlock(x, y, ...)`（生成期直接写数据，渲染交给后续 `RenderToTilemap`）。

> **存档零改动验证点**：刷树只动 `_worldData`（不进 `_playerEdits`）。读档流程 = `GenerateWorldWithSeed(savedSeed)` 重生成（树按同种子复现）+ `ApplyTileChanges`（重放玩家砍/放的编辑）。
> 所以**存档系统无需改任何代码**，但 Codex 必须验证：同种子两次生成树完全一致；存读档后树/已砍缺口一致。

---

## 接口签名汇总
```csharp
// === World/BlockType.cs === 追加 Wood=4, Leaves=5
// === World/TileRegistry.cs === 追加 _woodTile/_leavesTile 字段 + switch 分支
// === World/BlockDataRegistry.cs ===
public struct BlockData { public BlockType Type; public float Hardness; public ItemData DropItem; public float DropChance; }
public float GetDropChance(BlockType type);   // 新增
// === Player/PlayerBlockInteraction.cs === SpawnItemDrop 内加概率分支
// === World/WorldGenerator.cs === 新增 PlantTrees(int[] surfaceHeights) + 4 个 Inspector 参数
```

## 依赖
- 任务 003 WorldGenerator（扩展生成管线）
- 任务 004 / 005 方块挖掘（复用，仅 SpawnItemDrop 加概率分支）
- 任务 007 ItemData / ItemDatabase（新建两个物品）
- 任务 011 ItemDrop（掉落实体，复用）
- 任务 018 / 019 存档（**不改代码**，但需验证树的确定性复现）
- **下游**：026 合成系统的"木材"材料来源由本任务提供

## 文件清单

### 新增
- `Assets/Art/Tilesets/Tiles/WoodTile.asset`（MCP 创建，引用 oak_log_side）
- `Assets/Art/Tilesets/Tiles/LeavesTile.asset`（MCP 创建，引用 oak_leaves）
- `Assets/Data/Items/Item_Wood.asset`
- `Assets/Data/Items/Item_Sapling.asset`

### 修改
- `Assets/Scripts/World/BlockType.cs` — 追加 Wood / Leaves
- `Assets/Scripts/World/TileRegistry.cs` — 追加 2 个 tile 字段 + switch
- `Assets/Scripts/World/BlockDataRegistry.cs` — BlockData 加 DropChance + GetDropChance()
- `Assets/Scripts/Editor/BlockDataRegistryCsvImporter.cs` — **重要**：reimport 时像 DropItem 一样**保留**已有 DropChance（否则导一次 CSV 会把树叶概率清零）；CSV 仍只有 Type,Hardness 两列
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — SpawnItemDrop 加概率分支
- `Assets/Scripts/World/WorldGenerator.cs` — PlantTrees + 参数
- `Assets/Data/BlockDataRegistry.csv` — 追加 `Wood,0.4` / `Leaves,0.15` 两行
- `Assets/Data/BlockDataRegistry.asset` — Wood/Leaves 两行的 DropItem + DropChance（MCP 配置）
- `Assets/Data/TileRegistry.asset` — 绑定 WoodTile / LeavesTile
- `Assets/Resources/ItemDatabase.asset` — 注册 Item_Wood / Item_Sapling

## 验收标准

### 方块与挖掘
- [ ] 木块、树叶块能正确渲染（oak 贴图，像素清晰无模糊）
- [ ] 徒手挖木块掉落 1 个 Item_Wood，进背包
- [ ] 挖树叶块约 10% 掉 1 个 Item_Sapling（多挖几个统计验证，非精确）
- [ ] 木块可被放回（手持 Item_Wood 右键放置 Wood 块）
- [ ] 现有 Dirt/Grass/Stone 掉落行为不变（回归：DropChance 缺省不影响）

### 世界生成
- [ ] 地表草地上长出 oak 树（树干竖直 4~7 格 + 顶部叶团）
- [ ] 树只长在 Grass 表面，不长在洞穴削顶的列 / 不悬空 / 不嵌入地下
- [ ] 树之间有合理间隔（不挤成一片）
- [ ] **确定性**：同一 seed 连续生成两次，树的位置 / 高度完全一致（Codex 用 `Unity_RunCommand` 跑两次同种子，对比若干列的方块快照）

### 存档（零代码改动验证）
- [ ] 砍掉某棵树几格后存档 → 读档，树的缺口与砍前一致（玩家编辑重放正确）
- [ ] 未触碰的树读档后位置不变（种子复现正确）

### 综合
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] MCP 截图：地表一排树 + 玩家砍树掉木头 + 背包里的木头/树苗
- [ ] 符合 `coding-conventions.md`

## 注意事项

### ⚠️ 斧头不能砍树（现有代码约束，本期不解决）
现有 `Item_Axe` 是 **Weapon 类型**，而 `PlayerBlockInteraction.IsActionItemSelected` 把武器判为"动作物品"会**跳过挖掘**——
即手持斧头**无法挖任何方块**。且挖矿加速只对 `Tool` 类型生效且是**全局**加速，没有"斧子只对木头快"的机制。
- 本期方案：木头/树叶设为**低硬度**，**徒手或镐子**即可快速砍倒（类 Minecraft 早期）。
- **不**在本任务引入"斧子工具克制 / 按方块类型的挖掘效率"系统——那是独立的一套，留作未来任务。
- 如果你（用户）希望"必须用斧子砍树"，请单独提，会涉及改 IsActionItemSelected + 工具克制表，属另一个任务。

### 树苗用途
`Item_Sapling` 本期**只进背包**，无放置 / 种植 / 生长逻辑（种树系统是未来任务）。
现在掉它只是为了"树叶有点产出"+ 为以后种树铺垫。**不要**为它做种植机制。

### 树冠 floating 块允许
玩家从底部砍断树干，上面的木/叶会**悬空**（不掉落、不衰减）——这是方块游戏的常态，**本期接受**，不做"砍倒物理"或"树叶衰减"。

### 种子化随机是硬要求
`PlantTrees` 只能用已种子化的 `UnityEngine.Random`，且必须在固定的管线位置调用。任何非确定性随机都会导致读档树错位——这是本任务最容易踩的坑，验收必查确定性。

### CSV 导入器别清零 DropChance
`BlockDataRegistryCsvImporter` 重建数组时目前会保留 DropItem，**务必同样保留 DropChance**，否则每次"Import Default Block Data CSV"都会把树叶的 0.1 重置为 0。

### 不做的事
- 不做铁锭 / 矿石 / 熔炉冶炼（未来采矿任务；素材包里已有 ore 贴图可用）
- 不做斧子工具克制 / 按方块挖掘效率
- 不做种树 / 树苗生长 / 树叶衰减 / 砍倒物理
- 不做多树种（本期只 oak；素材已备，未来扩展）
- 不做果实 / 食物掉落（本期树叶只掉树苗）
- 不改存档代码（只验证确定性）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/029-tree-chopping.md` 写一份交付记录，
参考 `docs/codex-reports/README.md` 结构。Claude 审查时先读这份记录，没写视为未完成。

**交付记录额外要求**：
1. **授权确认**：写明 16x16 Block Texture Set 的实际许可证（CC0 / CC-BY / 其他），是否已在 credits 登记
2. **确定性证据**：同种子两次生成、若干列方块快照一致的 `Unity_RunCommand` 输出
3. **掉落统计**：挖 N 个树叶块、掉树苗次数（验证 ~10%）
4. **MCP 截图**：地表树林 + 砍树掉木头 + 背包木头/树苗（场景/相机视图，非 Editor 面板）
5. **回归说明**：确认 Dirt/Grass/Stone 掉落与挖掘行为未变
