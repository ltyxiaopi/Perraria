# 任务 023 - Boss 敌人 + 召唤系统

## 目标
1. 实现首个 Boss `EyeOfNight`（暂定名，对标 Terraria 克苏鲁之眼）：高 HP、多阶段、远程冲撞 + 投射物攻击。
2. 引入「召唤物 (Summon Item)」机制：玩家使用消耗品类型的物品触发 Boss 生成。
3. 玩家**新建存档**时初始背包带 1 个 `Item_SuspiciousEye`（沿用 020 的初始物品机制扩展）。

## 设计概要

### 召唤物 = 新的 ItemType + 使用动作
新增 `ItemType.Consumable`（与 Block / Tool / Weapon / Material 并列）。

`ItemData` 新增字段：
```csharp
[SerializeField] private GameObject _summonPrefab;        // Consumable 触发后实例化的 GameObject
[SerializeField] private bool _consumeOnUse = true;       // 用完是否扣除堆叠 -1
[SerializeField] private float _useCooldown = 1f;         // 使用冷却（避免连点）
```

### 玩家"使用消耗品"输入
当前 `OnAttack`（左键）走武器逻辑。**Consumable 用同一个左键**：
1. 在 `PlayerCombat.OnAttack` 分发器里加 `WeaponSubType` 之外的分支：先看 `ItemType`
2. 如果选中是 `Consumable` → 调用 `UseConsumable(item)`
3. `UseConsumable`：
   - 在玩家头顶或鼠标位置实例化 `_summonPrefab`
   - 如果 `_consumeOnUse` → `_inventory.RemoveFromSlot(SelectedHotbarIndex, 1)`
   - 应用 `_useCooldown`

> Boss 召唤时机由 `_summonPrefab` 自带的 `BossSpawner` 组件决定（生成 Boss 实体并销毁自身，或者带个出现动画）。

### Boss 多阶段状态机
继承 `Enemy` 但**子状态机**写在 `EyeOfNight` 内部，不污染基类的 `EnemyState` 枚举：

```csharp
public sealed class EyeOfNight : Enemy
{
    private enum BossPhase { Phase1Hover, Phase1Charge, Phase2Hover, Phase2Charge, Defeated }

    private BossPhase _phase = BossPhase.Phase1Hover;
    private float _phaseTimer;
    // 行为参数：Phase1 = 悬停 + 冲撞；Phase2（HP < 50%）= 悬停 + 冲撞 + 投射物
}
```

**Phase 1（HP 100% → 50%）**：
- 在玩家上方 5 米悬停 2 秒，左右晃动
- 然后向玩家位置直线冲撞 1 次（速度 12，持续 1 秒），冲撞后悬停
- 循环

**Phase 2（HP 50% → 0%）**：
- 悬停 1.5 秒（更短）
- 冲撞 1 次（速度 14）
- 悬停时每秒发射 1 颗 MagicProjectileRed 投射物（伤害 12）
- 循环

**Defeated**：
- HP = 0 时触发：播 1 秒缩放 → 0 动画，掉落物品（详见下方），销毁

### Boss 不受重力 / 不需要落地
Rigidbody2D `gravityScale = 0`，重写 `UpdateGroundedState() { _isGrounded = true; }`（让基类逻辑别走重力），
其实更干净的做法是让 `Enemy` 基类把"是否需要 GroundCheck"暴露成 virtual property，但 023 不动基类，
直接覆盖一个不干事的 `UpdateGroundedState()` 即可（Slime/Zombie 不受影响）。

### Boss 数值
| 维度 | 值 |
|---|---|
| MaxHealth | 400 |
| ContactDamage | 25 |
| DetectionRange | 30 |
| 阶段切换 HP | 200 |
| 冲撞速度 | Phase1=12, Phase2=14 |
| 投射物伤害 | 12 |
| 击杀奖励 | Item_NightShard ×3（新增材料） + 50 金（金币系统未实现，先掉物品） |

### 召唤物视觉
`Item_SuspiciousEye` 使用占位 sprite（紫色 Slime sprite + 紫色 tint）。Boss 召唤时：
1. `_summonPrefab` 实例化 → 一个 BossSpawner GameObject
2. BossSpawner 在玩家上方 8 米处实例化 `EyeOfNight.prefab`，自身销毁
3. Boss 开始 Phase 1

> 当前只做这一个 Boss，多 Boss 由后续任务做。

### 投射物
Boss 的远程攻击使用 021 的 `Projectile` 组件 + `MagicProjectileRed.prefab`：
- `Owner = Owner.Enemy`，`TargetLayer = Player`
- `Damage = 12`，`Knockback = 5`
- 不重力，不粘地形

021 已经把 `Projectile` 做成可复用，本任务**不需要碰 Projectile.cs**，只需要在 `EyeOfNight.FirePhase2Projectile` 里 `Instantiate + projectile.Launch(...)`。

## 接口签名

```csharp
// === Items/ItemType.cs === 修改
public enum ItemType : byte
{
    Block = 0,
    Tool = 1,
    Weapon = 2,
    Material = 3,
    Consumable = 4,   // 新增
}

// === Items/ItemData.cs === 新增 3 个字段
[SerializeField] private GameObject _summonPrefab;
[SerializeField] private bool _consumeOnUse = true;
[SerializeField] private float _useCooldown = 1f;

// === Player/PlayerCombat.cs === 分发器扩展
public void OnAttack(InputAction.CallbackContext ctx)
{
    if (...) return;
    ItemStack selected = _inventory.GetSelectedItem();
    if (selected.IsEmpty) return;

    switch (selected.Item.Type)
    {
        case ItemType.Weapon: HandleWeaponAttack(selected.Item); break;
        case ItemType.Consumable: HandleConsumableUse(selected.Item); break;
        // Tool / Block / Material 不响应攻击
    }
}

// === Enemies/Boss/EyeOfNight.cs === 新增（继承 Enemy）
public sealed class EyeOfNight : Enemy
{
    [Header("Boss")]
    [SerializeField] private float _hoverHeight = 5f;
    [SerializeField] private float _chargeSpeed = 12f;
    [SerializeField] private float _phase2ChargeSpeed = 14f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _phase2ProjectileInterval = 1f;

    private enum BossPhase { ... }
    private BossPhase _phase = BossPhase.Phase1Hover;
    private float _phaseTimer;
    private float _projectileTimer;

    protected override void UpdateBehavior() { /* state machine */ }
    protected override void UpdateGroundedState() { _isGrounded = true; /* skip */ }
    protected override void Die() { /* 缩放 0 动画 + 掉落 */ }
}

// === Enemies/Boss/BossSpawner.cs === 新增
public sealed class BossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private float _spawnHeightAboveCaller = 8f;

    public void Spawn(Vector3 callerPosition);
}
```

## 依赖
- 任务 012 Enemy（继承）
- 任务 013 PlayerCombat（OnAttack 分发器扩展）
- 任务 020 武器扩展 + 初始物品机制（新建存档默认带召唤物）
- 任务 021 Projectile（Boss Phase 2 用）
- 任务 008 Inventory（消耗物品）
- 任务 018 SaveData（CreateNewGameDefault 加 Item_SuspiciousEye）

## 文件清单

### 新增
- `Assets/Scripts/Enemies/Boss/EyeOfNight.cs`
- `Assets/Scripts/Enemies/Boss/BossSpawner.cs`
- `Assets/Prefabs/Enemies/EyeOfNight.prefab`
- `Assets/Prefabs/Enemies/Spawners/EyeOfNightSpawner.prefab`
- `Assets/Data/Items/Item_SuspiciousEye.asset`（Consumable）
- `Assets/Data/Items/Item_NightShard.asset`（Material）

### 修改
- `Assets/Scripts/Items/ItemType.cs` — 新增 Consumable
- `Assets/Scripts/Items/ItemData.cs` — 新增 3 字段
- `Assets/Scripts/Player/PlayerCombat.cs` — OnAttack 分发器扩展
- `Assets/Scripts/Save/SaveData.cs` — `CreateNewGameDefault` 在 Slot[2] 增加 Item_SuspiciousEye ×1
- `Assets/Data/ItemDatabase.asset` — 注册 Item_SuspiciousEye / Item_NightShard

### 美术
- Boss 贴图 **暂用占位**（深紫色巨型 Slime sprite，scale 1.5）—— 等用户后续提供
- 召唤物 sprite 同样占位

## 验收标准

### 召唤
- [ ] 新建存档进入 SampleScene，Slot[2] 是 Item_SuspiciousEye ×1
- [ ] 选中 Slot[2]，左键 → 玩家上方 8 米生成 Boss，物品 -1
- [ ] 物品数量为 0 后槽位变空，左键不再触发召唤
- [ ] 召唤冷却 1 秒生效（背包还有第二个 SuspiciousEye 时连点不连续召唤）

### Boss 行为
- [ ] Boss 出现后约 2 秒开始第一次冲撞（Phase 1 悬停时间）
- [ ] 冲撞穿过玩家位置，造成 25 接触伤害
- [ ] HP 降到 200 时切到 Phase 2
- [ ] Phase 2 开始发射红色投射物（约 1 秒 1 颗，命中玩家 12 伤）
- [ ] Phase 2 冲撞速度比 Phase 1 明显更快
- [ ] HP 0 时缩放 → 0 动画后销毁
- [ ] 死亡掉落 3 个 Item_NightShard

### 综合
- [ ] Boss 不受地形阻挡（飞行）
- [ ] Boss 离开屏幕时不消失（不走 EnemySpawner 的 AliveCount 上限 —— Boss 不应被算进去）
- [ ] 玩家击杀 Boss 后再次召唤（如果背包有第二个）能正常生成新一轮
- [ ] 编译无错误、无警告
- [ ] MCP 截图：召唤瞬间 / Phase 1 冲撞 / Phase 2 弹幕 / 击杀掉落

## 注意事项

### Boss 不算进 EnemySpawner.AliveCount
EnemySpawner 用 HashSet 跟踪敌人计数。Boss 实例化时**不订阅** EnemySpawner，让它独立。
具体做法：BossSpawner.Spawn 直接 `Instantiate`，Boss 不被 EnemySpawner 知道。
（或者更简单：Boss 死亡时 EnemySpawner 不需要补刷，因为不在它的 HashSet 里。）

### Phase 切换不要在 TakeDamage 内部完成
基类 `TakeDamage` 检查 HP=0 调 `Die`。Phase 切换写在 `UpdateBehavior` 里：每帧检查 `_currentHealth < MaxHealth/2 && _phase 还在 Phase1` 时切换。这样不耦合基类。

### 占位 Boss 视觉
当前美术资源没有 Boss 贴图。占位方案：
- 取 `slime_idle1_purple_0` sprite，scale 1.5
- SpriteRenderer.color 改为深紫色 (0.3, 0.0, 0.4)
- 后续用户找到合适 Boss 贴图后开 023.1 任务替换

Codex 实现完后**主动提醒用户找贴图**。

### 召唤物使用范围
当前不限地形 —— 玩家任何地点都能召唤。后续如果要做"必须在地表 / 必须夜间"，给 ItemData 加约束字段，
本任务**不做**，避免提前抽象。

### 不做的事
- **不做 Boss 血条 UI** —— 留给独立 UI 任务
- **不做 Boss 战 BGM 切换** —— 等音频系统
- **不做多 Boss** —— 023 只做 EyeOfNight
- **不做 Boss 死亡奖励金币 / 经验** —— 只掉物品
- **不做 Boss 全屏震屏 / 受击效果** —— 留给特效任务

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/023-boss-and-summon.md`
写一份交付记录。

**交付记录额外要求**：
1. **MCP 截图**：召唤瞬间、Phase 1、Phase 2、击杀
2. **战斗时长记录**：从召唤到击杀实测耗时（用骑士剑全程）
3. **占位贴图说明**：明确写"Boss / 召唤物 sprite 待用户提供，当前为占位"
4. **Phase 切换日志**：MCP 打印 HP 跨越 200 时的切换事件
