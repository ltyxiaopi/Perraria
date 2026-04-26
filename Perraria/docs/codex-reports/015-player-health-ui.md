# 015-player-health-ui

## 任务信息
- 任务: 015 - 玩家血量 UI（左上角心形）
- 规格书: `docs/tasks/015-player-health-ui.md`
- 分支: `feature/015-player-health-ui`

## 本次完成内容
- 新增 `PlayerHealthUI`，按 `PlayerHealth.OnHealthChanged` 事件驱动刷新 5 颗心形，不做每帧轮询。
- 在 `Awake` 和 `OnEnable` 主动首刷 UI，并在 `OnEnable` / `OnDisable` 做事件订阅解绑。
- 配置 `heartDisplay.png` 为 `Sprite Multiple + Point + Uncompressed`，按 `heartDisplay_0 / _1 / _2 = 满 / 空 / 半` 切图，并裁掉透明 padding。
- 在 `SampleScene` 的 `Canvas` 下新增 `HUD/HealthBar`，放置 5 个 32x32 心形 `Image`，绑定 `PlayerHealthUI`、`PlayerHealth` 和 3 张子 sprite。
- 将 `HUD` 放到 `Canvas` 最后一个 sibling，保证渲染顺序高于 `HotbarPanel` / `InventoryRoot`。

## 变更文件清单
- `Assets/Scripts/UI/PlayerHealthUI.cs`
- `Assets/Scripts/UI/PlayerHealthUI.cs.meta`
- `Assets/Art/UI.meta`
- `Assets/Art/UI/heartDisplay.png`
- `Assets/Art/UI/heartDisplay.png.meta`
- `Assets/Scenes/SampleScene.unity`

## 运行态自测日志
- 启动即显示 5 颗满心: `CurrentHealth=100`, `hearts=heartDisplay_0 x5`
- 扣 10 HP 后最右心为半心: `hp=90`, `hearts=heartDisplay_0,heartDisplay_0,heartDisplay_0,heartDisplay_0,heartDisplay_2`
- 再扣 10 HP 后最右心为空心: `hp=80`, `hearts=heartDisplay_0,heartDisplay_0,heartDisplay_0,heartDisplay_0,heartDisplay_1`
- 致死时 5 颗心全空且死亡触发: `hp=0`, `dead=True`, `hearts=heartDisplay_1 x5`
- 重生后立即恢复 5 颗满心: `hp=100`, `dead=False`, `hearts=heartDisplay_0 x5`
- 自然回血按顺序恢复:
  `regen start -> hp=80, hearts=...,_1`
  `regen observed half -> hp=99, hearts=...,_2`
  `regen full -> hp=100, hearts=...,_0`
- HUD 不被快捷栏 / 背包遮挡: `HUD sibling=2`, `Hotbar sibling=0`, `Inventory sibling=1`

## Claude 审查重点
- `PlayerHealthUI` 是否严格使用 `OnHealthChanged` 事件驱动，且 `OnEnable` 订阅 / `OnDisable` 解绑。
- `RefreshUI` 半心阈值是否符合规格书公式: `>= 20 -> full`, `>= 10 -> half`, 否则 empty。
- `SampleScene` 绑定是否完整:
  `_playerHealth -> PlayerHealth`
  `_heartImages -> Heart_1..Heart_5`
  `_heartFull/_heartEmpty/_heartHalf -> heartDisplay_0/_1/_2`
- `heartDisplay.png.meta` 的切图矩形是否已裁掉透明 padding，避免 UI 显示偏小。
- `HUD` 是否仍保持在 `Canvas` 最后 sibling，避免被现有 UI 遮挡。

## 已知问题
- 功能侧未发现已知问题。
- 运行态验收在 Unity MCP 下采用了 `pause + Step()` 的方式推进时间，以避免后台窗口失焦时 `Time.time` 不稳定；不影响游戏内实际逻辑。
