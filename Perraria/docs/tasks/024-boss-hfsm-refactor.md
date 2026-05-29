# 任务 024 - Boss HFSM 重构（EyeOfCorruption）

## 目标
1. 引入 [UnityHFSM](https://github.com/Inspiaaa/UnityHFSM) 库（MIT，免费开源，纯 C# / 无运行时依赖）作为项目的 HFSM 框架。
2. **仅重构** `EyeOfCorruption` 的状态机，从 `enum + switch` 改为 UnityHFSM 的层级状态机。
3. **保持行为完全等价**：HP、伤害、冲撞速度、酸液间隔、动画帧数、掉落等所有数值和时序与现版本一致。本任务**不引入任何新机制**。
4. 为后续 Boss #2、#3 留出可复用的 `BossStateMachineBase<TPhase>` 模板（如有必要可抽出，但**只在确实有重复时**抽，不强求）。

## 美术资源前置检查
- **是否需要新素材**: 否（沿用 023 的 sprite sheet 与 prefab）

## 范围明确（防止扩散）

✅ **改**：
- `Assets/Scripts/Enemies/Boss/EyeOfCorruption.cs`
- `Packages/manifest.json`（新增 UnityHFSM 依赖）

❌ **不改**：
- `Assets/Scripts/Enemies/Enemy.cs`（基类，保持给 Slime/Zombie 用）
- `Assets/Scripts/Enemies/Slime.cs`、`Zombie.cs`（小怪保持现状）
- `Assets/Scripts/Enemies/EnemyState.cs`（基础 enum）
- 任何 prefab（包括 `EyeOfCorruption.prefab` 的序列化字段，需保留 inspector 上的所有当前值）
- 投射物、召唤物、UI、存档等周边系统

## UnityHFSM 集成方式

**推荐：Git URL（一行加 manifest，无需额外注册表配置）**

```jsonc
// Packages/manifest.json，加在 dependencies 块里
"com.inspiaaa.unityhfsm": "https://github.com/Inspiaaa/UnityHFSM.git#upm",
```

Codex 实施后，Unity 打开会自动下载并出现在 Package Manager 的 "In Project" 列表。
确认能 `using UnityHFSM;` 即集成完成。

> 备选：OpenUPM。但需要先在 Project Settings → Package Manager 加 scoped registry `https://package.openupm.com`，
> 麻烦多一步。除非用户明确要求 OpenUPM，否则用 Git URL。

## HFSM 结构设计

```
Root (StateMachine)
├── Alive (HybridStateMachine)
│   ├── Phase1 (HybridStateMachine)
│   │   ├── Hover  (State)
│   │   └── Charge (State)
│   └── Phase2 (HybridStateMachine)
│       ├── Hover    (State，每帧累计 projectileTimer)
│       ├── DripAcid (State，播 drip 动画；OnExit 发射酸液弹)
│       └── Charge   (State)
└── Defeated (State)
```

### 转移条件（所有数值与现版本一致）

| 起点 | 终点 | 条件 |
|---|---|---|
| `Alive`（任意子态） | `Defeated` | `_currentHealth <= 0` |
| `Alive.Phase1`（任意子态） | `Alive.Phase2` | `_currentHealth <= MaxHealth * 0.5f` |
| `Phase1.Hover` | `Phase1.Charge` | `phaseTimer >= _phase1HoverDuration` |
| `Phase1.Charge` | `Phase1.Hover` | `chargeTimer >= _chargeDuration` |
| `Phase2.Hover` | `Phase2.DripAcid` | `!firedThisHover && projectileTimer >= prefireStartTime` |
| `Phase2.DripAcid` | `Phase2.Hover` | `dripTimer >= dripDuration` |
| `Phase2.Hover` | `Phase2.Charge` | `phaseTimer >= _phase2HoverDuration` |
| `Phase2.Charge` | `Phase2.Hover` | `chargeTimer >= _chargeDuration` |

> **关键化简**：`Alive → Defeated` 是父层全局转移，不需要在每个子态写一遍——这就是 HFSM 相对 FSM 的核心收益。
> 同样 `Phase1 → Phase2` 写在 Alive 上，对 Phase1 任意子态都生效。

### 进入 / 退出钩子

| 状态 | OnEnter | OnExit |
|---|---|---|
| `Phase1.Hover` | `_phaseTimer = 0f`（首次进入 Phase1 时重置，由 Charge→Hover 触发也重置） | — |
| `Phase1.Charge` | `BeginChargeSnapshot()`、`_chargeTimer = 0f` | — |
| `Phase2.Hover` | **见下面"phaseTimer 重置时机"** | — |
| `Phase2.DripAcid` | `_dripTimer = 0f` | `FireAcidProjectile()`、`_firedThisHover = true` |
| `Phase2.Charge` | `BeginChargeSnapshot()`、`_chargeTimer = 0f` | `_firedThisHover = false`、`_projectileTimer = 0f`、`_phaseTimer = 0f` |
| `Defeated` | `Halt()`、`DisableColliders()`、启动 `DefeatRoutine` 协程 | — |

> 注意：`DefeatRoutine` 协程仍由 MonoBehaviour 启动，不要塞进 HFSM 状态对象里跑。

#### ⚠️ phaseTimer 重置时机（关键，易出 bug）

**原行为**：`_phaseTimer` 在整个 Phase2Hover 元状态（含 DripAcid 子流程）**连续累加**，不在 drip 中途清零。

**HFSM 映射后**：
- ✅ Phase2.Charge OnExit 重置 `_phaseTimer = 0f`（"准备下一轮 hover"）
- ✅ 进入 Phase2 那一刻重置（见下方"Phase1→Phase2 转移副作用"）
- ❌ Phase2.Hover.OnEnter **不**重置 phaseTimer（否则 DripAcid→Hover 会把它清零，行为偏离原版）

#### ⚠️ phaseTimer 在 DripAcid 期间也要计时

原版 `Phase2Hover` 的 switch case 里 `_phaseTimer += Time.deltaTime` 每帧执行，不受 `isPlayingDripAcid` 影响。

HFSM 拆出 DripAcid 后必须保留这语义：
- Phase2.Hover.OnLogic：`_phaseTimer += Time.deltaTime`
- Phase2.DripAcid.OnLogic：**也**要 `_phaseTimer += Time.deltaTime`
- Phase2.Charge.OnLogic：不累加

否则 Boss 在 DripAcid 期间会"停时"，hover 周期总时长会被拉长（行为不等价）。

#### Phase1→Phase2 转移的副作用

UnityHFSM 的 Transition 可以挂 `onTransition` 回调。在 `Alive.AddTransition("Phase1", "Phase2", ...)` 时一并指定：

```csharp
alive.AddTransition(new Transition(
    "Phase1", "Phase2",
    condition: t => _currentHealth <= Mathf.FloorToInt(_maxHealth * 0.5f),
    onTransition: t =>
    {
        _firedThisHover = false;
        _projectileTimer = 0f;
        _phaseTimer = 0f;
        Halt();                          // 清 Phase1.Charge 残留速度
        Debug.Log($"EyeOfCorruption phase switch: HP {_currentHealth}/{_maxHealth}", this);
    }));
```

（若 Codex 验证 `onTransition` 参数不存在，可改为在 Phase2.Hover.OnEnter 里用 `_phaseTimer == 0 && _projectileTimer == 0` 判定首次进入——但不优雅，优先用 onTransition。）

### 每帧逻辑（OnLogic）分布

| 状态 | OnLogic |
|---|---|
| `Phase1.Hover` | `MoveTowardHoverPoint()`；`_phaseTimer += dt`；播 idle 动画 |
| `Phase1.Charge` | `_rigidbody2D.linearVelocity = _chargeDirection * _phase1ChargeSpeed`；`_chargeTimer += dt`；播 detect 动画 |
| `Phase2.Hover` | `MoveTowardHoverPoint()`；**`_phaseTimer += dt`**；`_projectileTimer += dt`；播 detect 动画 |
| `Phase2.DripAcid` | **`_phaseTimer += dt`**（也要累加！见前节）；`_dripTimer += dt`；播 drip 动画 |
| `Phase2.Charge` | `_rigidbody2D.linearVelocity = _chargeDirection * _phase2ChargeSpeed`；`_chargeTimer += dt`；播 detect 动画 |
| `Defeated` | 无（Halt 已在 OnEnter 做，协程自跑） |

## 实现要点

### 1. 状态对象与 EyeOfCorruption 的依赖
UnityHFSM 的 State 推荐用 lambda 或派生 State 类两种写法。
**推荐用 lambda**（简洁，所有变量从 EyeOfCorruption 实例闭包捕获），例：

```csharp
private void BuildStateMachine()
{
    _fsm = new StateMachine();

    var alive = new StateMachine();
    var phase1 = new StateMachine();
    var phase2 = new StateMachine();

    // ── Phase1 ────────────────────────────────────────────────
    phase1.AddState("Hover", new State(
        onEnter: s => _phaseTimer = 0f,
        onLogic: s =>
        {
            MoveTowardHoverPoint();
            _phaseTimer += Time.deltaTime;
            PlayLoop(_idleFrames, _idleFps, _phaseTimer);
        }));
    phase1.AddState("Charge", new State(
        onEnter: s => { BeginChargeSnapshot(); _chargeTimer = 0f; },
        onLogic: s =>
        {
            _rigidbody2D.linearVelocity = _chargeDirection * _phase1ChargeSpeed;
            _chargeTimer += Time.deltaTime;
            PlayLoop(_detectFrames, _detectFps, _chargeTimer);
        }));
    phase1.AddTransition("Hover",  "Charge", t => _phaseTimer  >= _phase1HoverDuration);
    phase1.AddTransition("Charge", "Hover",  t => _chargeTimer >= _chargeDuration);
    phase1.SetStartState("Hover");

    // ── Phase2 ────────────────────────────────────────────────
    float dripDuration  = _dripAcidFrames.Length / Mathf.Max(0.01f, _dripAcidFps);
    float prefireStart  = Mathf.Max(0f, _phase2ProjectileInterval - dripDuration);

    phase2.AddState("Hover", new State(
        onEnter: null,   // 不重置 _phaseTimer（见 "phaseTimer 重置时机"）
        onLogic: s =>
        {
            MoveTowardHoverPoint();
            _phaseTimer      += Time.deltaTime;
            _projectileTimer += Time.deltaTime;
            PlayLoop(_detectFrames, _detectFps, _phaseTimer);
        }));
    phase2.AddState("DripAcid", new State(
        onEnter: s => _dripTimer = 0f,
        onLogic: s =>
        {
            _phaseTimer += Time.deltaTime;          // ⚠️ 必须，见前节
            _dripTimer  += Time.deltaTime;
            PlayLoop(_dripAcidFrames, _dripAcidFps, _dripTimer);
        },
        onExit: s => { FireAcidProjectile(); _firedThisHover = true; }));
    phase2.AddState("Charge", new State(
        onEnter: s => { BeginChargeSnapshot(); _chargeTimer = 0f; },
        onLogic: s =>
        {
            _rigidbody2D.linearVelocity = _chargeDirection * _phase2ChargeSpeed;
            _chargeTimer += Time.deltaTime;
            PlayLoop(_detectFrames, _detectFps, _chargeTimer);
        },
        onExit: s => { _firedThisHover = false; _projectileTimer = 0f; _phaseTimer = 0f; }));

    phase2.AddTransition("Hover",    "DripAcid", t => !_firedThisHover && _projectileTimer >= prefireStart);
    phase2.AddTransition("DripAcid", "Hover",    t => _dripTimer  >= dripDuration);
    phase2.AddTransition("Hover",    "Charge",   t => _phaseTimer >= _phase2HoverDuration);
    phase2.AddTransition("Charge",   "Hover",    t => _chargeTimer >= _chargeDuration);
    phase2.SetStartState("Hover");

    // ── Alive ─────────────────────────────────────────────────
    alive.AddState("Phase1", phase1);
    alive.AddState("Phase2", phase2);
    alive.AddTransition(new Transition(
        "Phase1", "Phase2",
        condition: t => _currentHealth <= Mathf.FloorToInt(_maxHealth * 0.5f),
        onTransition: t =>
        {
            _firedThisHover  = false;
            _projectileTimer = 0f;
            _phaseTimer      = 0f;
            Halt();
            Debug.Log($"EyeOfCorruption phase switch: HP {_currentHealth}/{_maxHealth}", this);
        }));
    alive.SetStartState("Phase1");

    // ── Root ──────────────────────────────────────────────────
    _fsm.AddState("Alive", alive);
    _fsm.AddState("Defeated", new State(
        onEnter: s =>
        {
            Halt();
            DisableColliders();
            if (_defeatCoroutine == null) _defeatCoroutine = StartCoroutine(DefeatRoutine());
        }));
    // 不加 AddTransitionFromAny(HP<=0)，由 Die() 重写显式 RequestStateChange。

    _fsm.SetStartState("Alive");
    _fsm.Init();
}
```

> Codex 在实施时若发现 `Transition` 构造函数不接受 `onTransition` 命名参数，
> 退化方案：在 `condition` 里写副作用（破坏 condition 应当是纯函数的契约，不推荐但可行），
> 或在 Phase2.Hover 第一次 OnEnter 用 `_phaseTimer==0 && _projectileTimer==0` 哨兵判定。优先用 onTransition。

### 2. Update 入口
现在 `Enemy.Update` 调用 `UpdateBehavior()`。在 EyeOfCorruption 里：

```csharp
protected override void UpdateBehavior()
{
    if (_playerTransform == null || _state != EnemyState.Chasing)
    {
        Halt();
        PlayHoverAnimation();
        return;
    }
    _fsm.OnLogic();
}
```

> 仍保留"没看到玩家就 Halt"的短路，避免 HFSM 在 Idle 状态空跑。

### 3. Die() 与 Defeated 状态的衔接

**保留** `Die()` 重写，但简化为通过 HFSM 切换：

```csharp
protected override void Die()
{
    if (_defeatCoroutine != null || !TryEnterDeadState()) return;
    _fsm.RequestStateChange("Defeated", forceInstantly: true);
}
```

Defeated 状态的 OnEnter 负责 `Halt() / DisableColliders() / StartCoroutine(DefeatRoutine())`——逻辑集中在状态对象里，Die() 只做"宣告死亡"。

**不要**用 `AddTransitionFromAny(HP<=0)` 全局转移——会和 Die() 重复触发，调试时容易困惑。

### 4. 动画
原 `PlayLoop` 用单一 `_animationTimer`。HFSM 重构后每个状态各自的时间字段（`_phaseTimer / _chargeTimer / _dripTimer`）**复用为动画时间**——参见上面 Phase1/Phase2 的 OnLogic 代码示例，直接把这些 timer 传给 `PlayLoop(frames, fps, time)` 的第三参。

**删除** `_animationTimer` 和 `_dripAnimationTimer` 字段（被各状态局部 timer 替代）。

### 5. 字段保留 / 删除清单

**保留所有 SerializeField**（prefab 上的引用不能丢）：
`_hoverHeight, _hoverFollowSpeed, _hoverSwayAmplitude, _hoverSwayFrequency, _phase1HoverDuration, _phase2HoverDuration, _chargeDuration, _phase1ChargeSpeed, _phase2ChargeSpeed, _acidProjectilePrefab, _phase2ProjectileInterval, _acidProjectileSpeed, _acidProjectileDamage, _acidProjectileKnockback, _acidProjectileLifetime, _projectileTargetLayer, _terrainLayer, _idleFrames, _detectFrames, _dripAcidFrames, _idleFps, _detectFps, _dripAcidFps`

**删除**（被 HFSM 替代）：
- `private enum BossPhase` 及其字段 `_phase`
- `private float _animationTimer, _dripAnimationTimer`（改成各状态局部时间字段）
- `private bool _isPlayingDripAcid`（改成 HFSM 的 DripAcid 状态本身）
- `private bool _firedProjectileThisHover` → 改名 `_firedThisHover` 并由父状态 Phase2 管理

**新增**：
- `private StateMachine _fsm;`
- `private float _phaseTimer, _chargeTimer, _projectileTimer, _dripTimer;`（按需）
- `private bool _firedThisHover;`

### 6. 公开 API 兼容
`public string DebugPhaseName => _phase.ToString();` 这个调试属性**仍需保留**（外部可能用），改成：
```csharp
public string DebugPhaseName => _fsm?.GetActiveHierarchyPath() ?? "<uninit>";
```
**用 `GetActiveHierarchyPath()` 而不是 `ActiveStateName`**——后者只返回叶子名（"Hover"），无法区分 Phase1.Hover / Phase2.Hover。前者返回完整路径如 `/Alive/Phase2/DripAcid`。

### 7. Awake 集成顺序
HFSM 必须在 `_currentHealth = _maxHealth` 之后构建（否则 condition 闭包捕获到的 maxHealth 不对）：

```csharp
protected override void Awake()
{
    base.Awake();        // 设置 _currentHealth = _maxHealth、_rigidbody2D 引用
    _rigidbody2D.gravityScale = 0f;
    _rigidbody2D.freezeRotation = true;
    if (_projectileTargetLayer.value == 0) _projectileTargetLayer = LayerMask.GetMask("Player");
    if (_terrainLayer.value == 0) _terrainLayer = LayerMask.GetMask("Ground");

    BuildStateMachine();   // ← 在所有字段 ready 后构建
}
```

## 依赖
- 任务 023（EyeOfCorruption 现版本）
- UnityHFSM 2.3+（任务内首次引入）

## 文件清单

### 新增 / 修改
- `Packages/manifest.json` — 加 1 行 UnityHFSM 依赖
- `Assets/Scripts/Enemies/Boss/EyeOfCorruption.cs` — 全面改写主体逻辑

### 不动
- `Assets/Prefabs/Enemies/EyeOfCorruption.prefab`（验证序列化字段不丢即可）
- 其它任何文件

## 验收标准

### 行为等价（最重要）
- [ ] Phase1 悬停 2 秒后冲撞（速度 12，持续 1 秒），冲撞后回悬停 — 与现版本目视一致
- [ ] HP 降到 200（50%）时切 Phase2，**仅切换 1 次**，Debug.Log 出现 phase switch 提示
- [ ] Phase2 悬停 1.5 秒，期间发射 1 颗酸液弹（前摇 drip 动画播完才发射），冲撞速度 14
- [ ] 酸液弹伤害 12，击退 5，速度 9，命中后销毁（不附着）
- [ ] HP=0 触发 Defeated：1 秒缩放→0，掉落 Corrupt Shard ×3，销毁
- [ ] 玩家走出 30 格 detection 范围 → Boss 静止悬停，不冲撞

### 集成正确
- [ ] `Packages/manifest.json` 加入 UnityHFSM 引用，Unity 启动无报错
- [ ] EyeOfCorruption.cs 顶部 `using UnityHFSM;`
- [ ] 编译 0 错误 0 警告
- [ ] EyeOfCorruption.prefab 上所有 inspector 字段引用仍然完好（Acid Projectile prefab、Idle/Detect/Drip Acid Frames 等）

### 综合
- [ ] 不引入新的 console 错误 / 警告（运行时持续 60 秒无异常）
- [ ] `DebugPhaseName` 返回的字符串能正确反映当前活跃叶子状态（如 `"Hover"` / `"Charge"` / `"DripAcid"` / `"Defeated"`）
- [ ] 符合 `coding-conventions.md`
- [ ] 行数控制：重构后 EyeOfCorruption.cs 不应明显超过现 366 行（HFSM 应让代码更紧凑，否则说明抽象用错了）

## 注意事项

### 性能
单 Boss 单帧 HFSM tick 在 100-300 纳秒级，相对原 switch 多 ~10×，但绝对值远小于物理 / 渲染开销。**性能不是关注点**，可读性和可扩展性才是。

### 不要做的事
- ❌ **不要**修改 `Enemy.cs` 基类（哪怕觉得"顺手抽个 BossBase"也别做——本任务专注 1 个 Boss）
- ❌ **不要**改 Slime / Zombie（HFSM 不适合单状态 AI）
- ❌ **不要**新增任何 Boss 技能 / 阶段 / 数值微调（行为必须等价）
- ❌ **不要**改 prefab 资产文件（除非 SerializeField 字段名变了才需要 inspector 重绑，但本任务尽量不改字段名）

### 自测顺序
1. 集成 UnityHFSM 后先确认编译通过（`AssetDatabase.Refresh` + 无 console error）
2. 实例化 Boss prefab 到场景，跑 Play 模式，观察从 Phase1 → Phase2 → Defeated 的完整流程
3. 用 `Unity_SceneView_Capture2DScene` 截 5 张关键帧：
   - Phase1 悬停
   - Phase1 冲撞
   - Phase2 悬停（含酸液）
   - Phase2 冲撞
   - Defeated（缩放中）
4. 对照 023 交付报告的截图，行为应**视觉一致**

### 关键陷阱
UnityHFSM 的 lambda 闭包会捕获 EyeOfCorruption 的 `this`——但因为 lambda 只在 `BuildStateMachine`（Awake 阶段）创建一次，**不会**产生每帧 GC 分配。Codex 实施时不要为了"避免闭包"把所有逻辑都搬到独立 State 派生类里，那会让代码更繁琐。

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/024-boss-hfsm-refactor.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。

**额外要求**：
1. **MCP 5 张截图**（Phase1 悬停 / Phase1 冲撞 / Phase2 悬停 / Phase2 冲撞 / Defeated）
2. **行为对照**：用调试加速时间或直接 Damage 把 HP 推到 200，记录 Debug.Log 是否在 HP=200 时打出 phase switch
3. **DebugPhaseName 值变化日志**：连续 30 秒内每秒打印一次 `_fsm.ActiveStateName`，确认状态流转顺序符合预期
4. **重构前后行数对比**：原 366 行，新版多少行？（不是硬指标，但要给数据供 Claude 评估抽象是否合理）
