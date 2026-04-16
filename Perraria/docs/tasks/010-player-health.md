# 任务 010 - 玩家生命值系统

## 目标
实现玩家生命值管理：受击扣血、无敌帧、自然回复、死亡重生、受击闪烁。
本任务只做数据层和表现层基础（Sprite 闪烁），不做生命值条 UI（留给后续任务）。

## 设计概要

### 生命值
- 最大生命值 `MaxHealth = 100`，可通过 Inspector 调整
- 当前生命值 `CurrentHealth`，Awake 时设为 MaxHealth
- 生命值范围 `[0, MaxHealth]`，不允许超上限或低于 0

### 受击
- `TakeDamage(int damage)` 扣减生命值
- damage ≤ 0 时忽略
- 无敌状态下忽略伤害
- 扣血后触发 `OnHealthChanged` 事件
- 生命值归 0 时触发死亡

### 无敌帧
- 受击后进入无敌状态，持续 `_invincibilityDuration`（默认 1.0 秒）
- 无敌期间忽略所有伤害
- 无敌期间 Sprite 闪烁（每 `_flashInterval` 秒切换透明度，默认 0.1 秒）
- 无敌结束后恢复正常显示

### 自然回复
- 最后一次受击后等待 `_regenDelay` 秒（默认 5.0 秒）后开始回复
- 每秒回复 `_regenPerSecond` 点生命值（默认 2）
- 回复时触发 `OnHealthChanged` 事件
- 再次受击时重置回复计时器

### 死亡与重生
- 生命值归 0 时触发 `OnDied` 事件
- 等待 `_respawnDelay` 秒（默认 1.0 秒）后重生
- 重生位置为 `_respawnPoint`（SerializeField Vector3，默认由场景配置）
- 重生时生命值恢复为 MaxHealth，触发 `OnHealthChanged` 和 `OnRespawned` 事件
- 死亡到重生期间禁用玩家输入（禁用 PlayerController 和 PlayerBlockInteraction）

## 接口签名

```csharp
// === Player/PlayerHealth.cs ===
// 挂载在 Player 游戏对象上
[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _invincibilityDuration = 1f;
    [SerializeField] private float _flashInterval = 0.1f;
    [SerializeField] private float _regenDelay = 5f;
    [SerializeField] private int _regenPerSecond = 2;
    [SerializeField] private float _respawnDelay = 1f;
    [SerializeField] private Vector3 _respawnPoint;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private int _currentHealth;
    private bool _isInvincible;
    private bool _isDead;
    private float _invincibilityTimer;
    private float _flashTimer;
    private float _regenTimer;        // 受击后的回复等待倒计时
    private float _regenAccumulator;  // 回复积累器（处理非整数帧间隔）

    /// <summary>当前生命值</summary>
    public int CurrentHealth => _currentHealth;

    /// <summary>最大生命值</summary>
    public int MaxHealth => _maxHealth;

    /// <summary>是否处于无敌状态</summary>
    public bool IsInvincible => _isInvincible;

    /// <summary>是否已死亡</summary>
    public bool IsDead => _isDead;

    /// <summary>生命值变化时触发，参数 (currentHealth, maxHealth)</summary>
    public event System.Action<int, int> OnHealthChanged;

    /// <summary>死亡时触发</summary>
    public event System.Action OnDied;

    /// <summary>重生时触发</summary>
    public event System.Action OnRespawned;

    /// <summary>
    /// 对玩家造成伤害。damage ≤ 0、无敌中、已死亡时忽略。
    /// </summary>
    public void TakeDamage(int damage);

    /// <summary>
    /// 回复生命值。amount ≤ 0 或已死亡时忽略。
    /// </summary>
    public void Heal(int amount);
}
```

### Update 逻辑伪代码

```csharp
private void Update()
{
    if (_isDead) return;

    HandleDebugInput();      // 调试按键（临时）
    UpdateInvincibility();   // 无敌倒计时 + 闪烁
    UpdateRegeneration();    // 自然回复
}
```

### TakeDamage 逻辑

```csharp
public void TakeDamage(int damage)
{
    if (damage <= 0 || _isInvincible || _isDead) return;

    _currentHealth = Mathf.Max(0, _currentHealth - damage);
    OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

    // 重置回复计时
    _regenTimer = _regenDelay;

    if (_currentHealth <= 0)
    {
        Die();
        return;
    }

    StartInvincibility();
}
```

### Die / Respawn 逻辑

```csharp
private void Die()
{
    _isDead = true;
    OnDied?.Invoke();
    SetPlayerControlsEnabled(false);
    // 延迟重生用协程
    StartCoroutine(RespawnAfterDelay());
}

private IEnumerator RespawnAfterDelay()
{
    yield return new WaitForSeconds(_respawnDelay);
    Respawn();
}

private void Respawn()
{
    transform.position = _respawnPoint;
    _currentHealth = _maxHealth;
    _isDead = false;
    _isInvincible = false;
    _regenTimer = 0f;
    RestoreSpriteAlpha();
    SetPlayerControlsEnabled(true);
    OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    OnRespawned?.Invoke();
}
```

### SetPlayerControlsEnabled

```csharp
private void SetPlayerControlsEnabled(bool enabled)
{
    // 获取同 GameObject 上的 PlayerController 和 PlayerBlockInteraction
    // Awake 中缓存引用
    if (_playerController != null) _playerController.enabled = enabled;
    if (_blockInteraction != null) _blockInteraction.enabled = enabled;
}
```

### 无敌闪烁

```csharp
private void UpdateInvincibility()
{
    if (!_isInvincible) return;

    _invincibilityTimer -= Time.deltaTime;
    if (_invincibilityTimer <= 0f)
    {
        EndInvincibility();
        return;
    }

    // 闪烁：周期性切换 Sprite 透明度
    _flashTimer -= Time.deltaTime;
    if (_flashTimer <= 0f)
    {
        _flashTimer += _flashInterval;
        ToggleSpriteAlpha();  // 在 alpha=1 和 alpha=0.3 之间切换
    }
}
```

### 调试按键（临时，后续移除）

```csharp
private void HandleDebugInput()
{
    if (Keyboard.current == null) return;

    // P 键：自伤 10 点
    if (Keyboard.current.pKey.wasPressedThisFrame)
    {
        TakeDamage(10);
    }

    // L 键：立即死亡（设为当前血量，测试死亡→重生全流程）
    if (Keyboard.current.lKey.wasPressedThisFrame)
    {
        TakeDamage(_currentHealth);
    }
}
```

### 自然回复

```csharp
private void UpdateRegeneration()
{
    if (_currentHealth >= _maxHealth) return;

    _regenTimer -= Time.deltaTime;
    if (_regenTimer > 0f) return;

    // 累积回复量，处理帧间隔不整除的情况
    _regenAccumulator += _regenPerSecond * Time.deltaTime;
    int healAmount = Mathf.FloorToInt(_regenAccumulator);
    if (healAmount > 0)
    {
        _regenAccumulator -= healAmount;
        Heal(healAmount);
    }
}
```

## 依赖
- `PlayerController` — 死亡时禁用/重生时启用 ✅ 已实现 (001)
- `PlayerBlockInteraction` — 死亡时禁用/重生时启用 ✅ 已实现 (004)
- `SpriteRenderer` — 闪烁效果 ✅ Player 上已存在

## 文件清单
- `Assets/Scripts/Player/PlayerHealth.cs` — 新增，生命值组件
- 无需修改已有文件

## 场景配置
完成代码后需在 Unity 中：
1. 在 Player 游戏对象上添加 `PlayerHealth` 组件
2. 将 Player 的 `SpriteRenderer` 拖入 `_spriteRenderer` 字段
3. 将 `_respawnPoint` 设置为世界生成时的玩家出生点位置（可先手动设置一个合理的坐标，后续任务再自动化）

## 验收标准

### 生命值基础
- [ ] Awake 后 `CurrentHealth == MaxHealth`
- [ ] `TakeDamage(10)` 后 `CurrentHealth` 正确减少 10
- [ ] `TakeDamage(0)` 和 `TakeDamage(-5)` 不产生效果
- [ ] `Heal(10)` 后 `CurrentHealth` 正确增加，不超过 MaxHealth
- [ ] 所有生命值变化触发 `OnHealthChanged(current, max)`

### 无敌帧
- [ ] 受击后 `IsInvincible == true`
- [ ] 无敌期间再次受击不扣血
- [ ] 无敌持续时间结束后 `IsInvincible == false`
- [ ] 无敌期间 Sprite 可见闪烁效果（alpha 在 1.0 和 0.3 之间交替）
- [ ] 无敌结束后 Sprite 恢复 alpha = 1.0

### 自然回复
- [ ] 受击后等待 `_regenDelay` 秒，生命值开始自动回复
- [ ] 回复速度约为每秒 `_regenPerSecond` 点
- [ ] 生命值满后停止回复
- [ ] 回复期间再次受击，重置等待计时器

### 死亡与重生
- [ ] 生命值归 0 时触发 `OnDied`，`IsDead == true`
- [ ] 死亡期间 PlayerController 和 PlayerBlockInteraction 被禁用
- [ ] 等待 `_respawnDelay` 秒后自动重生
- [ ] 重生后位置为 `_respawnPoint`
- [ ] 重生后 `CurrentHealth == MaxHealth`，`IsDead == false`
- [ ] 重生后 PlayerController 和 PlayerBlockInteraction 恢复启用
- [ ] 重生时触发 `OnRespawned`
- [ ] 死亡期间 `TakeDamage` 无效

### 调试按键
- [ ] 按 P 键扣 10 点血，可观察到闪烁 + 血量下降（Inspector 验证）
- [ ] 按 L 键立即死亡，触发死亡→重生全流程
- [ ] 连续按 P 直到死亡，验证死亡→重生→满血恢复
- [ ] 受击后等待 5 秒，观察血量自动回复

### 通用
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 不修改已有文件
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项
- **不做 UI**：生命值条 UI 留给后续任务，本任务仅提供事件供订阅
- **不做击退**：受击不产生物理击退效果，v1.0 简化处理
- **不做掉落物散落**：死亡时不掉落背包物品
- **协程用于重生延迟**：`WaitForSeconds` 简洁直接，无需 Timer 类
- **SpriteRenderer 缓存**：用 `[SerializeField]` 而非 `GetComponent`，与 PlayerController 的 `_spriteRenderer` 风格一致
- **PlayerController 和 PlayerBlockInteraction 缓存**：在 Awake 中通过 `GetComponent` 获取同对象上的组件并缓存到私有字段，不用 SerializeField（因为都在同一个 GameObject 上，不需要手动拖拽）
- **_regenAccumulator 的作用**：`_regenPerSecond` 是整数但 `Time.deltaTime` 是浮点数，需要累积器确保回复量精确。例如 2hp/s 在 60fps 下每帧 0.033hp，累积到 1 才执行一次 Heal(1)
- **调试按键是临时代码**：`HandleDebugInput()` 中的 P/L 键仅供本任务验收使用，敌人系统上线后将移除。使用 `Keyboard.current` 与项目已有输入风格一致
