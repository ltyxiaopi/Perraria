# Perraria - 任务看板
## 待实现 (TODO)
> **下一个**：027。
> **阶段顺序**：026 → 026b → 027 → 028（建造系统）。

### 建造系统（远期）
- [027 - 家具放置系统](tasks/027-furniture-placement.md) ⚠️ 骨架版 — Furniture 基类 + 多格占用 + 拆除机制
- [028 - 房屋构建判定](tasks/028-house-validation.md) ⚠️ 骨架版 — Flood Fill 围合检测 + UI 反馈

## 进行中 (In Progress)
暂无

## 审查中 (In Review)
暂无

## 已完成 (Done)
- [026b - 工作台改版](tasks/026b-workbench-rework.md) — 通过（feature/026b-workbench-rework）；工作台改为 **2 格宽×1 格高**（`Workbench` + `WorkbenchRight`），背包合成只留工作台配方，木剑/木箭/木镐移入独立 `WorkbenchUI`；右键工作台打开 UI，离开范围自动关闭。审查打回的 `workbench.png` PPU=100 视觉缩小问题已修复为 PPU=16，并用 Play Mode 相机截图确认 TilemapRenderer bounds 为 **2×1**、接地对齐，[交付记录](../codex-reports/026b-workbench-rework.md)。
- [026 - 合成系统](tasks/026-crafting-system.md) — 通过（PR #25, master 8d46ce4）；工作台做成 `BlockType.Workbench` 方块、合成集成进背包面板、4 配方只耗木材；资产值/木剑朝向(`_iconAngleOffset=45`)/合成原子性均 MCP 实测确认，[交付记录](../codex-reports/026-crafting-system.md)。**后续改版见 026b**（2 格宽 + 独立工作台 UI）
- [029 - 砍树系统](tasks/029-tree-chopping.md) — 通过（PR #24；方块式树 Wood/Leaves + 砍木掉 Item_Wood + 树叶 10% 掉 Item_Sapling + 种子化刷树；复用挖掘/掉落/存档全套，仅给 BlockData 加 DropChance；独立 MCP 验证编译0错误+配置正确+确定性 equal=True；素材 OpenGameArt 16x16 Block Texture Set CC0, oak），[交付记录](../codex-reports/029-tree-chopping.md)
- [025 - 昼夜循环系统](tasks/025-day-night-cycle.md) — 通过（PR #22；时间循环 + Light2D 渐变 + 代码渐变天空 + 三层视差云 + 夜空星月 + 僵尸夜刷 + 存档；PR #23 追加调试快捷键：按住 T 快进、按 N 跳时段），[交付记录](../codex-reports/025-day-night-cycle.md)
- [024 - Boss HFSM 重构](tasks/024-boss-hfsm-refactor.md) — 通过（PR #20；EyeOfCorruption 从 enum+switch 重构为 UnityHFSM 层级状态机，行为 1:1 等价；审查确认 DripAcid 期间 `_phaseTimer` 持续累加、`!IsDead` 守卫防止死亡瞬间误发射酸液），[交付记录](../codex-reports/024-boss-hfsm-refactor.md)
- [023 - Boss 敌人 + 召唤系统](tasks/023-boss-and-summon.md) — 通过（PR #18；附修复：sprite 透明边距 trim + Visual 子物体补偿；规格漏洞补丁：PlayerBlockInteraction 让 Consumable 也阻挡挖矿），[交付记录](../codex-reports/023-boss-and-summon.md)
- [022 - 僵尸敌人](tasks/022-enemy-zombie.md) — 通过（附修复：Idle 漂移归零 + 删除死代码 `_stuckTimer`），[交付记录](../codex-reports/022-enemy-zombie.md)
- [021 - 远程武器系统](tasks/021-ranged-weapons.md) — 通过（弓/法杖/投掷斧 + 通用 Projectile 组件，新增 Player/Projectile Layer 及碰撞矩阵），[交付记录](../codex-reports/021-ranged-weapons.md)
- [020 - 近战武器扩展 + 木镐工具](tasks/020-melee-weapons-and-pickaxe.md) — 通过（附修复：木镐源图从 80×55 缩为 12×14 像素），[交付记录](../codex-reports/020-melee-weapons-and-pickaxe.md)
- [019 - 存档/读档接入 UI](tasks/019-save-load-integration.md) — 通过（含独立 MCP 验证 Flow 4 完整还原 + Flow 6 损坏存档容错），[交付记录](../codex-reports/019-save-load-integration.md)
- [018 - 存档系统数据结构与服务](tasks/018-save-system.md) — 通过（含独立 MCP 14 项断言往返验证），[交付记录](../codex-reports/018-save-system.md)
- [017 - 暂停菜单](tasks/017-pause-menu.md) — 通过，[交付记录](../codex-reports/017-pause-menu.md)（follow-up：`PlayerHealth.HandleDebugInput()` 的 P/L 调试键暂未 pause-gate，后续清理 debug 入口时统一处理）
- [016 - 主菜单场景](tasks/016-main-menu.md) — 通过（附修复：按钮文案改英文以避免 CJK tofu），[交付记录](../codex-reports/016-main-menu.md)
- [015 - 玩家血量 UI（左上角心形）](tasks/015-player-health-ui.md) — 通过，[交付记录](../codex-reports/015-player-health-ui.md)
- [014 - 敌人生成器（EnemySpawner）](tasks/014-enemy-spawner.md) — 通过，[交付记录](../codex-reports/014-enemy-spawner.md)
- [013 - 玩家战斗系统（PlayerCombat + 剑武器）](tasks/013-player-combat.md) — 通过，[交付记录](../codex-reports/013-player-combat.md)
- [012 - 敌人基础框架 + 紫色史莱姆](tasks/012-enemy-slime.md) — 通过（附修复），[交付记录](../codex-reports/012-slime-collider-followup.md)、[审查修订](../codex-reports/012-review-fixes.md)
- [011 - 掉落物实体](tasks/011-item-drop.md) — 通过，[交付记录](../codex-reports/011-item-drop.md)
- [010 - 玩家生命值系统](tasks/010-player-health.md) — 通过，[交付记录](../codex-reports/010-player-health.md)
- [009 - 快捷栏 + 背包 UI](tasks/009-hotbar-inventory-ui.md) — 通过，[交付记录](../codex-reports/009-hotbar-inventory-ui.md)
- [008 - 背包系统](tasks/008-inventory-system.md) — 通过，[交付记录](../codex-reports/008-inventory-system.md)
- [007 - 物品数据定义](tasks/007-item-data.md) — 通过，[交付记录](../codex-reports/007-item-data.md)
- [006 - 挖掘视觉效果简化](tasks/006-mining-visual-simplify.md) — 通过（附修复：相机 size 恢复为 7）
- [005 - 方块挖掘耗时系统](tasks/005-mining-duration.md) — 通过（附修复：MCP 补绑 _miningOverlay 引用 + 相机 size 恢复为 7），[交付记录](../codex-reports/005-mining-duration.md)
- [004 - 方块交互系统（挖掘+放置）](tasks/004-block-interaction.md) — 通过，[交付记录](../codex-reports/004-block-interaction.md)
- [003 - 地形生成系统 + 摄像机跟随](tasks/003-terrain-generation.md) — 通过，[交付记录](../codex-reports/003-terrain-generation.md)
- [002 - 玩家角色场景搭建](tasks/002-player-character-setup.md) — 通过（附修复），[审查反馈](../codex-reports/002-review-feedback.md)
- [001 - 玩家基础移动](tasks/001-player-movement.md) — PlayerController: 行走+跳跃
