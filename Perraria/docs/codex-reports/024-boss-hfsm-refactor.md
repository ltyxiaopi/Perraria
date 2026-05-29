# 任务 024 - Boss HFSM 重构

## 任务信息
- 任务编号: 024
- 任务规格: `docs/tasks/024-boss-hfsm-refactor.md`
- 分支: `feature/024-boss-hfsm-refactor`
- 完成时间: 2026-05-29
- 执行方式: Codex + Unity MCP Play Mode 自测

## 本次完成内容
- `EyeOfCorruption` 从 `BossPhase enum + switch` 重构为 UnityHFSM 层级状态机:
  `Root -> Alive -> Phase1/Phase2 -> Hover/Charge/DripAcid`，以及 `Defeated`。
- `Die()` 简化为 `RequestStateChange("Defeated", forceInstantly: true)`，`Halt / DisableColliders / DefeatRoutine` 集中在 `Defeated` 状态 OnEnter。
- `DebugPhaseName` 改为 `_fsm.GetActiveHierarchyPath()`，运行时返回完整路径如 `/Alive/Phase2/DripAcid`。
- 保留全部原有 `SerializeField` 字段名；删除旧 `_phase`、`BossPhase`、`_animationTimer`、`_dripAnimationTimer`、`_isPlayingDripAcid`、`_firedProjectileThisHover`。
- `Packages/manifest.json` 加入 `com.inspiaaa.unityhfsm` Git URL 依赖。

## 变更文件
- `Assets/Scripts/Enemies/Boss/EyeOfCorruption.cs`
- `Packages/manifest.json`
- `docs/codex-reports/024-boss-hfsm-refactor.md`
- `docs/codex-reports/024-boss-hfsm-refactor-images/*.png`

## MCP 截图
![Phase1 Hover](024-boss-hfsm-refactor-images/01-phase1-hover.png)
![Phase1 Charge](024-boss-hfsm-refactor-images/02-phase1-charge.png)
![Phase2 Hover](024-boss-hfsm-refactor-images/03-phase2-hover.png)
![Phase2 DripAcid](024-boss-hfsm-refactor-images/04-phase2-dripacid.png)
![Phase2 Charge](024-boss-hfsm-refactor-images/05-phase2-charge.png)
![Defeated](024-boss-hfsm-refactor-images/06-defeated.png)

## 验证结果
- UnityHFSM 解析成功: Package Manager 实际版本 `2.3.0`，PackageCache 指纹 `f8fdc2efb862522acd26fab6cd940886c84cde37`。
- 编译检查: `AssetDatabase.Refresh()` 后 Unity Console `0 error / 0 warning`。
- Prefab 字段检查:
  `_acidProjectilePrefab=AcidProjectile`, `_idleFrames=8`, `_detectFrames=4`, `_dripAcidFrames=8`, `_spriteRenderer=Visual`, `_itemDropPrefab=ItemDrop`, `_dropItem=Item_CorruptShard`。
- Play Mode 完整序列:
  `/Alive/Phase1/Hover -> /Alive/Phase1/Charge -> /Alive/Phase2/Hover -> /Alive/Phase2/DripAcid -> /Alive/Phase2/Hover -> /Alive/Phase2/Charge -> /Defeated`。
- HP=200 phase switch:
  `EyeOfCorruption phase switch: HP 200/400` 在清空 Console 后计数为 `1`。
- 60 秒连续运行:
  使用 paused Play Mode + `EditorApplication.Step()` 模拟 60 秒，结束后 `Unity_GetConsoleLogs(Error,Warning)` 为 `0 error / 0 warning`。
- 行数对比:
  原版 `366` 行，新版 `371` 行。

## DebugPhaseName 30 秒采样
```text
t=01s /Alive/Phase1/Hover
t=02s /Alive/Phase1/Hover
t=2.04s transition /Alive/Phase1/Hover -> /Alive/Phase1/Charge
t=3.00s forced HP to 200
t=03s /Alive/Phase1/Charge
t=3.02s transition /Alive/Phase1/Charge -> /Alive/Phase2/Hover
t=3.24s transition /Alive/Phase2/Hover -> /Alive/Phase2/DripAcid
t=04s /Alive/Phase2/DripAcid
t=4.06s transition /Alive/Phase2/DripAcid -> /Alive/Phase2/Hover
t=4.54s transition /Alive/Phase2/Hover -> /Alive/Phase2/Charge
t=05s /Alive/Phase2/Charge
t=06s /Alive/Phase2/DripAcid
t=07s /Alive/Phase2/Hover
t=08s /Alive/Phase2/Charge
t=09s /Alive/Phase2/DripAcid
t=10s /Alive/Phase2/Charge
t=11s /Alive/Phase2/DripAcid
t=12s /Alive/Phase2/Hover
t=13s /Alive/Phase2/Charge
t=14s /Alive/Phase2/DripAcid
t=15s /Alive/Phase2/Charge
t=16s /Alive/Phase2/DripAcid
t=17s /Alive/Phase2/Hover
t=18s /Alive/Phase2/Charge
t=19s /Alive/Phase2/DripAcid
t=20s /Alive/Phase2/Charge
t=21s /Alive/Phase2/DripAcid
t=22s /Alive/Phase2/Hover
t=23s /Alive/Phase2/Charge
t=24s /Alive/Phase2/DripAcid
t=25s /Alive/Phase2/Charge
t=26s /Alive/Phase2/Hover
t=27s /Alive/Phase2/Hover
t=28s /Alive/Phase2/Charge
t=29s /Alive/Phase2/DripAcid
t=30s /Alive/Phase2/Charge
```

## Claude Code 重点检查项
- Phase2 `DripAcid` 期间 `_phaseTimer` 是否持续累加，且 `Phase2.Hover.OnEnter` 没有重置 `_phaseTimer`。
- `Defeated` 是否只由 `Die()` 显式请求，未加入 HP<=0 全局转移。
- `DebugPhaseName` 是否使用完整层级路径，而不是叶子状态名。
- `Packages/packages-lock.json` 未提交；任务要求只通过 `Packages/manifest.json` 加 Git URL，Unity 本地 resolve 用于验证实际版本。

## 已知说明
- 运行自测过程中 Unity AI Assistant 偶发 `Account API did not become accessible within 30 seconds` 警告，来源为 Unity 包自身网络状态；清空 Console 后最终编译与 60 秒运行验证均为 `0 error / 0 warning`。
- Play Mode harness 只在运行时创建临时 Player/Boss 对象，并未保存 scene 或 prefab。
