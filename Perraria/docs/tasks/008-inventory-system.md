# 任务 008 - 背包系统

## 目标
实现运行时物品管理：ItemStack（物品栈）数据结构、Inventory（背包）组件、快捷栏选择逻辑，
并将其接入已有的 PlayerBlockInteraction，使挖掘产出物品进入背包、放置消耗背包中的方块物品。
本任务只做数据层和逻辑层，不做 UI 渲染（留给 009）。

## 设计概要

### 背包结构
- 总计 40 个槽位：前 10 格为快捷栏（Hotbar），后 30 格为背包主体
- 每个槽位存储一个 ItemStack（物品引用 + 数量）
- 空槽位用 `ItemStack.Empty` 表示（Item == null, Count == 0）

### 物品添加规则（AddItem）
1. **优先合并**：遍历所有槽位，找到相同物品（按 ItemId 比较）且未满栈的槽位，尽可能合并
2. **再填空位**：剩余数量放入第一个空槽位
3. **背包已满**时返回实际未能添加的剩余数量（0 表示全部添加成功）

### 快捷栏选择
- 数字键 1–9 对应槽位 0–8，数字键 0 对应槽位 9
- 鼠标滚轮上/下循环切换（向上 = 索引 -1，向下 = 索引 +1，首尾循环）
- 选中槽位决定右键放置时使用的物品

### PlayerBlockInteraction 改动
- **挖掘完成**：通过 `BlockDataRegistry.GetDropItem()` 获取掉落物品 → 调用 `Inventory.AddItem()`
- **右键放置**：读取当前选中槽位物品 → 检查是否为 Block 类型 → 消耗 1 个 → 放置对应方块
- **移除** `_placeBlockType` 字段（不再硬编码放置类型）

## 接口签名

```csharp
// === Items/ItemStack.cs ===
// 运行时物品栈：一个槽位中的物品及其数量
[System.Serializable]
public struct ItemStack
{
    public ItemData Item;
    public int Count;

    public bool IsEmpty => Item == null || Count <= 0;

    public ItemStack(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }

    public static ItemStack Empty => default;
}
```

```csharp
// === Items/Inventory.cs ===
// 背包组件，挂载在 Player 游戏对象上
public sealed class Inventory : MonoBehaviour
{
    public const int TotalSlots = 40;
    public const int HotbarSlots = 10;

    // 槽位数组，[SerializeField] 使 Inspector 可见，便于调试验证
    [SerializeField] private ItemStack[] _slots;   // Awake 中初始化为 TotalSlots 长度

    private int _selectedHotbarIndex;

    // 事件：UI 层（009）订阅用
    public event System.Action<int> OnSlotChanged;            // 参数：变更的槽位索引
    public event System.Action<int> OnSelectedHotbarChanged;  // 参数：新选中的快捷栏索引

    /// <summary>当前选中的快捷栏索引 (0-9)</summary>
    public int SelectedHotbarIndex => _selectedHotbarIndex;

    /// <summary>读取指定槽位内容，越界返回 Empty</summary>
    public ItemStack GetSlot(int index);

    /// <summary>获取当前选中快捷栏槽位的内容</summary>
    public ItemStack GetSelectedItem();

    /// <summary>
    /// 添加物品到背包。自动合并同类已有栈，再填充空槽位。
    /// 返回未能添加的剩余数量（0 = 全部成功）。
    /// </summary>
    public int AddItem(ItemData item, int count = 1);

    /// <summary>
    /// 从指定槽位移除指定数量。数量归零时槽位变为 Empty。
    /// 返回 false 表示槽位为空或数量不足（不做部分扣减）。
    /// </summary>
    public bool RemoveFromSlot(int slotIndex, int count = 1);

    /// <summary>交换两个槽位的内容</summary>
    public void SwapSlots(int indexA, int indexB);

    /// <summary>选择快捷栏槽位 (0-9)，越界时 clamp</summary>
    public void SelectHotbar(int index);
}
```

### PlayerBlockInteraction 改动

```csharp
// === Player/PlayerBlockInteraction.cs ===
// 以下为需要修改的部分，非完整文件

public sealed class PlayerBlockInteraction : MonoBehaviour
{
    // ── 新增字段 ──
    [SerializeField] private Inventory _inventory;

    // ── 移除字段 ──
    // [SerializeField] private BlockType _placeBlockType = BlockType.Dirt;  ← 删除此字段

    // ── Update() 中新增快捷栏输入处理 ──
    // 读取数字键 1-0 → 调用 _inventory.SelectHotbar()
    // 读取鼠标滚轮 → 循环切换快捷栏
    // 使用 Keyboard.current 和 Mouse.current.scroll，与现有输入风格一致

    // ── ContinueMining() 改动 ──
    // 挖掘完成时（_miningProgress >= 1f），在 SetBlock(Air) 之前：
    //   BlockType minedType = _tileManager.GetBlock(minedCell);
    //   ItemData dropItem = _blockDataRegistry.GetDropItem(minedType);
    //   if (dropItem != null) _inventory.AddItem(dropItem, 1);

    // ── PlaceBlock() 改动 ──
    // 替换原有逻辑，改为从背包获取：
    //   ItemStack selected = _inventory.GetSelectedItem();
    //   if (selected.IsEmpty || selected.Item.Type != ItemType.Block) return;
    //   if (IsPlayerOccupyingCell(cellPosition)) return;
    //   _inventory.RemoveFromSlot(_inventory.SelectedHotbarIndex, 1);
    //   _tileManager.SetBlock(cellPosition, selected.Item.PlaceBlockType);
}
```

## 依赖
- `ItemData` — 物品数据 ScriptableObject ✅ 已实现 (007)
- `ItemType` — 物品类型枚举 ✅ 已实现 (007)
- `BlockType` — 方块类型枚举 ✅ 已实现
- `BlockDataRegistry` — 方块掉落映射 (`GetDropItem`) ✅ 已实现 (007)
- `PlayerBlockInteraction` — 挖掘/放置入口 ✅ 已实现 (004)
- `TileManager` — 方块读写 ✅ 已实现 (003)

## 文件清单
- `Assets/Scripts/Items/ItemStack.cs` — 新增，物品栈结构体
- `Assets/Scripts/Items/Inventory.cs` — 新增，背包组件
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — 修改，接入背包 + 快捷栏输入

## 场景配置
完成代码后需在 Unity 中：
1. 在 Player 游戏对象上添加 `Inventory` 组件
2. 在 `PlayerBlockInteraction` 的 Inspector 中将 Player 上的 `Inventory` 组件拖入 `_inventory` 字段

## 验收标准

### ItemStack
- [ ] `IsEmpty` 对 Item 为 null 或 Count ≤ 0 返回 true
- [ ] 构造函数正确设置 Item 和 Count

### Inventory 数据操作
- [ ] Awake 后 40 个槽位全部为 Empty
- [ ] `AddItem` 优先合并到同类已有栈，不超过 `MaxStackSize`
- [ ] `AddItem` 合并后仍有剩余时填入第一个空槽位
- [ ] `AddItem` 背包全满时返回 > 0 的剩余数量
- [ ] `RemoveFromSlot` 正确扣减数量，扣至 0 时槽位变为 Empty
- [ ] `RemoveFromSlot` 数量不足时返回 false 且不做部分扣减
- [ ] `SwapSlots` 正确交换两个槽位内容
- [ ] 所有数据变更操作触发 `OnSlotChanged` 事件
- [ ] `GetSlot` 越界时返回 Empty 而不是抛异常

### 快捷栏选择
- [ ] `SelectHotbar` 正确切换索引，越界时 clamp 到 0–9
- [ ] `SelectHotbar` 触发 `OnSelectedHotbarChanged` 事件
- [ ] 数字键 1–9 对应槽位 0–8，数字键 0 对应槽位 9
- [ ] 鼠标滚轮可循环切换快捷栏（首尾相连）

### 挖掘 → 背包
- [ ] 挖掘方块后，对应物品进入背包（Inspector 中 `_slots` 数组可见验证）
- [ ] 方块无 DropItem 配置时（null），挖掘后不添加物品

### 背包 → 放置
- [ ] 选中槽位有方块物品时，右键放置成功，数量减 1
- [ ] 物品用完（数量归 0）后槽位变为 Empty，无法继续放置
- [ ] 选中非 Block 类型物品或空槽位时，右键不触发放置

### 通用
- [ ] `_placeBlockType` 字段已从 PlayerBlockInteraction 中移除
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项
- **不做 UI**：本任务不渲染任何 UI 元素。验证通过 Inspector 查看 `_slots` 数组或 MCP 截图。快捷栏和背包界面留给 009
- **不做掉落实体**：挖掘后物品直接进入背包，不生成场景中的掉落物实体。掉落物留给 010
- **背包满时静默丢弃**：`AddItem` 返回剩余数量但调用方不需要处理（不做掉落到地面）。后续任务可改为掉出
- **事件仅预留**：`OnSlotChanged` / `OnSelectedHotbarChanged` 本任务中无订阅者，只需确保正确触发
- **输入风格一致**：快捷栏输入使用 `Keyboard.current` / `Mouse.current.scroll`，与 PlayerBlockInteraction 中已有的 `Mouse.current` 风格保持一致，不使用 Input System Action Map
- **Inventory 的 `_slots` 用 `[SerializeField]`**：使 Inspector 可见，方便调试验证和 MCP 检查
- **ContinueMining 中获取方块类型的时序**：必须在 `SetBlock(Air)` 之前读取被挖掘方块的类型，否则已被覆盖为 Air
