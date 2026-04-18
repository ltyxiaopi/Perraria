# 任务 012 - 敌人基础框架 + 紫色史莱姆

## 目标
建立敌人模块的**可继承抽象基类** `Enemy`，承载所有敌人的共性属性与状态（生命值、玩家检测、
接触伤害、落地检测、死亡掉落、状态机骨架）。在此之上实现首个具体敌人 `Slime`（紫色史莱姆），
使用已切片的 `slime_idle1_purple_0` 作为静态贴图。

本任务为后续敌人生成器（013）、玩家攻击系统、更多敌人类型（飞行怪、远程怪、Boss 等）奠定基础——
新增敌人只需继承 `Enemy` 并实现特有的行为方法，无需重复编写共性逻辑。

## 设计概要

### 继承结构
```
Enemy (abstract MonoBehaviour)           ← 共性：生命值、状态、玩家检测、接触伤害、落地、掉落
  ├── Slime : Enemy                      ← 本任务实现：跳跃追击
  ├── (未来) FlyingEnemy : Enemy         ← 例：不需要落地检测，重写 UpdateGroundedState()
  ├── (未来) RangedEnemy : Enemy         ← 例：增加投射物、保持距离
  └── (未来) Boss : Enemy                ← 例：多阶段状态、技能
```

### 敌人状态（EnemyState）
所有敌人共用一个粗粒度状态枚举，细化行为（如"跳跃冷却中"、"攻击蓄力"）由子类自行维护私有字段。

```csharp
public enum EnemyState
{
    Idle,      // 玩家在检测范围外，静止
    Chasing,   // 玩家在检测范围内，执行子类具体追击/攻击行为
    Dead,      // 已死亡，不再响应任何输入
}
```

> 保持枚举简洁——复杂敌人（如 Boss）可在子类内部维护更细的子状态机，基类只管三态。

### Enemy 基类职责
**数据**
- `MaxHealth / CurrentHealth`（Inspector 可配）
- `ContactDamage`（接触玩家造成的伤害）
- `DetectionRange`（玩家进入多远开始追击）
- `ItemDropPrefab / DropItem / DropCount`（死亡掉落）
- `GroundCheck / GroundCheckRadius / GroundLayer`（落地检测，与 PlayerController 一致）
- `SpriteRenderer`（朝向翻转）
- `State`（当前状态）

**通用逻辑（sealed 或 virtual，子类可覆写）**
- `Awake`: 缓存 Rigidbody2D、SpriteRenderer；通过 `GameObject.FindWithTag("Player")` 缓存玩家引用
- `Update`:
  1. 死亡态直接返回
  2. 更新落地状态 `UpdateGroundedState()`
  3. 更新玩家距离 → 切换 Idle / Chasing 状态
  4. 调用 `UpdateBehavior()`（**abstract**，子类实现）
  5. 更新朝向 `UpdateFacing()`
- `TakeDamage(int damage)`: 公共受击入口，归零时调用 `Die()`
- `Die()` (virtual): 切换到 Dead 状态 → `SpawnDrop()` → `Destroy(gameObject)`。子类可覆写加入死亡动画/音效钩子
- `SpawnDrop()` (virtual): 实例化 ItemDrop 并 Initialize
- `OnCollisionStay2D`: 检测 `PlayerHealth` 并调用 `TakeDamage`（依赖玩家无敌帧节流）
- `UpdateGroundedState()` (virtual): `OverlapCircle` 检测；飞行敌人可覆写返回固定 true
- `UpdateFacing()` (virtual): 按 Rigidbody 水平速度翻转 flipX

**事件**
- `event Action<int, int> OnDamaged`
- `event Action OnDied`

### Slime 子类职责
继承 `Enemy`，只负责"**如何移动**"——跳跃追击：
- 玩家在检测范围内（`State == Chasing`）时，每 `_hopInterval` 秒跳一次
- 跳跃条件：已落地（`IsGrounded`）且冷却结束
- 水平速度 = `_hopHorizontalSpeed` × 朝向玩家的方向
- 垂直速度 = `_hopVerticalSpeed` 向上
- 空中不主动控制，由重力和惯性决定落点

Slime 实现的唯一 abstract 方法 `UpdateBehavior()` 只有十几行代码。

### 紫色史莱姆 Prefab
- 使用 `slime_idle1.png` 的切片 `slime_idle1_purple_0` 作为静态贴图
- Rigidbody2D: Dynamic，GravityScale = 1，冻结 Rotation-Z
- Collider: `CapsuleCollider2D` 贴合史莱姆身形（非 trigger）
- 子对象 `GroundCheck`，位于脚下
- Scale 建议 0.3 左右，MCP 截图确认使其视觉大小约为玩家 0.8 倍
- Layer: 新增 `Enemy` Layer（如不存在由 Codex 通过 MCP 添加）

### 死亡掉落物：史莱姆凝胶
- 新增 `Item_SlimeGel` (ItemData ScriptableObject)：
  - `_itemId`：查询 `ItemDatabase.asset` 最大 ID + 1
  - `_itemName` = "Slime Gel"
  - `_type = ItemType.Material`
  - `_maxStackSize = 99`
  - `_icon`：临时复用 `slime_idle1_purple_0`，后续做专用图标时替换
- 注册到 `ItemDatabase.asset`
- 紫色史莱姆死亡掉落 1 个

### 调试按键（临时）
- 按 `K` 键对玩家附近最近的 `Enemy` 造成 10 点伤害
- 建议独立成 `Assets/Scripts/Debug/EnemyDebugInput.cs`，使用
  `Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)` 遍历找最近目标
- 用于验证 `TakeDamage → Die → ItemDrop → Inventory` 完整链路
- PlayerCombat 上线后移除

## 接口签名

### EnemyState

```csharp
// === Enemies/EnemyState.cs ===
public enum EnemyState
{
    Idle,
    Chasing,
    Dead,
}
```

### Enemy（抽象基类）

```csharp
// === Enemies/Enemy.cs ===
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour
{
    // ── 生命值 ──
    [Header("Health")]
    [SerializeField] protected int _maxHealth = 20;

    // ── 检测与伤害 ──
    [Header("Combat")]
    [SerializeField] protected float _detectionRange = 10f;
    [SerializeField] protected int _contactDamage = 10;

    // ── 掉落 ──
    [Header("Drop")]
    [SerializeField] protected ItemDrop _itemDropPrefab;
    [SerializeField] protected ItemData _dropItem;
    [SerializeField] protected int _dropCount = 1;

    // ── 落地检测 ──
    [Header("Ground Check")]
    [SerializeField] protected Transform _groundCheck;
    [SerializeField] protected float _groundCheckRadius = 0.15f;
    [SerializeField] protected LayerMask _groundLayer;

    // ── 渲染 ──
    [Header("Visual")]
    [SerializeField] protected SpriteRenderer _spriteRenderer;

    protected Rigidbody2D _rigidbody2D;
    protected Transform _playerTransform;

    protected int _currentHealth;
    protected bool _isGrounded;
    protected EnemyState _state = EnemyState.Idle;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public EnemyState State => _state;
    public bool IsDead => _state == EnemyState.Dead;
    public bool IsGrounded => _isGrounded;

    public event System.Action<int, int> OnDamaged;
    public event System.Action OnDied;

    protected virtual void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _currentHealth = _maxHealth;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        UpdateGroundedState();
        UpdateDetection();
        UpdateBehavior();
        UpdateFacing();
    }

    /// <summary>子类实现具体的行为逻辑（移动、攻击等）。</summary>
    protected abstract void UpdateBehavior();

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnDamaged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        _state = EnemyState.Dead;
        OnDied?.Invoke();
        SpawnDrop();
        Destroy(gameObject);
    }

    protected virtual void SpawnDrop()
    {
        if (_itemDropPrefab == null || _dropItem == null || _dropCount <= 0) return;

        ItemDrop drop = Instantiate(_itemDropPrefab, transform.position, Quaternion.identity);
        drop.Initialize(_dropItem, _dropCount);
    }

    protected virtual void UpdateGroundedState()
    {
        _isGrounded = _groundCheck != null
            && Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
    }

    protected virtual void UpdateDetection()
    {
        if (_playerTransform == null)
        {
            _state = EnemyState.Idle;
            return;
        }

        float distance = Vector2.Distance(transform.position, _playerTransform.position);
        _state = distance <= _detectionRange ? EnemyState.Chasing : EnemyState.Idle;
    }

    protected virtual void UpdateFacing()
    {
        if (_spriteRenderer == null) return;
        if (Mathf.Abs(_rigidbody2D.linearVelocity.x) < 0.01f) return;
        _spriteRenderer.flipX = _rigidbody2D.linearVelocity.x < 0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;

        PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(_contactDamage);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }
}
```

### Slime（具体敌人）

```csharp
// === Enemies/Slime.cs ===
public sealed class Slime : Enemy
{
    [Header("Slime Hop")]
    [SerializeField] private float _hopInterval = 1.5f;
    [SerializeField] private float _hopHorizontalSpeed = 3f;
    [SerializeField] private float _hopVerticalSpeed = 6f;

    private float _hopTimer;

    protected override void UpdateBehavior()
    {
        if (_state != EnemyState.Chasing) return;

        _hopTimer -= Time.deltaTime;
        if (_hopTimer <= 0f && _isGrounded)
        {
            Hop();
            _hopTimer = _hopInterval;
        }
    }

    private void Hop()
    {
        float direction = Mathf.Sign(_playerTransform.position.x - transform.position.x);
        _rigidbody2D.linearVelocity = new Vector2(direction * _hopHorizontalSpeed, _hopVerticalSpeed);
    }
}
```

### 调试按键

```csharp
// === Debug/EnemyDebugInput.cs ===
// 临时调试入口，PlayerCombat 上线后移除
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EnemyDebugInput : MonoBehaviour
{
    [SerializeField] private int _damagePerPress = 10;
    [SerializeField] private float _maxRange = 5f;
    [SerializeField] private Transform _player;

    private void Awake()
    {
        if (_player == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _player = player.transform;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.kKey.wasPressedThisFrame) return;
        if (_player == null) return;

        Enemy nearest = FindNearestEnemy();
        if (nearest != null)
        {
            nearest.TakeDamage(_damagePerPress);
        }
    }

    private Enemy FindNearestEnemy()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearest = null;
        float bestSqr = _maxRange * _maxRange;
        foreach (Enemy e in enemies)
        {
            if (e.IsDead) continue;
            float sqr = ((Vector2)(e.transform.position - _player.position)).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                nearest = e;
            }
        }
        return nearest;
    }
}
```

## 依赖
- `ItemDrop` ✅ 已实现 (011) — 死亡掉落复用预制体
- `ItemData` / `ItemDatabase` ✅ 已实现 (007) — 新增 `Item_SlimeGel` 并注册
- `PlayerHealth.TakeDamage` ✅ 已实现 (010) — 接触伤害目标
- `PlayerController` ✅ 已实现 (001) — Player 带 `Player` Tag（若未设置，Codex 通过 MCP 补设）

## 文件清单
- `Assets/Scripts/Enemies/Enemy.cs` — 新增，敌人抽象基类
- `Assets/Scripts/Enemies/EnemyState.cs` — 新增，状态枚举
- `Assets/Scripts/Enemies/Slime.cs` — 新增，紫色史莱姆具体类
- `Assets/Scripts/Debug/EnemyDebugInput.cs` — 新增，临时调试入口
- `Assets/Prefabs/Enemies/Slime.prefab` — 新增，紫色史莱姆预制体
- `Assets/Data/Items/Item_SlimeGel.asset` — 新增，史莱姆凝胶物品
- `Assets/Data/ItemDatabase.asset` — 修改，追加 `Item_SlimeGel` 条目
- `docs/architecture.md` — 修改，敌人模块改为 `Enemy` 基类 + 子类的设计

## 场景配置（通过 MCP）
1. 确保 Player GameObject 的 Tag 为 `Player`（若未设置则补上）
2. 新增 `Enemy` Layer（若项目中未定义）
3. 在 `SampleScene` 挂载 `EnemyDebugInput` 到任意 GameObject（如 Player）
4. 在 `SampleScene` 实例化 1–2 只 `Slime.prefab` 放在玩家出生点附近（距离 5–8 单位）
5. `Slime.prefab` 字段绑定：
   - `_itemDropPrefab` → `Assets/Prefabs/ItemDrop.prefab`
   - `_dropItem` → `Item_SlimeGel.asset`
   - `_groundCheck` → 子对象 `GroundCheck`
   - `_groundLayer` → `Ground` Layer
   - `_spriteRenderer` → 预制体上的 SpriteRenderer

## 验收标准

### Enemy 基类
- [ ] `Enemy` 是 abstract 类，不能被直接实例化
- [ ] Awake 后 `CurrentHealth == MaxHealth`，`State == Idle`
- [ ] 玩家在 `_detectionRange` 内时 `State` 切换为 `Chasing`，外时切换回 `Idle`
- [ ] `TakeDamage(10)` 正确扣血并触发 `OnDamaged`
- [ ] `TakeDamage(0)` 和 `TakeDamage(-5)` 不产生效果
- [ ] 血量归零时 `State` 切换为 `Dead`，触发 `OnDied`，生成 ItemDrop，销毁 GameObject
- [ ] 死亡后不再响应 `TakeDamage`
- [ ] 接触玩家时通过 `PlayerHealth.TakeDamage` 扣血，受玩家无敌帧节流

### Slime 子类
- [ ] 继承 `Enemy` 并 `override UpdateBehavior()`
- [ ] 玩家在检测范围外时史莱姆静止
- [ ] 玩家进入范围后按 `_hopInterval` 周期向玩家方向跳跃
- [ ] 跳跃仅在落地后触发（空中不连跳）
- [ ] 朝向根据水平速度翻转（flipX）

### 死亡掉落
- [ ] 紫色史莱姆死亡时在当前位置生成 1 个 `Item_SlimeGel` 的 `ItemDrop`
- [ ] 掉落物正确显示 `Item_SlimeGel.Icon`
- [ ] 玩家靠近自动拾取，`Inventory` 内看到 "Slime Gel ×1"

### 预制体与场景
- [ ] `Slime.prefab` 存在于 `Assets/Prefabs/Enemies/`
- [ ] 场景中的史莱姆视觉大小接近玩家 0.8 倍（MCP 截图确认）
- [ ] 史莱姆受重力落地，不卡在地形中
- [ ] 史莱姆与玩家能够碰撞，不会相互穿透

### 调试按键
- [ ] 按 K 键能对最近（5 单位内）的 `Enemy` 造成 10 伤害
- [ ] 连续按两次 K 可击杀紫色史莱姆（20 血）并看到掉落物

### 架构文档
- [ ] `docs/architecture.md` 敌人模块更新为 `Enemy` 基类 + `Slime` 子类的描述
- [ ] 明确记录基类职责和"新增敌人只需继承并实现 `UpdateBehavior`"的扩展模式

### 通用
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项
- **基类不是万能仓库**：`Enemy` 只承载**所有**敌人都需要的东西。如果某个字段只有少数敌人用（如飞行敌人的悬停高度），放到具体子类里，不要下放到基类
- **abstract vs virtual**：`UpdateBehavior()` 用 abstract 强制子类实现；`Die`/`SpawnDrop`/`UpdateGroundedState`/`UpdateFacing` 用 virtual，默认实现能覆盖 90% 情况，特殊敌人再 override
- **状态枚举保持三态**：`Idle / Chasing / Dead` 够用。子类如需更细行为（如 Slime 的"冷却中"），用私有字段表达，不要扩枚举——枚举扩展性差，会污染所有敌人
- **不做 EnemySpawner**：史莱姆由 Codex 在场景中手动放置用于验证，自动刷怪留给 013
- **不做玩家攻击**：玩家无法对敌人造成伤害，仅通过调试按键 K 测试。PlayerCombat 为独立后续任务
- **不做动画**：仅用 `slime_idle1_purple_0` 静态贴图。其他切片 (`slime_idle1_purple_1` / `slime_hit` / `slime_die` / `slime_jump` 等) 保留给后续动画任务
- **不做击退/受击闪烁**：敌人受击仅扣血，无视觉反馈
- **不做 Boss / 精英怪**：只做普通紫色史莱姆一种
- **玩家引用**：v1.0 用 `GameObject.FindWithTag("Player")` 缓存。后续引入 GameManager 单例后再重构
- **Sprite 尺寸**：`slime_idle1_purple_0` 为 80×72，PPU=16，原始约 5×4.5 世界单位，明显过大。
  Slime 预制体 `transform.scale ≈ 0.3`，最终以 MCP 截图确认视觉比例
- **Layer 新增**：若 `Enemy` Layer 不存在，通过 `TagManager.asset` 的 MCP 操作添加；Slime 预制体的 Layer 设为 `Enemy`
- **ItemDatabase 注册**：`Item_SlimeGel._itemId` 取当前最大 ID + 1，避免冲突
- **复用 ItemDrop 预制体**：方块和敌人共用 011 的 `ItemDrop.prefab`，通过 `Initialize()` 注入不同 ItemData
- **OnCollisionStay2D vs OnCollisionEnter2D**：必须用 Stay，Enter 在玩家站在敌人身上时不会重复触发
- **protected 字段命名**：下划线前缀 `_fieldName`，与项目其他类保持一致（见 PlayerController）
