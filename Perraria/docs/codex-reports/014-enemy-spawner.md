# 任务 014 交付记录 - EnemySpawner

## 任务信息

- 任务编号：014
- 任务名称：EnemySpawner
- 实现分支：`feature/014-enemy-spawner`

## 本次完成内容

### 1. 新增敌人生成功能脚本

- 新增 `Assets/Scripts/Enemies/EnemySpawnEntry.cs`
  - 定义单个敌人生成条目：`Prefab + Weight`
- 新增 `Assets/Scripts/Enemies/EnemySpawnerConfig.cs`
  - `ScriptableObject` 配置生成间隔、并发上限、最小/最大生成距离、射线高度、射线距离、地面层和生成表
- 新增 `Assets/Scripts/Enemies/EnemySpawner.cs`
  - 运行态维护 `AliveCount`
  - 基于权重表抽取敌人 Prefab
  - 按 `GroundLayer` 做向下射线找地表
  - 只允许在当前相机视口外生成
  - 生成点与玩家水平距离限制在 `[MinSpawnDistance, MaxSpawnDistance]`
  - 通过订阅 `Enemy.OnDied` 维护本地存活集合，不使用 `FindObjectsOfType` 每帧扫描
  - 暴露 `TriggerSpawnAttempt()` 供调试和运行态验收使用
- 生成调度最终采用 `Update + _spawnTimer`，而不是协程循环
  - 这样在组件禁用/重新启用时更稳定，便于满足“停刷/恢复”要求

### 2. MCP 创建与配置生成器资源

- 通过 Unity MCP 创建 `Assets/Data/Enemies/EnemySpawnerConfig.asset`
- 配置内容：
  - `SpawnInterval = 3`
  - `MaxConcurrent = 8`
  - `MinSpawnDistance = 14`
  - `MaxSpawnDistance = 22`
  - `SkyRaycastHeight = 20`
  - `RaycastDistance = 60`
  - `GroundLayer = Ground`
  - `SpawnTable[0] = Slime.prefab, Weight = 1`

### 3. MCP 更新场景配置

- 在 `SampleScene` 中新增空物体 `EnemySpawner`
- 挂载 `EnemySpawner` 组件，并绑定：
  - `_config = EnemySpawnerConfig.asset`
  - `_mainCamera = Main Camera`
  - `_playerTransform = Player`
- 移除了场景里原先手摆的 2 只史莱姆
- 移除了 `Player` 身上的 `EnemyDebugInput` 组件

### 4. 清理旧调试入口

- 删除 `Assets/Scripts/Debug/EnemyDebugInput.cs`
- 删除 `Assets/Scripts/Debug/EnemyDebugInput.cs.meta`
- 删除 `Assets/Scripts/Debug.meta`
- 空的 `Assets/Scripts/Debug/` 目录已移除

## 变更文件

- `Assets/Scripts/Enemies/EnemySpawnEntry.cs`
- `Assets/Scripts/Enemies/EnemySpawnerConfig.cs`
- `Assets/Scripts/Enemies/EnemySpawner.cs`
- `Assets/Data/Enemies/EnemySpawnerConfig.asset`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/Debug/EnemyDebugInput.cs`
- `docs/codex-reports/014-enemy-spawner.md`

## 运行态验证结果

### 1. 首只史莱姆按间隔出现

- 使用 Unity MCP 运行态自包含探针重置玩家、相机、存活史莱姆和 `EnemySpawner` 状态后验证
- 探针结果：
  - `aliveAtProbeStart=0`
  - `aliveBeforeInterval=0`
  - `noSpawnBeforeInterval=True`
  - `aliveAfterInterval=1`
  - `firstSpawnAfterInterval=True`

### 2. 生成点与并发验证

- 使用 Unity MCP 运行态自包含探针逐次调用 `TriggerSpawnAttempt()`，对每次“刚生成当帧”的新史莱姆位置做采样
- 探针结果：
  - `spawnedCount=8`
  - `liveAtCap=8`
  - `countCapped=True`
  - `immediateSpawnPositionsValid=True`
- 采样结果表明 8 次生成均满足：
  - 在相机视口外
  - 与玩家水平距离位于 `14~22`
  - 位于 `Ground` 命中点上方约 `0.5` 单位

### 3. 击杀递减与补刷验证

- 运行态探针结果：
  - `aliveAfterKill=7`
  - `aliveCountDecremented=True`
  - `aliveAfterRefill=8`
  - `refillAfterKill=True`

### 4. 禁用停刷与重新启用恢复

- 运行态探针结果：
  - `aliveWhileDisabled=7`
  - `aliveAfterDisabledWait=7`
  - `noSpawnWhenDisabled=True`
  - `aliveAfterReenable=8`
  - `resumedAfterReenable=True`

### 5. 编译与控制台

- `Assembly-CSharp` 编译通过
- Unity 控制台未发现本任务代码引入的错误或警告
- 当前仅剩 1 条已存在的编辑器联网 warning：
  - `Account API did not become accessible within 30 seconds`
  - 来源为 `com.unity.ai.assistant` 包，不是本任务代码

## Claude 审查重点

- `EnemySpawner` 是否严格通过 `AliveCount + OnDied` 事件维护存活计数
- 生成点是否只依赖：
  - 玩家当前位置
  - 离屏判定
  - `GroundLayer` 射线
  - `[14, 22]` 水平距离
- `EnemySpawnerConfig.asset` 是否仅绑定 `Slime.prefab`
- `SampleScene` 是否已经由生成器接管，不再保留手摆史莱姆和 `EnemyDebugInput`

## 已知说明

- 运行态验收使用了 Unity MCP 自包含探针，先把玩家和相机重置到一块稳定的地表区域，再开始计时和采样；这是为了避免前一轮测试残留状态干扰生成距离与视口判定
- 探针验证“生成点合法性”时取的是“新史莱姆生成当帧”的位置，而不是运动/落地一段时间后的当前位置，避免把后续移动误判成非法生成点
