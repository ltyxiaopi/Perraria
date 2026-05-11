# 任务 025 - 合成系统（工作台 + 配方）【骨架版】

> ⚠️ **实施前必须补全**
> 本规范是**远期骨架版**，仅记录目标 / 依赖 / 验收骨架。**真正轮到 025 实施前，必须重新检查并补全**：
> - [ ] UI 交互细节（拖拽 / 一键合成 / 配方筛选）
> - [ ] 配方数据格式（手填 ScriptableObject vs CSV vs JSON）
> - [ ] 工作台是否分级（木工台 / 铁砧 / 神秘工作台）
> - [ ] 合成产出动画 / 音效（先不做但要决定）
> - [ ] 关键资产清单（配方 SO 数量、工作台 prefab 数量、初始合成菜单）
> - [ ] 与 022（僵尸掉落腐肉） / 023（Boss 掉落 NightShard）的具体配方衔接
>
> **执行 Codex 实现前，由 Claude Code 重新审视并产出完整版规范替换本文件。**

## 目标
1. 引入 `Recipe` ScriptableObject：N 个材料 → 1 个产出物。
2. 引入 `CraftingStation` MonoBehaviour：玩家靠近触发合成 UI。
3. 提供首个工作台 `Item_Workbench`（家具放置 026 任务真正实现可放置，025 阶段先做合成数据 + UI 桩）。
4. 玩家初始能合成的物品至少 5 个（待 025 实施前补全清单）。

## 设计概要（粗略）

### Recipe 结构
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

    public Ingredient[] Inputs;
    public ItemData Output;
    public int OutputCount;
    public ItemData[] RequiredStations;   // 空 = 任意位置可合成（手工合成）
}
```

### CraftingStation
- 玩家靠近 1.5 米内 → "C" 键打开合成 UI
- UI 显示玩家当前能合成的所有 Recipe（材料够 → 高亮）
- 选中 Recipe + 确认 → 扣材料 + 加产出物 + UI 列表更新

### 初始配方（待 025 实施前补全）
**示例草稿（不要当成最终）**：
- Item_Workbench：5 木材（？需先有"木材"物品来源 → 砍树系统未做 → 这里有依赖问题）
- Item_KnightSword：3 铁锭（铁锭系统未做）
- Item_SuspiciousEye：5 RottenFlesh + 1 NightShard（022/023 死亡掉落已存在）

⚠️ **依赖问题**：当前没有"木材"和"铁锭"物品。025 实施前必须先决定：
- A：先开"砍树系统"任务，做出 Item_Wood
- B：用现有的 Stone 当通用材料代替木材（玩法上不合理，但能跑）
- C：让 025 一并做"木材物品"（任务臃肿）

**025 真正开做时由 Claude Code 重新评估并选择**。

## 依赖（候选，待补全）
- 任务 008 Inventory（材料检查 / 扣除 / 添加）
- 任务 009 Hotbar UI（合成结果加进背包）
- 任务 020 ItemData（已有 ItemDatabase）
- 任务 022 + 023 部分材料来源
- **新增前置依赖（实施前确认）**：木材 / 铁锭 / 矿石等基础材料的来源任务

## 文件清单（候选）
- `Assets/Scripts/Crafting/Recipe.cs`
- `Assets/Scripts/Crafting/CraftingStation.cs`
- `Assets/Scripts/Crafting/CraftingService.cs`（核心合成逻辑，独立于 UI）
- `Assets/Scripts/UI/CraftingUI.cs`
- `Assets/Data/Recipes/`（多个 Recipe 资产）

## 验收标准（骨架）
- [ ] 配方 SO 可在 Inspector 编辑
- [ ] 玩家拥有所有材料时 Recipe 可执行
- [ ] 缺材料时 Recipe 灰显且不可执行
- [ ] 合成成功扣材料 + 加产出物（产出物数量正确）
- [ ] 工作台必填的 Recipe 在没有工作台时不可合成（手工合成路径只对 RequiredStations 为空的配方开放）

> **025 实施前要补的验收标准**：
> - UI 交互流畅性指标
> - 配方筛选 / 搜索（如果做）
> - 一次合成多个的处理（按住 Shift？）

## 注意事项
- **025 实施前必须做的事**：
  1. 决定基础材料（木材 / 铁锭）来源任务的优先级
  2. 草拟初始 10-20 个配方完整列表
  3. 确定 UI 框架（沿用 InventoryUI 风格 vs 独立窗口）
  4. 是否需要"配方解锁"机制（玩家不是一开始就知道所有配方）
- **不做的事（025 范围内）**：
  - 配方解锁 / 学习机制（先全部默认解锁）
  - 合成动画 / 进度条（瞬时合成）
  - 多个工作台并存的优先级（先按"附近任意工作台都行"处理）
  - 合成失败 / 损坏机制

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/025-crafting-system.md`
写一份交付记录。

---

> 📝 **重要提醒**（贴顶强调）：本文件是骨架版，025 实施前 Claude Code 必须重新走一轮：
> 1. 确认前置依赖（木材物品来源等）已经满足
> 2. 完整列出初始配方表
> 3. 与用户确认 UI 交互细节
> 4. 用完整版规范替换本文件再交给 Codex
