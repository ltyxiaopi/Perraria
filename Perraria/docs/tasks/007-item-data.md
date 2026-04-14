# 任务 007 - 物品数据定义

## 目标
建立物品系统的数据层：定义物品类型枚举、单个物品的数据结构（ScriptableObject）、物品注册表，
以及方块→物品的掉落映射。这是背包、掉落拾取、快捷栏、战斗等后续系统的基础依赖，本任务不涉及
运行时逻辑（背包操作、掉落实体等留给后续任务）。

## 设计概要

### 物品类型
v1.0 需要的物品类型：
- **Block** — 方块物品，可放置到世界中（Dirt、Stone 等）
- **Tool** — 工具，影响挖掘速度（镐）
- **Weapon** — 武器，影响攻击伤害（剑）
- **Material** — 材料，用于合成或无特殊功能的掉落物

### 物品数据字段
每个物品是一个 ScriptableObject 实例，包含：
| 字段 | 类型 | 说明 |
|------|------|------|
| ItemId | int | 唯一标识符，手动分配，不可重复 |
| ItemName | string | 物品显示名称 |
| Description | string | 物品描述文本 |
| Icon | Sprite | 物品图标（UI 显示用） |
| Type | ItemType | 物品类型枚举 |
| MaxStackSize | int | 最大堆叠数量（默认 99，工具/武器为 1） |
| PlaceBlockType | BlockType | 仅 Block 类型有效，放置时对应的方块类型；非 Block 类型设为 Air |

### 物品注册表
`ItemDatabase` 是一个 ScriptableObject，持有所有 `ItemData` 的引用数组，提供按 ID 查询的接口。

### 方块→物品映射
扩展现有 `BlockDataRegistry.BlockData` 结构，新增 `DropItem` 字段（`ItemData` 引用），
表示该方块被挖掘后掉落的物品。Air 的 DropItem 为 null。

## 接口签名

```csharp
// === Items/ItemType.cs ===
public enum ItemType : byte
{
    Block    = 0,
    Tool     = 1,
    Weapon   = 2,
    Material = 3
}

// === Items/ItemData.cs ===
// 单个物品的静态定义，每种物品一个资产实例
[CreateAssetMenu(fileName = "New Item", menuName = "Perraria/Item Data")]
public sealed class ItemData : ScriptableObject
{
    [SerializeField] private int _itemId;
    [SerializeField] private string _itemName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ItemType _type;
    [SerializeField] private int _maxStackSize = 99;
    [SerializeField] private BlockType _placeBlockType = BlockType.Air;

    public int ItemId => _itemId;
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemType Type => _type;
    public int MaxStackSize => _maxStackSize;
    public BlockType PlaceBlockType => _placeBlockType;
}

// === Items/ItemDatabase.cs ===
// 全局物品注册表，按 ID 查询物品定义
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Perraria/Item Database")]
public sealed class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemData[] _items;

    /// <summary>按 ItemId 查找物品定义，未找到返回 null</summary>
    public ItemData GetItemById(int itemId);

    /// <summary>获取所有已注册物品（只读）</summary>
    public ReadOnlySpan<ItemData> GetAllItems();
}
```

### BlockDataRegistry 改动

```csharp
// === World/BlockDataRegistry.cs ===
// 在现有 BlockData 结构中新增掉落物品字段
[System.Serializable]
public struct BlockData
{
    public BlockType Type;
    public float Hardness;
    public ItemData DropItem;   // 新增：挖掘后掉落的物品，null 表示无掉落
}

// 新增查询方法
public ItemData GetDropItem(BlockType type);
```

## 依赖
- `BlockType` — 方块类型枚举 ✅ 已实现
- `BlockDataRegistry` — 需要扩展，新增 DropItem 字段和查询方法

## 文件清单
- `Assets/Scripts/Items/ItemType.cs` — 新增，物品类型枚举
- `Assets/Scripts/Items/ItemData.cs` — 新增，物品数据 ScriptableObject
- `Assets/Scripts/Items/ItemDatabase.cs` — 新增，物品注册表
- `Assets/Scripts/World/BlockDataRegistry.cs` — 修改，BlockData 新增 DropItem 字段 + GetDropItem 方法
- `Assets/Data/Items/` — 新增目录，存放 ItemData 资产实例
- `Assets/Data/ItemDatabase.asset` — 新增，ItemDatabase 资产实例

## 资产配置
完成代码后需在 Unity 中创建以下 ScriptableObject 资产：

### ItemData 资产（v1.0 初始物品）
| 资产名 | ItemId | ItemName | Type | MaxStackSize | PlaceBlockType |
|--------|--------|----------|------|-------------|----------------|
| Item_Dirt | 1 | Dirt | Block | 99 | Dirt |
| Item_Grass | 2 | Grass Block | Block | 99 | Grass |
| Item_Stone | 3 | Stone | Block | 99 | Stone |

> Icon 字段暂时留空（后续美术任务补充），不影响数据层功能。
> 工具和武器物品在战斗系统任务中再添加。

### ItemDatabase 资产
- 创建 `Assets/Data/ItemDatabase.asset`
- 将上述 3 个 ItemData 拖入 `_items` 数组

### BlockDataRegistry 更新
- 在已有的 BlockDataRegistry 资产中，为 Dirt / Grass / Stone 的 BlockData 分别关联对应的 ItemData

## 验收标准
- [ ] `ItemType` 枚举包含 Block、Tool、Weapon、Material 四个值
- [ ] `ItemData` ScriptableObject 可在 Inspector 中创建和编辑，所有字段正确暴露
- [ ] `ItemDatabase.GetItemById()` 能正确返回对应物品，ID 不存在时返回 null
- [ ] `BlockDataRegistry.BlockData` 包含 `DropItem` 字段，Inspector 中可赋值
- [ ] `BlockDataRegistry.GetDropItem()` 能正确返回方块的掉落物品
- [ ] 已创建 Dirt、Grass、Stone 三个 ItemData 资产，字段正确
- [ ] 已创建 ItemDatabase 资产并注册上述 3 个物品
- [ ] BlockDataRegistry 资产中 Dirt/Grass/Stone 已关联对应 DropItem
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 符合 `coding-conventions.md` 规范（文件结构、命名、SerializeField 等）

## 注意事项
- **ScriptableObject 资产路径**：代码中的定义放 `Assets/Scripts/Items/`，资产实例放 `Assets/Data/Items/`，符合 coding-conventions.md 中"定义放模块目录，实例放 Data"的约定
- **ItemId 唯一性**：v1.0 不做运行时唯一性校验（物品少，手动管理足够），但 `GetItemById` 遇到重复 ID 应返回第一个匹配项并在 Editor 下 `Debug.LogWarning`
- **不要过度设计**：本任务只做数据定义，不做运行时物品实例（ItemStack）、不做背包逻辑、不做掉落实体。这些留给 008-010
- **BlockDataRegistry 向后兼容**：只新增字段，不修改现有字段，不破坏已有的 Hardness 数据。已有的 CSV 导入器如果受影响需要同步更新
- **ReadOnlySpan vs 数组**：如果 Unity 版本不支持 `ReadOnlySpan<T>` 作为 ScriptableObject 的返回值，改用 `IReadOnlyList<ItemData>` 即可
