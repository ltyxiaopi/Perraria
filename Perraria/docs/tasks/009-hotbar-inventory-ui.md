# 任务 009 - 快捷栏 + 背包 UI

## 目标
实现快捷栏（HotbarUI）和背包（InventoryUI）的界面显示与交互，让玩家能够看到背包内容、
切换快捷栏选中项、以及通过点击在槽位之间移动物品。本任务使用 Unity UI (uGUI) + TextMeshPro。

## 设计概要

### 整体布局
```
┌──────────────────────────────────────────────────┐
│                   游戏画面                        │
│                                                  │
│                                                  │
│     ┌──────────────────────────────────┐         │
│     │     InventoryUI (Tab 切换)       │         │
│     │  ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐│         │
│     │  │ 0│ 1│ 2│ 3│ 4│ 5│ 6│ 7│ 8│ 9││ ← 快捷栏行 │
│     │  ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤│         │
│     │  │10│11│12│13│14│15│16│17│18│19││         │
│     │  ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤│         │
│     │  │20│21│22│23│24│25│26│27│28│29││         │
│     │  ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤│         │
│     │  │30│31│32│33│34│35│36│37│38│39││         │
│     │  └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘│         │
│     └──────────────────────────────────┘         │
│                                                  │
│  ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐                │
│  │ 0│ 1│ 2│ 3│ 4│ 5│ 6│ 7│ 8│ 9│  ← HotbarUI   │
│  └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘   (始终显示)    │
└──────────────────────────────────────────────────┘
```

- **HotbarUI**：屏幕底部居中，始终显示，10 个槽位（索引 0–9），高亮当前选中项
- **InventoryUI**：按 Tab 切换显示/隐藏，屏幕中央，4×10 网格（40 个槽位），含光标物品交互
- 两者显示相同的 Inventory 数据，通过事件保持同步

### 槽位显示 (SlotUI)
每个槽位包含：
- 背景图（默认半透明深色底框）
- 物品图标（读取 `ItemData.Icon`，为 null 时隐藏）
- 数量文字（`TMP_Text`，数量 ≤ 1 时隐藏）
- 选中高亮（仅 HotbarUI 使用，当前选中槽位显示边框高亮）

### 点击交互（仅 InventoryUI 打开时生效）
玩家通过左键点击在槽位之间移动物品，使用"光标物品"机制：

| 操作 | 光标状态 | 目标槽位 | 结果 |
|------|---------|---------|------|
| 点击 | 空 | 有物品 | 物品移到光标 |
| 点击 | 持有物品 | 空 | 光标物品放入槽位 |
| 点击 | 持有物品 A | 有物品 B（不同类） | 交换：A 放入槽位，B 到光标 |
| 点击 | 持有物品 A | 有物品 A（同类） | 合并：尽可能合入槽位（受 MaxStackSize 限制），溢出留在光标 |

- 光标物品跟随鼠标位置显示（浮动图标 + 数量）
- 关闭背包时，光标中的物品通过 `Inventory.AddItem()` 放回背包
- 点击交互对 HotbarUI 的槽位和 InventoryUI 的槽位均有效

### 输入
- **Tab 键**：切换背包面板开关
- 背包打开时，挖掘/放置操作不应响应 UI 区域的点击（通过 `EventSystem.IsPointerOverGameObject()` 阻止穿透）

## 接口签名

### 新增 Inventory 方法

```csharp
// === Items/Inventory.cs ===
// 新增方法，用于 UI 层直接设置槽位内容（点击交互需要）

/// <summary>
/// 直接设置指定槽位的内容。传入 ItemStack.Empty 清空槽位。
/// 越界时静默忽略。
/// </summary>
public void SetSlot(int index, ItemStack stack);
```

### SlotUI

```csharp
// === UI/SlotUI.cs ===
// 可复用的槽位 UI 组件，挂载在每个槽位的 GameObject 上
// 实现 IPointerClickHandler 以接收点击事件
public sealed class SlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private GameObject _selectionHighlight;

    /// <summary>点击事件，参数为槽位索引</summary>
    public event System.Action<int> OnClicked;

    /// <summary>本槽位在 Inventory 中的索引</summary>
    public int SlotIndex { get; }

    /// <summary>初始化槽位索引，由 HotbarUI / InventoryUI 调用</summary>
    public void Initialize(int slotIndex);

    /// <summary>刷新显示内容</summary>
    public void UpdateDisplay(ItemStack stack);

    /// <summary>设置选中高亮状态（仅快捷栏使用）</summary>
    public void SetSelected(bool selected);

    // IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData);
}
```

### HotbarUI

```csharp
// === UI/HotbarUI.cs ===
// 始终显示的快捷栏，纯显示 + 选中高亮，不处理点击逻辑
public sealed class HotbarUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private SlotUI[] _slots;   // 10 个 SlotUI 引用

    // Awake/Start 中初始化各 SlotUI 的索引
    // OnEnable 订阅 Inventory.OnSlotChanged 和 OnSelectedHotbarChanged
    // OnDisable 取消订阅
    // 收到 OnSlotChanged(index) 且 index < 10 时，更新对应 SlotUI
    // 收到 OnSelectedHotbarChanged(index) 时，更新高亮
}
```

### InventoryUI

```csharp
// === UI/InventoryUI.cs ===
// 背包面板：切换显示、40 槽位网格、光标物品、点击交互
public sealed class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private GameObject _panel;          // 背包面板根对象，切换 SetActive
    [SerializeField] private SlotUI[] _slots;            // 40 个 SlotUI 引用
    [SerializeField] private Image _cursorIcon;          // 光标物品图标
    [SerializeField] private TMP_Text _cursorCountText;  // 光标物品数量

    private ItemStack _cursorStack;                      // 光标持有的物品
    private bool _isOpen;

    /// <summary>背包是否处于打开状态</summary>
    public bool IsOpen => _isOpen;

    // Update 中检测 Tab 键切换面板
    // 面板打开时：光标物品跟随鼠标位置
    // 关闭面板时：光标物品通过 AddItem 放回背包

    // 槽位点击处理（订阅所有 SlotUI.OnClicked）：
    //   光标空 + 槽位有物品 → 拾取
    //   光标有物品 + 槽位空 → 放入
    //   光标有物品 + 槽位有不同物品 → 交换
    //   光标有物品 + 槽位有同类物品 → 合并（受 MaxStackSize 限制）
}
```

### PlayerBlockInteraction 改动

```csharp
// === Player/PlayerBlockInteraction.cs ===
// Update() 开头新增 UI 穿透检查

private void Update()
{
    // ... 现有的 null 检查 ...

    // 新增：鼠标在 UI 上时跳过挖掘和放置（快捷栏输入仍然响应）
    bool isPointerOverUI = EventSystem.current != null
        && EventSystem.current.IsPointerOverGameObject();

    HandleHotbarInput();  // 快捷栏切换不受 UI 影响

    if (isPointerOverUI)
    {
        ResetMining();
        ClearHighlight();
        return;         // 跳过高亮、挖掘、放置
    }

    // ... 后续原有逻辑不变 ...
}
```

## 依赖
- `Inventory` — 背包数据 + 事件 ✅ 已实现 (008)
- `ItemStack` — 物品栈结构 ✅ 已实现 (008)
- `ItemData` — 物品数据（Icon 字段）✅ 已实现 (007)
- `PlayerBlockInteraction` — 需添加 UI 穿透检查 ✅ 已实现 (004/008)
- TextMeshPro — ✅ 项目中已可用

## 文件清单
- `Assets/Scripts/UI/SlotUI.cs` — 新增，可复用槽位组件
- `Assets/Scripts/UI/HotbarUI.cs` — 新增，快捷栏显示
- `Assets/Scripts/UI/InventoryUI.cs` — 新增，背包面板 + 交互
- `Assets/Scripts/Items/Inventory.cs` — 修改，新增 `SetSlot` 方法
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — 修改，新增 UI 穿透检查

## 场景配置
完成代码后需在 Unity 中创建以下对象：

### 1. EventSystem
- 场景中当前无 EventSystem，需创建（GameObject > UI > Event System）
- 确保包含 `InputSystemUIInputModule`（项目使用 New Input System）

### 2. Canvas
- 创建 Canvas，设置：
  - Render Mode: Screen Space - Overlay
  - Canvas Scaler: Scale With Screen Size, Reference Resolution 1920×1080, Match Width Or Height = 0.5
- 添加 `GraphicRaycaster` 组件（创建 Canvas 时默认包含）

### 3. HotbarUI 层级
```
Canvas
└── HotbarPanel (锚定底部居中, HotbarUI 组件)
    ├── HorizontalLayoutGroup (spacing 4, childAlignment = MiddleCenter)
    └── Slot_0 ~ Slot_9 (各含 SlotUI 组件)
        ├── Background (Image, 半透明深色, 60×60)
        ├── Icon (Image, raycastTarget=false)
        ├── CountText (TMP_Text, 右下角, raycastTarget=false)
        └── SelectionHighlight (Image, 亮色边框, 默认隐藏)
```

### 4. InventoryUI 层级
```
Canvas
└── InventoryRoot (InventoryUI 组件)
    ├── Panel (锚定中央, 默认隐藏)
    │   ├── 半透明黑色背景
    │   └── GridLayoutGroup (cellSize 60×60, spacing 4, columns=10)
    │       └── Slot_0 ~ Slot_39 (各含 SlotUI 组件, 结构同上但无 SelectionHighlight)
    ├── CursorIcon (Image, raycastTarget=false, 默认隐藏)
    └── CursorCountText (TMP_Text, raycastTarget=false, 默认隐藏)
```

### 5. 引用绑定
- HotbarUI._inventory → Player 上的 Inventory 组件
- HotbarUI._slots → HotbarPanel 下的 Slot_0 ~ Slot_9
- InventoryUI._inventory → Player 上的 Inventory 组件
- InventoryUI._panel → Panel 对象
- InventoryUI._slots → Panel/Grid 下的 Slot_0 ~ Slot_39
- InventoryUI._cursorIcon → CursorIcon
- InventoryUI._cursorCountText → CursorCountText
- 每个 SlotUI 的 _background / _icon / _countText / _selectionHighlight → 对应子对象

## 验收标准

### SlotUI
- [ ] 有物品时显示图标（Icon 非 null）和数量（> 1 时显示），无物品时隐藏图标和数量
- [ ] Icon 为 null 时不报错，正常显示空状态
- [ ] 点击触发 OnClicked 事件，传递正确的 SlotIndex

### HotbarUI
- [ ] 始终显示在屏幕底部居中，10 个槽位
- [ ] 实时反映 Inventory 槽位 0–9 的内容变化
- [ ] 当前选中槽位有高亮边框，切换时高亮跟随

### InventoryUI
- [ ] Tab 键切换面板显示/隐藏
- [ ] 面板显示时呈现 4×10 网格，共 40 个槽位
- [ ] 所有槽位实时反映 Inventory 数据变化
- [ ] 光标空 + 左键点击有物品的槽位 → 物品移到光标，槽位变空
- [ ] 光标持有物品 + 左键点击空槽位 → 物品放入槽位，光标变空
- [ ] 光标持有物品 A + 左键点击物品 B 的槽位 → A 和 B 交换
- [ ] 光标持有物品 + 左键点击同类物品槽位 → 合并（不超过 MaxStackSize），溢出留在光标
- [ ] 光标物品图标跟随鼠标移动
- [ ] 关闭面板时光标物品通过 AddItem 放回背包
- [ ] 点击 HotbarUI 和 InventoryUI 区域的槽位均可触发交互

### Inventory.SetSlot
- [ ] 正确设置槽位内容，触发 OnSlotChanged
- [ ] 越界索引静默忽略

### UI 穿透阻止
- [ ] 鼠标在 UI 元素上时，不触发挖掘和放置
- [ ] 鼠标在 UI 元素上时，不显示方块高亮
- [ ] 快捷栏数字键和滚轮切换不受 UI 穿透检查影响

### 通用
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项
- **当前物品无图标**：Task 007 中 ItemData 的 Icon 字段为 null，SlotUI 必须正确处理 null Icon（隐藏 Image 或设为透明）。即使没有图标，数量文字仍能让玩家识别槽位是否有物品
- **EventSystem 使用 InputSystemUIInputModule**：项目使用 New Input System，不要用旧版 StandaloneInputModule
- **光标物品的 raycastTarget**：CursorIcon 和 CursorCountText 必须设置 `raycastTarget = false`，否则会挡住对槽位的点击
- **SlotUI 的 Icon 和 CountText 也设 raycastTarget = false**：确保点击事件由 Background Image 接收，不被子元素拦截
- **Canvas 排序**：如有多个 Canvas，确保 InventoryUI 的光标物品渲染在最上层
- **不做右键操作**：v1.0 不实现右键半栈拾取，仅左键交互
- **不做拖拽**：使用点击拾取/放置模式，不实现拖拽
- **不暂停游戏**：背包打开时游戏继续运行，玩家仍可移动
- **PlayerBlockInteraction 中 using 需补充**：新增 `using UnityEngine.EventSystems;`
