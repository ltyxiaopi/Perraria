# 任务 010 交付记录 - 玩家生命值系统

## 交付内容

- 新增 `Assets/Scripts/Player/PlayerHealth.cs`
- 在 `SampleScene` 的 `Player` 对象上挂载了 `PlayerHealth`
- 已绑定 `PlayerHealth._spriteRenderer -> Player.SpriteRenderer`
- 已将场景序列化中的 `_respawnPoint` 设为 `Player` 当前场景坐标 `(0, 0, 0)`

## 实现说明

`PlayerHealth` 实现了规格书要求的公开接口和核心逻辑：

- 生命值初始化、扣血、治疗、上下限约束
- 无敌帧计时与 Sprite alpha 闪烁
- 受击后延迟自然回血，使用 `_regenAccumulator` 处理非整帧回复
- 死亡后禁用 `PlayerController` / `PlayerBlockInteraction`
- 延迟重生、满血恢复、恢复控制、触发 `OnRespawned`
- 临时调试键：
  - `P` -> `TakeDamage(10)`
  - `L` -> `TakeDamage(_currentHealth)`

额外处理：

- 现有 `WorldGenerator` 会在运行时把玩家传送到地表出生点，和场景里的 `(0, 0, 0)` 不一致。
- 为保证重生点跟随真实出生点，`PlayerHealth` 在 `Awake()` 中启动一个短延时协程，若场景中的 `_respawnPoint` 仍为零点，则在运行时稳定后记录实际出生位置。

关键代码位置：

- `PlayerHealth` 字段与事件定义：`Assets/Scripts/Player/PlayerHealth.cs:12-43`
- 组件缓存与运行时出生点捕获：`Assets/Scripts/Player/PlayerHealth.cs:45-65`
- 调试键映射：`Assets/Scripts/Player/PlayerHealth.cs:111-127`
- 受击 / 无敌 / 回血：`Assets/Scripts/Player/PlayerHealth.cs:79-189`
- 死亡 / 重生 / 控制开关：`Assets/Scripts/Player/PlayerHealth.cs:191-238`

## 验收记录

### 场景配置核对

- `PlayerHealth` 已挂到 `Player`
- `_spriteRenderer` 引用不为空
- 场景序列化中的 `_respawnPoint` 为 `(0, 0, 0)`，与 `Player` 的场景坐标一致

对应序列化片段：

- `Assets/Scenes/SampleScene.unity:6465` `Assembly-CSharp::PlayerHealth`
- `Assets/Scenes/SampleScene.unity:6472` `_respawnPoint: {x: 0, y: 0, z: 0}`
- `Assets/Scenes/SampleScene.unity:6473` `_spriteRenderer: {fileID: 540911843}`

### Play 模式验收

已在 Play 模式验证核心行为。由于当前工具环境下注入 `Keyboard.current.*.wasPressedThisFrame` 不稳定，无法可靠地自动触发 `P/L` 的帧事件；因此：

- 运行时行为使用等价调用验收：
  - `TakeDamage(10)` 对应 `P`
  - `TakeDamage(CurrentHealth)` 对应 `L`
- 同时已核对源码中的调试键映射与规格一致

运行时验证结果：

- 初始生命值 `100`
- 伤害后生命值 `100 -> 90`
- 进入无敌状态：`IsInvincible = true`
- 闪烁已观察到：`sawFlash = true`
- 无敌结束后恢复正常显示：`alphaAfterInvincible = 1.00`
- 5 秒后自然回血生效：`90 -> 92`
- 致死后：
  - `IsDead = true`
  - `PlayerController.enabled = false`
  - `PlayerBlockInteraction.enabled = false`
- 重生后：
  - `IsDead = false`
  - `CurrentHealth = 100`
  - `PlayerController.enabled = true`
  - `PlayerBlockInteraction.enabled = true`

运行时还确认了一点：

- `PlayerHealth` 在启动稳定后记录到的运行时出生点，与玩家实际运行时位置一致
- `Respawn()` 代码路径直接执行 `transform.position = _respawnPoint`

### Console 检查

- 未观察到由本任务引入的新 Unity Console Error
- 当前仅有一个既存 Warning，来自 `com.unity.ai.assistant` 的 Account API 网络访问，不属于本任务代码

## 备注

- 工作区中的 `Assets/Scenes/SampleScene.unity` 在本任务开始前已经存在大量未提交改动；本次仅在其中追加了 `PlayerHealth` 组件与对应序列化字段，没有回退用户已有变更。
