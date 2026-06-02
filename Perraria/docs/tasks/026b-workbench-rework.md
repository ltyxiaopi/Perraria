# 任务 026b - 工作台改版（2 格宽方块 + 独立工作台 UI + 右键打开）

> 这是对已合并的 [026 合成系统](026-crafting-system.md)（PR #25）的一次**改版**。
> 用户人手试玩后提出三点改动，已逐项与用户确认（见下「设计决策」）。
> **026 已落地的 `Recipe` / `CraftingService` / `RecipeSlotUI` / `WorkbenchProximity` 等纯逻辑保持不变，本任务只改"工作台形态"与"合成入口"。**

## 背景（用户反馈）
1. 工作台放到地上**太小**，希望做成 **2 格宽 × 1 格高**（长 = 两个方块，高 = 一个方块）。
2. 和背包一起的合成 UI **只保留"用木块制作工作台"这一条**配方。
3. 木剑 / 木箭 / 木镐移到一个**独立的工作台 UI**：玩家走到工作台**一定距离内、用鼠标点击工作台**才打开这个独立 UI，再在里面合成。

## 设计决策（已与用户确认 2026-06-02）

| 决策点 | 结论 |
| --- | --- |
| 工作台形态 | **真·双格方块**（占左右 2 个格子）。沿用现有 Tilemap 方块系统，**不引入 027 家具系统**。 |
| 双格实现方式 | **配对 BlockType**：`Workbench`(=6，左/锚点，渲染加宽贴图) + 新增 `WorkbenchRight`(=7，右/从属，透明贴图、仍是实心)。两格各自独立存进 `WorldData`，**存读档零改动**（每格就是个普通方块，自动随现有方块存档往返）。 |
| 加宽视觉 | **不新增 32×16 拉伸 PNG**，改用 `WorkbenchTile` 的 `m_Transform` x 轴缩放 ×2（`e00=2`），让 16×16 贴图在世界里渲染成 2 格宽。物品图标仍用同一张 16×16（方形槽位正常显示）。 |
| 工作台贴图 | 换成用户选定的**大号抽屉工作桌** `roguelikeSheet` **col24, row5**（比原来的 col23 更像工作台），重切 `workbench.png`（16×16）。详见「美术素材」。 |
| 打开工作台 UI | **右键点击工作台格子**（在交互范围内）→ 打开独立 `WorkbenchUI`。左键按住仍可挖回工作台（保留现有挖掘）。 |
| 背包合成入口 | 背包面板（Tab）**只剩 `Recipe_Workbench` 一条**（手工合成，任何地方可做）。其余 3 条移走。 |
| 独立工作台 UI | 新增 `WorkbenchUI` 面板 + 脚本，承载木剑/木箭/木镐 3 条配方。复用现有 `RecipeSlotUI` / `CraftingService`。玩家走出范围自动关闭；Esc / 关闭按钮可手动关闭。 |

---

## 一、工作台改成双格方块

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
    Workbench = 6,       // 左/锚点格：渲染 2 格宽贴图
    WorkbenchRight = 7   // 新增：右/从属格，透明贴图、实心、与左格联动
}
```
> 约定：一台工作台 = 相邻两格 `[Workbench, WorkbenchRight]`（锚点在**左**，从属在**右** = 锚点 x+1）。

### 2. `TileRegistry`（`Assets/Scripts/World/TileRegistry.cs`）
- 新增一个 `WorkbenchRight` → `_workbenchRightTile` 映射。
- `_workbenchRightTile` = 一个**透明 Tile**（`m_Sprite` 为空 / 全透明），但 `m_ColliderType = Grid`（整格碰撞，保证右半边也是实心、可站立、不能再放方块）。

### 3. `WorkbenchTile` 资产（加宽视觉）
- 贴图换成新的 `workbench.png`（16×16，col24 见美术节）。
- 把 `m_Transform` 的 `e00` 从 `1` 改成 **`2`**（x 轴缩放 ×2），pivot 保持 `(0,0)` 底部左对齐 → 该 16×16 贴图在世界里从锚点格左下角向**右铺满 2 格、向上铺满 1 格**，接地。
- `m_ColliderType` 保持 **Grid（整格盒碰撞，1 格）**；右半边碰撞由 `WorkbenchRight` 的透明 Tile 提供。两格合起来 = 2×1 实心，视觉来自锚点格的加宽贴图。
> 即：**视觉**靠锚点格一张加宽贴图覆盖 2 格；**碰撞/占位**靠两格各自的整格碰撞。互不依赖，不会双重渲染。

### 4. `BlockDataRegistry` 登记（`Assets/Data/...` 资产里加两条）
- `Workbench`：硬度沿用原值（与 Wood 接近，可快速挖回）、**DropItem = `Item_Workbench`、DropChance = 1**。
- `WorkbenchRight`：硬度同上、**DropItem = 空、DropChance = 0/无掉落**（掉落只由锚点格负责，避免双倍掉落）。

### 5. 放置逻辑（`Assets/Scripts/Player/PlayerBlockInteraction.cs` 的 `PlaceBlock`）
当选中物品的 `PlaceBlockType == BlockType.Workbench` 时，走**双格放置**分支：
- 设点击格 = **锚点（左）**，右格 = 锚点 x+1。
- 校验：**锚点格与右格都必须是 `Air`、都 `InBounds`、都不被玩家占据**（`IsPlayerOccupyingCell` 对两格都查）。任一不满足 → 放置失败、不扣物品。
- 通过则：`SetBlock(锚点, Workbench)` + `SetBlock(右格, WorkbenchRight)`，扣 1 个 `Item_Workbench`。
- 其它（非工作台）方块的放置逻辑**保持原样**。
> 实现提示：可加一个小工具 `bool IsWorkbenchItem(ItemData)` / 在 `PlaceBlock` 里按 `PlaceBlockType == Workbench` 分流，避免污染通用放置路径。
> 可选打磨（非必须）：手持 `Item_Workbench` 时高亮**两格**预览（现有 `UpdateHighlight` 只高亮 1 格）。

### 6. 挖掘逻辑（`PlayerBlockInteraction` 挖掘完成处 `ContinueMining`）
现有挖掘按"点击的那一格"计算硬度/进度。**完成时**改为：
- 若挖掉的格是 `Workbench`：同时把 **右邻格**（若为 `WorkbenchRight`）一并 `SetBlock(Air)`。
- 若挖掉的格是 `WorkbenchRight`：同时把 **左邻格**（若为 `Workbench`）一并 `SetBlock(Air)`。
- **只在锚点格 `SpawnItemDrop` 一次**（掉 1 个 `Item_Workbench`）；从属格不掉落（靠 §4 的 DropChance=0 兜底，但代码上也只对锚点调用掉落）。
- 其它方块的挖掘**保持原样**。
> 注意先取 `dropItem`/算掉落、再清两格，避免清完取不到 BlockType。两格相邻、同一台，不存在歧义（锚点恒在左）。

### 7. 右键点击工作台 → 打开独立 UI（`PlayerBlockInteraction`）
在右键的处理路径里（两个分支：`IsActionItemSelected()` 内的右键、以及常规右键），**在调用 `PlaceBlock` 之前**先判断：
```
若 (右键 && isInRange) {
    var bt = _tileManager.GetBlock(targetCell);
    if (bt == BlockType.Workbench || bt == BlockType.WorkbenchRight) {
        _workbenchUI.Open();   // 命中工作台任一格 → 开 UI，吃掉这次右键，不放方块
        return;
    }
    PlaceBlock(targetCell);    // 否则照常放方块
}
```
- 新增 `[SerializeField] private WorkbenchUI _workbenchUI;` 引用（场景里挂）。
- 复用现有 `_interactionRange`（点击超距无效）；UI 已在 `IsPointerOverGameObject()` 时屏蔽挖矿/放置，避免点 UI 误操作。

---

## 二、合成 UI 拆分

### 8. 背包面板只留工作台配方（`Assets/Scripts/UI/InventoryUI.cs`）
- `InventoryUI` 代码是**配方数据驱动**的，核心逻辑无需大改。改动主要在：
  - 场景里把 `_recipes` / `_recipeSlots` 从 4 条缩成 **1 条 = `Recipe_Workbench`**（手工合成，`RequiredStations` 为空，任何地方可合成）。
  - 移除背包面板里多余的 3 个 `RecipeSlotUI` 行（只留 1 行）。
  - `_workbenchProximity` 引用在背包侧不再需要（工作台配方是手工合成）；可解绑或保留为空（保留 `IsStationAvailable` 对无台配方恒返回 true 的现有逻辑即可，不报错）。
- 目标表现：打开背包 **只显示"工作台（10 木）"一条**，材料够则可合成。

### 9. 新增独立工作台 UI（`Assets/Scripts/UI/WorkbenchUI.cs` + 新面板）
新建 `WorkbenchUI`（参考 `InventoryUI` 的"配方区"部分，但是**独立面板、自管开关**）：
```csharp
[DisallowMultipleComponent]
public sealed class WorkbenchUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Recipe[] _recipes;            // 木剑 / 木箭 / 木镐
    [SerializeField] private RecipeSlotUI[] _recipeSlots;
    [SerializeField] private WorkbenchProximity _workbenchProximity; // 走出范围自动关
    [SerializeField] private TMP_Text _craftingFeedbackText;

    public bool IsOpen { get; }
    public void Open();      // 显示面板、刷新配方
    public void Close();     // 隐藏面板
    // Update：若 IsOpen 且 !_workbenchProximity.IsNearWorkbench() → Close()；Esc → Close()
}
```
- **配方**：`Recipe_WoodSword` / `Recipe_WoodArrow` / `Recipe_WoodPickaxe`（沿用 026 已有的 3 个资产）。
- **合成**：复用现有 `CraftingService` + `RecipeSlotUI.OnClicked`。因为 UI 只在工作台旁能打开，调用 `TryCraft(recipe, inventory, stationAvailable: true)`；**材料是否充足仍要 gate**（不够则灰显，沿用 `RecipeSlotUI` 现有高亮/灰显）。
- **打开**：由 §7 的右键命中工作台触发 `Open()`。
- **关闭**：①Esc；②点关闭按钮（可选）；③`Update` 里检测玩家走出工作台范围（`WorkbenchProximity.IsNearWorkbench()` 为 false）自动 `Close()`。
- **刷新**：`Open()` 时刷新一次；合成成功后刷新；背包变动（`Inventory.OnSlotChanged`）时刷新配方可用性。**不要每帧无脑刷**（026 遗留项之一就是 InventoryUI 每帧刷，本次新面板别重蹈，按事件 + Open 刷即可）。
- **无需光标拖拽系统**：`WorkbenchUI` 只有配方行（`RecipeSlotUI` 自带 `IPointerClickHandler` 点击合成），不涉及背包格拖拽，**不要**引入第二套 cursor。
- 面板视觉沿用现有配色 / `RecipeSlotUI` 风格，**不引入新美术**。

> 背包面板（Tab）与工作台面板（右键）是两个独立面板，可不互斥；但建议打开工作台面板时若背包开着可顺手关掉背包，避免叠面板（**可选**，不强求）。

---

## 三、美术素材（换工作台贴图，无新增外部资源）

**源图**：`Assets/Art/Tilesets/blocks/Spritesheet/roguelikeSheet_transparent.png`（Kenney Roguelike/RPG，CC0，**已在仓库、credits 已登记**）。
- 该图 16×16 瓦片、瓦片间 1px 间隔 → 步距 17px；瓦片 (col,row) 像素位置 = `x = col*17, y = row*17`，尺寸 16×16。
- **新工作台 = 大号抽屉工作桌**：`col=24, row=5` → 像素矩形 **`x=408, y=85, w=16, h=16`**（双抽屉、比原 col23 更像工作台；用户已看图确认）。
- **落地**：重切覆盖现有 `Assets/Art/Tilesets/blocks/workbench.png`（16×16），导入设置**对齐现有方块贴图**（PPU 100、`filterMode=Point`、`alignment` 用底部左 `(0,0)` 与现有一致）。
- 同一张 16×16 既给 `WorkbenchTile`（靠 §3 的 `e00=2` 渲成 2 格宽），也给 `Item_Workbench.Icon`（方形槽位用 16×16 正常显示）。
- **加宽靠 Tile 变换，不靠 PNG 拉伸**：仓库根目录 `tmp_stretch.png` 是当时给用户看的拉伸预览（仅参考、可删），**实际不要**生成拉伸 PNG。
> 授权：col23→col24 仍属同一张 Kenney CC0 图集，`docs/credits.md` **无需新增**。

---

## 文件清单
- `Assets/Scripts/World/BlockType.cs`（改：+`WorkbenchRight=7`）
- `Assets/Scripts/World/TileRegistry.cs`（改：+`WorkbenchRight` → 透明 Tile 映射）
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`（改：工作台双格放置 / 双格挖掘联动 / 右键命中工作台开 UI）
- `Assets/Scripts/UI/InventoryUI.cs`（改：配方缩为 1 条工作台；解除工作台 proximity 依赖）
- `Assets/Scripts/UI/WorkbenchUI.cs`（新：独立工作台合成面板）
- `Assets/Art/Tilesets/blocks/workbench.png`（改：重切 col24,row5 = x408,y85,16×16）
- `Assets/Art/Tilesets/Tiles/WorkbenchTile.asset`（改：`m_Transform.e00=2`，换新 sprite）
- 新增 `WorkbenchRightTile`（透明 Tile 资产）+ 注册进 `TileRegistry`
- `BlockDataRegistry` 资产（改：加 `Workbench`/`WorkbenchRight` 两条；Workbench 掉 `Item_Workbench`，Right 不掉）
- 场景：新建 `WorkbenchUI` 面板（含 3 个 `RecipeSlotUI` 行 + 关闭逻辑），挂引用；`PlayerBlockInteraction._workbenchUI` 绑定；背包面板配方区缩为 1 行
- `Recipe` / `CraftingService` / `RecipeSlotUI` / `WorkbenchProximity` / `Item_Workbench` / 4 个 Recipe 资产 / `ItemData` / `PlayerCombat`：**不改**（沿用 026）

## 验收标准
- [ ] 放置 `Item_Workbench`（右键空地）→ 占**左右 2 格**、渲染成 **2 格宽 × 1 格高**接地工作台；右半边是实心（不能再放方块、能站上去）
- [ ] 锚点格右边一格被占（非 Air）时放置失败、不扣物品；越界 / 玩家身体占住任一格时同样失败
- [ ] 挖掉工作台**任一格**（左或右）→ 两格一起消失、**只掉 1 个** `Item_Workbench`
- [ ] 工作台**存读档**：保存后重载，2 格仍正确还原成 2 格宽工作台（复用现有方块存档，验证一次）
- [ ] 打开背包（Tab）→ **只显示 1 条**"工作台（10 木）"配方，材料够可手工合成
- [ ] 走到工作台**交互范围内、右键点击工作台**（左或右格）→ 打开**独立工作台 UI**；范围外点击不打开
- [ ] 工作台 UI 内显示木剑 / 木箭 / 木镐 3 条；材料够则可合成、不够灰显；合成正确扣材料 + 加产出（木箭 1 木 → 5 箭）
- [ ] 合成产出物使背包满塞不下 → 合成被拒、材料不扣（原子性，沿用 `CraftingService`），UI 提示
- [ ] 工作台 UI 打开时玩家走出范围 → **自动关闭**；Esc 可手动关闭
- [ ] 右键命中工作台时**不会**误放方块；非工作台格右键照常放方块；左键按住照常挖矿（含挖工作台）
- [ ] MCP 验证：**0 编译错误、0 新增控制台报错**；场景实测完整闭环（手搓工作台 → 放置 2 格 → 右键开 UI → 合成木剑/箭/镐 → 挖回）

## 不做的事（本期范围外）
- 027 家具基类 / 通用多格占用框架（本次只为工作台做配对 BlockType，不抽象通用家具）
- 工作台分级（铁砧等）/ 新材料 / 新配方（沿用 026 的 4 条）
- 合成动画 / 进度条 / 拖拽合成 / Shift 批量
- 工作台 UI 与背包的深度联动（共享拖拽、并排显示材料格等）——本期工作台 UI 是独立配方列表

## 交付记录（Codex 必填）
完成并自测通过后、**push 分支前**，在 `docs/codex-reports/026b-workbench-rework.md` 写交付记录
（实现要点、双格放置/挖掘的边界处理、自测结果、需 Claude 重点确认的事项）。
