# 任务 026 - 合成系统（工作台 + 配方）【完整版】

> ✅ 本文件已由骨架版补全为可执行规范（2026-06-01）。设计决策见
> `memory/project_crafting_026_decisions.md` 与本文「设计决策」一节。

## 目标
实现 Perraria 第一版合成系统，跑通一条经典的「砍树 → 手工合成工作台 → 放置工作台 → 在工作台旁合成木质工具/武器」闭环：

1. 新增 `Recipe` ScriptableObject：N 个材料 → 1 个产出物，可选「需要的工作台」。
2. 新增 `CraftingService`：纯逻辑层，负责「能否合成 / 执行合成」，不依赖 UI。
3. 合成 UI **集成进现有背包面板**（Tab 打开背包时同时显示配方列表），沿用 InventoryUI 的视觉风格与交互。
4. 工作台做成**新方块** `BlockType.Workbench`，复用现有方块放置（004）/ 挖掘 / 掉落 / 存档系统，**不引入 027 家具系统**。
5. 初始 4 个配方（全部只消耗木材 `Item_Wood`），无配方解锁机制（默认全解锁）。

## 设计决策（已与用户确认）

| 决策点 | 结论 |
| --- | --- |
| 工作台形态 | **普通方块** `BlockType.Workbench`，用 `Item_Workbench`（`Type=Block`）走现有方块放置系统放下，可被挖回。**不做 027 家具基类**。 |
| 合成入口 | **集成进背包面板**，按 Tab 打开背包同时显示配方列表。**不新增 C 键、不做独立窗口。** |
| 合成分层 | 两类配方：①手工合成（`RequiredStations` 为空，任何地方可合成）②工作台合成（需玩家附近有 Workbench 方块）。 |
| 配方循环 | 工作台本身**手工合成**；木剑/木箭/木镐**需要工作台**。形成 Terraria 式渐进闭环。 |
| 材料 | 本期所有配方**只消耗 `Item_Wood`**。铁锭/矿石本期不做。 |
| 配方解锁 | **不做**，全部默认解锁。 |
| 合成动画/进度条 | **不做**，瞬时合成。 |
| 配方数据格式 | 手填 `Recipe` ScriptableObject 资产（`Assets/Data/Recipes/`）。 |

## 依赖（均已就绪）
- 008 Inventory：材料检查 / 扣除 / 添加（本任务需给 `Inventory` 补一个**计数查询**方法，见下）
- 009 Hotbar/InventoryUI：合成 UI 集成进 `InventoryUI` 的面板
- 007/020 ItemData + ItemDatabase：产出物与材料的物品定义
- 004 PlayerBlockInteraction + TileManager：工作台方块的放置 / 挖掘 / 邻近检测
- 029 砍树系统：提供材料来源 `Item_Wood`（itemId 36，`Type=Block`，`PlaceBlockType=Wood`）
- 018/019 存档系统：工作台是方块，自动随 `WorldData` 方块编辑一起存档，无需额外工作

## 接口定义

### 1. `BlockType` 扩展（`Assets/Scripts/World/BlockType.cs`）
```csharp
public enum BlockType : byte
{
    Air = 0,
    Dirt = 1,
    Grass = 2,
    Stone = 3,
    Wood = 4,
    Leaves = 5,
    Workbench = 6   // 新增
}
```

### 2. `Recipe`（`Assets/Scripts/Crafting/Recipe.cs`）
```csharp
[CreateAssetMenu(fileName = "Recipe", menuName = "Perraria/Recipe")]
public sealed class Recipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemData Item;
        public int Count;
    }

    [SerializeField] private Ingredient[] _inputs;
    [SerializeField] private ItemData _output;
    [SerializeField] private int _outputCount = 1;
    // 空数组 = 手工合成（任何地方可合成）。
    // 非空 = 玩家附近必须存在其中任一工作台对应的方块。
    [SerializeField] private ItemData[] _requiredStations;

    public IReadOnlyList<Ingredient> Inputs => _inputs;
    public ItemData Output => _output;
    public int OutputCount => _outputCount;
    public IReadOnlyList<ItemData> RequiredStations => _requiredStations;
    public bool RequiresStation => _requiredStations != null && _requiredStations.Length > 0;
}
```

### 3. `Inventory` 新增计数方法（`Assets/Scripts/Items/Inventory.cs`）
合成 UI 需要「拥有数量」来判断材料是否充足（够则高亮、不够则灰显）。现有 `Inventory` 只有
`AddItem` / `RemoveItem`，缺查询。新增：
```csharp
/// <summary>统计背包中某物品的总数量（跨所有 slot 累加）。</summary>
public int CountItem(ItemData item);
```
- `item == null` 返回 0。
- 遍历所有 slot，累加 `Item.ItemId == item.ItemId` 的 `Count`。
- 不分配垃圾（不要用 LINQ），保持与现有方法一致的 for 循环风格。

### 4. `CraftingService`（`Assets/Scripts/Crafting/CraftingService.cs`）
纯逻辑层（普通 C# 类或 MonoBehaviour 均可，倾向**普通静态/实例工具类**，便于单测），不直接触碰 UI：
```csharp
public sealed class CraftingService
{
    // 材料是否齐全（只看背包，不看工作台）
    public bool HasIngredients(Recipe recipe, Inventory inventory);

    // 工作台条件是否满足（recipe 不需要工作台时恒为 true）
    // stationAvailable: 由调用方（UI/Station 检测器）传入「附近是否有所需工作台」
    public bool CanCraft(Recipe recipe, Inventory inventory, bool stationAvailable);

    // 执行合成：先校验 CanCraft；扣材料；加产出物。返回是否成功。
    // 注意产出物可能因背包满而塞不下 —— 用 AddItem 的返回值判断剩余，
    //   若塞不下则不应扣材料（先模拟/先加后回滚，二选一，实现需保证原子性）。
    public bool TryCraft(Recipe recipe, Inventory inventory, bool stationAvailable);
}
```
**原子性要求**：合成必须「要么全部成功（扣材料 + 给产出物），要么完全不改变背包」。
推荐顺序：①`CanCraft` 通过 → ②先确认产出物加得进（`AddItem` 返回 0 剩余，或预留空位）→ ③再扣材料。
若产出物加不进背包，则取消本次合成并给出反馈（UI 层提示「背包已满」即可）。

### 5. 工作台邻近检测（`Assets/Scripts/Crafting/WorkbenchProximity.cs` 或并入 UI）
- 在 `TileManager` 上扫描玩家周围一定范围（建议半径 4 格的方形区域）内是否存在 `BlockType.Workbench`。
- 复用 `TileManager.WorldToCell(player.position)` 得到中心 cell，再在 `[-R, R]²` 偏移范围内
  调用 `GetBlock` 检查。范围小、每帧/每次打开面板算一次即可，无 GC。
- 输出：`bool IsNearStation(ItemData stationItem)`（按工作台物品 → 对应 BlockType 映射）。
  本期只有一个工作台，可先简化为 `bool IsNearWorkbench()`，但**接口设计保留按 station 物品查询**以便扩展。
  > 工作台物品 → BlockType 的映射可复用 `ItemData.PlaceBlockType`（`Item_Workbench.PlaceBlockType == Workbench`）。

### 6. 合成 UI（集成进 `Assets/Scripts/UI/InventoryUI.cs` + 新增 `CraftingPanel`/`RecipeSlotUI`）
- 背包面板（Tab 打开的 `_panel`）内**新增一块配方列表区域**。打开背包时一并刷新。
- 每个配方显示：产出物图标 + 名称 + 所需材料（图标×数量），材料够且工作台条件满足 → 可点击/高亮；
  否则灰显且不可点击。
- 点击某配方 → 调用 `CraftingService.TryCraft`；成功后刷新配方列表与背包 slot（材料数变化会影响其它配方可用性）。
- 工作台条件：打开面板时 / 玩家移动时重新计算 `IsNearWorkbench()`；不在工作台旁时，需要工作台的配方灰显。
- **不做**：拖拽合成、一键合成全部、配方搜索/筛选、Shift 批量。点一次合成一次。
- 视觉沿用现有 `SlotUI` / 面板配色，新增的 `RecipeSlotUI` 复用现有图标 + 数字文本风格，避免引入新美术。

> 实现提示：配方列表的数据源 = 一个 `Recipe[]`（在 InventoryUI 或一个 `RecipeBook` SO/字段里挂上 4 个 Recipe 资产）。
> 不需要做「自动发现所有 Recipe 资产」的反射/Resources 扫描，手挂即可（默认全解锁，列表固定）。

### 7. `ItemData` 新增 `_iconAngleOffset`（武器朝向补偿，支撑斜画木剑）
本期木剑素材（Vollrat 剑包）在 16×16 格子里是**斜着画的（刀刃朝右上 45°）**，而现有武器（如 `weapon_rusty_sword`）
都是**竖直画的（刀刃朝正上方）**。`PlayerCombat` 的挥砍逻辑（`WeaponFacingOffsetDegrees = -90f`）默认「刀刃朝正上方」，
直接绑定斜画剑会导致**装备静止 + 挥砍时刀都歪 45°**。为干净地支撑这套斜画剑（将来石/铁/秘银等高阶剑同样斜画，可复用），
给 `ItemData` 增加一个**通用角度补偿字段**：

```csharp
// ItemData.cs，与其它武器字段放一起
[SerializeField] private float _iconAngleOffset = 0f; // 度。把「素材里刀刃的朝向」归一化到「正上方」，0 = 不变（所有现有武器保持原样）
public float IconAngleOffset => _iconAngleOffset;
```

`PlayerCombat.cs` 改动（**仅加偏移量，不改动挥砍/命中判定的其它逻辑**）：
- 在**所有设置武器精灵旋转**的地方，把 `weaponItem.IconAngleOffset` 一并加进旋转角：
  - `SwingRoutine` 里的 `startRotation` / `endRotation`（`aimAngle + ...Offset + WeaponFacingOffsetDegrees` → 再 `+ weaponItem.IconAngleOffset`）。
  - 静止持握姿态（`RefreshWeaponRenderer` 里武器分支）也要让刀刃归一化到正上方，避免静止时木剑歪着。
- 默认 `_iconAngleOffset = 0`，对所有现有武器**零影响**（数学上等价于原逻辑），无需改任何现有武器资产。

> 角度方向已推导验证：木剑素材刀刃在 sprite 局部约 +45°，现有武器为 +90°，故 **`Item_WoodSword._iconAngleOffset = 45`** 即可对齐。
> Codex 落地后**务必用 MCP 截图实测**：装备木剑静止时刀尖朝上、挥砍时刀刃沿挥动方向（与现有剑表现一致）。

## 初始配方表（本期全部只消耗 Item_Wood）

| 配方资产 | 产出物 | 产出数 | 材料 | 需要工作台 |
| --- | --- | --- | --- | --- |
| `Recipe_Workbench` | `Item_Workbench`（新建，id 39，`Type=Block`，`PlaceBlockType=Workbench`） | 1 | 10 × Item_Wood | ❌ 手工合成 |
| `Recipe_WoodSword` | `Item_WoodSword`（新建，id 38，`Type=Weapon`，`WeaponSubType=Melee`） | 1 | 7 × Item_Wood | ✅ Workbench |
| `Recipe_WoodArrow` | `Item_Arrow`（已存在，id 27） | 5 | 1 × Item_Wood | ✅ Workbench |
| `Recipe_WoodPickaxe` | `Item_Pickaxe_Wood`（已存在，id 6） | 1 | 8 × Item_Wood | ✅ Workbench |

> **木剑数值（已定，取现有最低阶 `Item_RustySword` dmg6 的约 80%，略低一档）**，Codex 直接按此填 `Item_WoodSword.asset`：
> `WeaponDamage=5`、`WeaponRange=1.3`、`SwingArcDegrees=95`、`SwingDuration=0.42`、`KnockbackForce=3`、
> `WeaponSubType=Melee`、`MaxStackSize=1`、`MiningSpeedMultiplier=1`、`IconAngleOffset=45`。数值可在试玩后微调。

## 资产清单

### 美术素材（已由用户落位，Claude 已看图确认切片信息 2026-06-01）

> 两张源图已放进仓库，下面给出**精确像素坐标**。Codex 负责裁切/导入/配置（沿用现有同类资源的导入设置）。

**1. 木剑 Sprite —— 源图 `Assets/Art/Sprites/Weapons/swordsprites.png`（160×32）**
- 该图是 Vollrat《16x16 Basic Sword Sprites》，**16×16 网格、10 列 × 2 行、无间隔/无内边距**。
- **木剑 = 左下角那一格**：像素矩形 `x=0, y=16, w=16, h=16`（从图片左上角算起；即第 2 行第 1 列，棕色刀身+剑柄）。
  已逐像素确认该格内容铺满 16×16（无透明留白，无需额外裁剪，见 [[project-sprite-slicing]]）。
- **落地方式**（与现有 `weapon_*.png` 单图约定一致）：把这一格裁成单独文件
  `Assets/Art/Sprites/Weapons/weapon_wood_sword.png`（16×16），导入为单一 Sprite。
- **导入设置对齐现有武器**（参考 `weapon_rusty_sword.png.meta`）：`spriteMode=Single`、`filterMode=Point(0)`、
  `PPU=16`、`pivot=(0.5, 0)` 底部中央。
- ⚠️ 该剑**斜画 45°**，需配合上文「接口定义 §7 `_iconAngleOffset`」：`Item_WoodSword._iconAngleOffset = 45`。
  pivot 先沿用 `(0.5,0)`；若 MCP 截图显示剑与手部明显脱节，再把 pivot 微调到剑柄像素（约归一化 (0.16,0.13)）。
- 顺带：同图还有石/铁/金/紫(秘银)等高阶剑，**本期不裁**，留将来武器升级任务。

**2. 工作台 Tile —— 源图 `Assets/Art/Tilesets/blocks/Spritesheet/roguelikeSheet_transparent.png`（968×526）**
- 该图是 Kenney Roguelike/RPG pack，**16×16 瓦片、瓦片间 1px 间隔 → 步距 17px**，57 列 × 31 行，零偏移。
  任意瓦片 (col,row) 的像素位置 = `x = col*17, y = row*17`，尺寸 16×16。
- **工作台 = 用户选定的「抽屉工作桌」**：`col=23, row=5` → 像素矩形 `x=391, y=85, w=16, h=16`（接地、带一个抽屉）。
- **落地方式**（与现有 `Assets/Art/Tilesets/blocks/*.png` 单块单图约定一致）：把这一格裁成单独文件
  `Assets/Art/Tilesets/blocks/workbench.png`（16×16），导入设置**对齐现有方块贴图**（参考同目录任一 `*.png.meta`，
  如 `cobblestone.png.meta`），同一张图既作 `BlockType.Workbench` 的 `TileBase`，也作 `Item_Workbench` 图标。

**授权署名（Codex 落地时一并补）** → 新建 `docs/credits.md` 登记：
- 木剑：Vollrat《16x16 Basic Sword Sprites》— **CC-BY 3.0（必须署名）**。
- 工作台/方块图集：Kenney《Roguelike/RPG pack》— CC0（署名非必须，建议一并写上）。

### 需要新建的资产（Codex 通过 MCP 创建/配置）
- 裁切并导入 `weapon_wood_sword.png`、`workbench.png` 两张单图（坐标/设置见上「美术素材」节）
- `Item_WoodSword.asset`（itemId 38，Weapon/Melee，绑定 `weapon_wood_sword` sprite，数值见配方表下方，`IconAngleOffset=45`）
- `Item_Workbench.asset`（itemId 39，Block，`PlaceBlockType=Workbench`，绑定 `workbench` sprite 作图标）
- 工作台 `TileBase` 资产 + 注册进 `TileRegistry`（让 `BlockType.Workbench` 能渲染）
- `BlockDataRegistry` 中登记 `Workbench`：硬度（建议与 Wood 接近，可快速挖回）、掉落物 = `Item_Workbench`、DropChance = 1
- 4 个 `Recipe` 资产置于 `Assets/Data/Recipes/`
- 将 4 个 Recipe 挂到 InventoryUI/RecipeBook 引用上
- `ItemDatabase` 注册新增的 2 个物品

## 文件清单
- `Assets/Scripts/World/BlockType.cs`（改：+Workbench）
- `Assets/Scripts/Items/Inventory.cs`（改：+CountItem）
- `Assets/Scripts/Items/ItemData.cs`（改：+`_iconAngleOffset` 字段及只读属性，见 §7）
- `Assets/Scripts/Player/PlayerCombat.cs`（改：旋转处叠加 `IconAngleOffset`，见 §7，**不动其它战斗逻辑**）
- `Assets/Scripts/Crafting/Recipe.cs`（新）
- `Assets/Scripts/Crafting/CraftingService.cs`（新）
- `Assets/Scripts/Crafting/WorkbenchProximity.cs`（新，或并入 UI）
- `Assets/Scripts/UI/InventoryUI.cs`（改：集成配方列表）
- `Assets/Scripts/UI/RecipeSlotUI.cs`（新，单个配方行 UI）
- `Assets/Data/Recipes/Recipe_Workbench.asset` 等 4 个（新）
- `Assets/Data/Items/Item_WoodSword.asset`、`Item_Workbench.asset`（新）
- `Assets/Art/Sprites/Weapons/weapon_wood_sword.png`、`Assets/Art/Tilesets/blocks/workbench.png`（新，裁切自源图）
- `docs/credits.md`（新：登记 Vollrat CC-BY 3.0 署名 + Kenney CC0）

## 验收标准
- [ ] `Recipe` SO 可在 Inspector 编辑（材料数组 / 产出 / 产出数 / 需要的工作台）
- [ ] `Inventory.CountItem` 正确统计跨 slot 总数，`item==null` 返回 0
- [ ] 打开背包（Tab）时显示 4 个配方；不在工作台旁时，3 个需工作台的配方灰显且不可点击
- [ ] 材料不足时配方灰显不可合成；材料充足（且工作台条件满足）时高亮可合成
- [ ] 合成工作台（手工）：消耗 10 木材 → 背包 +1 Item_Workbench
- [ ] 把 Item_Workbench 放到世界（右键，复用方块放置）→ 渲染出工作台方块；挖掉能掉回 Item_Workbench
- [ ] 站在工作台旁打开背包 → 木剑/木箭/木镐配方变为可合成
- [ ] 合成成功：正确扣材料、正确加产出物（含产出数：木箭 1 木 → 5 箭）
- [ ] 背包满导致产出物塞不下时：合成被拒绝，**材料不被扣除**（原子性），UI 给出提示
- [ ] 合成后配方列表即时刷新（材料变化导致的可用性变化反映出来）
- [ ] 工作台方块随存档保存 / 读档还原（复用现有方块存档，验证一次即可）
- [ ] 木剑朝向：装备静止时刀尖朝上、挥砍时刀刃沿挥动方向（与现有剑一致，无 45° 歪斜）；现有武器表现不受 `_iconAngleOffset` 影响
- [ ] MCP 验证：0 编译错误、0 新增控制台报错；场景中实测一遍完整闭环

## 不做的事（本期范围外）
- 配方解锁 / 学习机制（默认全解锁）
- 合成动画 / 进度条（瞬时）
- 拖拽合成 / 一键全部 / Shift 批量 / 配方搜索筛选
- 铁锭 / 矿石 / 其它材料（留未来采矿任务）
- 027 家具基类 / 多格占用（工作台本期就是 1×1 方块）
- 多种工作台分级（铁砧等）—— 架构保留 `RequiredStations` 多工作台扩展点，但本期只做一个

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/026-crafting-system.md`
写一份交付记录（实现要点、自测结果、需 Claude 重点确认的事项）。
