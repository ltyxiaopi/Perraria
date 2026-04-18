# 任务 012 补充记录 - 审查修订

## 背景

这份记录用于向 Claude 说明任务 012 在初版提交后的审查修订内容。

上一版实现中额外引入了 `EnemyScenePlacement` 运行时自动放置逻辑，并在场景中放置了 12 只史莱姆。根据审查反馈，这超出了任务 012 的边界，敌人批量放置应留给任务 013。

## 本次修订

### 1. 删除自动放置逻辑

- 删除 `Assets/Scripts/Enemies/EnemyScenePlacement.cs`
- 删除 `Assets/Scripts/Enemies/EnemyScenePlacement.cs.meta`
- 从 `Assets/Scenes/SampleScene.unity` 的 `Enemies` 根节点移除 `EnemyScenePlacement` 组件

说明：

- 任务 012 只保留敌人基础框架和单个敌人类型实现
- 场景内不再依赖运行时自动重新摆放史莱姆

### 2. 场景中仅保留 2 只史莱姆

- `SampleScene` 中删除其余 10 只 `Slime`
- 仅保留 2 只手动摆放的 `Slime`
- 两只史莱姆位于玩家运行时出生点附近的地表位置，当前坐标为：
  - `Slime #1 -> (5.50, 52.30, 0.00)`
  - `Slime #2 -> (7.50, 53.30, 0.00)`

说明：

- 这两个位置是根据运行时玩家出生点附近的地表射线结果手动确定的
- 场景中不再保留 12 只量产实例

### 3. 回退 ItemDrop 到 master

- `Assets/Scripts/Items/ItemDrop.cs` 已回退到 `master` 状态

说明：

- 任务 012 不再携带对任务 011 已合并模块的行为修补
- 若后续确认自动拾取存在独立问题，应单开任务处理

## 当前与任务 012 相关的史莱姆参数

- `Transform.localScale = (0.65, 0.55, 1.00)`
- `CapsuleCollider2D.offset = (-0.40, -0.03)`
- `CapsuleCollider2D.size = (1.25, 1.03)`
- `GroundCheck.localPosition = (0.00, -0.66, 0.00)`

## 核对结果

- `SampleScene` 中 `EnemyScenePlacement` 已不存在
- `SampleScene` 中 `Slime` 实例数量为 2
- `ItemDrop.cs` 与 `master` 无差异
- Unity 编译通过

## 备注

- `docs/codex-reports/` 目录已恢复保留，用于继续向 Claude 传达本次任务和后续修订信息
- 与本次任务无关的本地改动未纳入处理：
  - `Assets/DefaultVolumeProfile.asset`
  - `docs/task-board.md`
  - `Assets/_Recovery.meta`
