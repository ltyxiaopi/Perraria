# 任务 027 - 家具放置系统【骨架版】

> ⚠️ **实施前必须补全**
> 本规范是**远期骨架版**，仅记录目标 / 依赖 / 验收骨架。**真正轮到 026 实施前，必须重新检查并补全**：
> - [ ] 家具占用多格的实现（Tilemap 多格 vs 独立 GameObject）
> - [ ] 家具与玩家 / 敌人的物理交互（可踩 / 可穿过）
> - [ ] 家具拆除规则（破坏后是否回收物品）
> - [ ] 家具的"激活"行为（工作台开 UI / 床睡觉 / 椅子坐下）
> - [ ] 完整家具清单（桌、椅、床、门、灯、储物箱、工作台...）
>
> **执行 Codex 实现前，由 Claude Code 重新审视并产出完整版规范替换本文件。**

## 目标
1. 引入"家具"物品类型（区别于 Block 单格瓦片）：可占用多格、有特殊行为、放置后是独立 GameObject 而非 Tilemap tile。
2. 玩家从快捷栏选中家具物品，鼠标点击合法位置 → 放置家具实体。
3. 家具占用的方块网格上，玩家不能再放置普通 Block（碰撞检查）。
4. 拆除家具（镐子挖？专用拆除键？待决定）回收物品。
5. 至少实现 4 种家具：工作台、椅子、桌子、床。

## 设计概要（粗略）

### Furniture 物品类型
扩展 `ItemType`：
```csharp
public enum ItemType : byte
{
    Block = 0,
    Tool = 1,
    Weapon = 2,
    Material = 3,
    Consumable = 4,
    Furniture = 5,   // 新增
}
```

`ItemData` 新增字段：
```csharp
[SerializeField] private GameObject _furniturePrefab;   // 放置时实例化的 prefab
[SerializeField] private Vector2Int _furnitureSize = new(1, 1);   // 占用网格大小
```

### Furniture MonoBehaviour 基类
```csharp
public abstract class Furniture : MonoBehaviour
{
    public Vector2Int Size { get; }
    public Vector2Int OriginCell { get; }
    public ItemData SourceItem { get; }   // 拆除时还原的物品

    public abstract void OnInteract(GameObject player);   // 玩家按 E / 右键交互
    public virtual void OnPlaced(Vector2Int origin) { }
    public virtual void OnRemoved() { }
}

public sealed class WorkbenchFurniture : Furniture { ... }
public sealed class ChairFurniture : Furniture { ... }
public sealed class BedFurniture : Furniture { ... }
public sealed class TableFurniture : Furniture { ... }
```

### 放置流程
1. 选中 Furniture 物品 → 鼠标显示半透明预览（占用网格高亮）
2. 鼠标位置满足条件（所有占用格都是 Air 且玩家不在占用格内）→ 高亮绿色，否则红色
3. 右键 → `Instantiate(prefab)`，记录 `OriginCell` 到 `FurnitureRegistry`，扣除 1 个物品
4. 占用的格子在 `FurnitureRegistry.IsCellOccupied` 中返回 true，`PlayerBlockInteraction.PlaceBlock` 检查这个

### FurnitureRegistry
全局家具注册表，跟踪所有已放置家具的占用网格：
```csharp
public sealed class FurnitureRegistry : MonoBehaviour
{
    public static FurnitureRegistry Instance { get; }
    public bool IsCellOccupied(Vector2Int cell);
    public Furniture GetFurnitureAt(Vector2Int cell);
    public void RegisterFurniture(Furniture furniture);
    public void UnregisterFurniture(Furniture furniture);
}
```

### 拆除
**待 026 实施前确定**：
- 选项 A：选中"无物品"槽 + 镐子，左键点击家具 → 拆除（语义和挖方块一致）
- 选项 B：专用拆除键（E + 长按）
- 选项 C：右键家具弹出菜单"拆除 / 交互"

### 存档
家具放置状态需要进 SaveData：
```csharp
public sealed class FurnitureSaveData
{
    public int ItemId;
    public Vector2Int OriginCell;
    public string FurnitureStateJson;   // 子类自定义状态（如储物箱内容）
}
```
`SaveData` 根新增 `List<FurnitureSaveData> Furnitures`。

## 依赖（候选）
- 任务 008 Inventory（消耗物品）
- 任务 020 ItemData（新增字段）
- 任务 023 ItemType（已加 Consumable，本任务加 Furniture）
- 任务 026 合成系统（家具大多通过合成获得）
- 任务 018 SaveData（新增 Furnitures 字段）

## 文件清单（候选）
- `Assets/Scripts/Furniture/Furniture.cs`（基类）
- `Assets/Scripts/Furniture/WorkbenchFurniture.cs` / `ChairFurniture.cs` / `BedFurniture.cs` / `TableFurniture.cs`
- `Assets/Scripts/Furniture/FurnitureRegistry.cs`
- `Assets/Scripts/Player/PlayerFurniturePlacer.cs`（处理预览 / 放置输入）
- `Assets/Prefabs/Furniture/`（多个 prefab）
- `Assets/Data/Items/Item_Workbench.asset` 等

## 验收标准（骨架）
- [ ] 选中家具物品后鼠标显示放置预览
- [ ] 占用格不合法时预览红色，不能放置
- [ ] 占用格合法时预览绿色，右键放置成功
- [ ] 家具占用的格子在 `PlayerBlockInteraction` 内不能再放普通方块
- [ ] 拆除家具回收物品（具体实现见拆除决策）
- [ ] 存档保存家具位置 + 类型，读档后恢复
- [ ] 家具与工作台 Recipe（025）联动：玩家在 Workbench 1.5 米内可合成需要工作台的配方

## 注意事项
- **026 实施前必须做的事**：
  1. 确定拆除机制（A/B/C 选哪个）
  2. 完整家具清单（首批 5-10 个）
  3. 床的"睡觉"行为是否做（涉及 024 时间跳到第二天）
  4. 储物箱（Chest）的存储 UI 是否独立任务
  5. 家具占用多格时的视觉对齐（pivot 中心 vs 左下角）
- **不做的事（026 范围内）**：
  - 家具旋转（先固定朝向）
  - 家具着色 / 染色
  - 家具组合规则（Terraria 的"高级椅子"等）
  - NPC 入住条件（属于 027 房屋系统）
  - 家具堆叠（一格放多个家具）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/027-furniture-placement.md`
写一份交付记录。

---

> 📝 **重要提醒**（贴顶强调）：本文件是骨架版，026 实施前 Claude Code 必须重新走一轮：
> 1. 与用户讨论拆除机制选项
> 2. 完整家具清单
> 3. 床 / 储物箱等"特殊家具"是否拆成子任务
> 4. 用完整版规范替换本文件再交给 Codex
