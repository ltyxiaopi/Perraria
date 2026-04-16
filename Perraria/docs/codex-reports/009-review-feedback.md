# 任务 009 审查反馈

## 审查结论: 通过，需 1 处修改

代码逻辑、接口签名、验收标准全部满足。编译无错误，无新增控制台问题。
以下修改完成后即可合并。

---

## 必须修改

### 1. InventoryUI: `FindFirstObjectByType` 改为 SerializeField

**文件**: `Assets/Scripts/UI/InventoryUI.cs`

**问题**: 第 18 行用私有字段 + `FindFirstObjectByType<HotbarUI>()` 查找引用（第 68 行）。
这是隐式依赖，场景中看不到引用关系，且 `FindFirstObjectByType` 是 O(n) 搜索，
与其他字段（`_inventory`、`_panel`、`_slots` 等）全部用 SerializeField 的风格不一致。

**修改**:

1. 将第 18 行:
```csharp
private HotbarUI _hotbarUI;
```
改为:
```csharp
[SerializeField] private HotbarUI _hotbarUI;
```

2. 删除 `CacheReferences()` 方法（第 64-69 行）及其在 `Awake()`（第 24 行）和 `OnEnable()`（第 33 行）中的两处调用。

3. 在 Unity 场景中将 `InventoryUI._hotbarUI` 字段绑定到 HotbarPanel 上的 `HotbarUI` 组件。

---

## 场景验证提醒

修改完成后请确认:
- `InventoryUI` Inspector 中 `_hotbarUI` 字段已正确引用 HotbarUI
- Play 模式下点击 Hotbar 槽位仍能触发拾取/放入交互
