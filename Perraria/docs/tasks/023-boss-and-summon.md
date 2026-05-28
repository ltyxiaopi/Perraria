# 任务 023 - Boss 敌人 + 召唤系统

## 目标
1. 实现首个 Boss `EyeOfCorruption`（黄眼绿酸主题，对标 Terraria 克苏鲁之眼）：高 HP、多阶段、远程冲撞 + 酸液弹幕。
2. 引入「召唤物 (Summon Item)」机制：玩家使用消耗品类型的物品触发 Boss 生成。
3. 玩家**新建存档**时初始背包带 1 个 `Item_SuspiciousEye`（沿用 020 的初始物品机制扩展）。

## 美术资源前置检查（Claude 已完成）
- **需要新素材**：是
- **素材清单**：
  - [x] `Assets/Art/Sprites/Enemies/Boss/EyeOfCorruption/Eye Monster Sprite Sheet.png` —— 已落位
    - 来源：[Elthen — 2D Pixel Art Flying Eye Monster](https://elthen.itch.io/2d-pixel-art-flying-eye-monster)
    - 授权：免费（Name your own price）/ 商用 OK / 不强制署名（建议在游戏致谢里加 "Elthen's Pixel Art Shop"）
- **切片规格**：
  - 纹理总尺寸：**256×160**
  - 单帧：**32×32**
  - 网格：8 列 × 5 行（共 40 cells，部分为空）
  - **导入设置**（与 Slime/Zombie 一致）：
    - Texture Type = Sprite (2D and UI)
    - Sprite Mode = Multiple
    - Pixels Per Unit = **32**（1 帧 = 1 世界单位高，配合 Boss prefab Transform Scale = **2.0** 实现"2 格高"巨型 Boss 视觉）
    - Filter Mode = **Point**（必须，像素艺术）
    - Compression = None
    - Generate Mip Maps = Off
  - **可见帧分布**（左到右、上到下编号，0-indexed）：
    - **Row 0 (frames 0-7)**：Idle / Movement —— 眼球完整 8 帧（瞳孔扫视）
    - **Row 1 (frames 8-11)**：Detect —— 眼睛瞪大警觉 4 帧（frames 12-15 空）
    - **Row 2 (frames 16-23)**：Drip Acid —— 攻击前摇眼睛张大并积酸 8 帧
    - **Row 3 (frames 24-27)**：Acid Projectile（mid-air）—— 散落酸滴 4 帧（frames 28-31 空）
    - **Row 4 (frames 32-36)**：Acid Splash（impact）—— 酸液飞溅 5 帧（frames 37-39 空）

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

> Boss 召唤时机由 `_summonPrefab` 自带的 `BossSpawner` 组件决定（生成 Boss 实体并销毁自身）。

### Boss 多阶段状态机
继承 `Enemy` 但**子状态机**写在 `EyeOfCorruption` 内部，不污染基类的 `EnemyState` 枚举：

```csharp
public sealed class EyeOfCorruption : Enemy
{
    private enum BossPhase { Phase1Hover, Phase1Charge, Phase2Hover, Phase2Charge, Defeated }

    private BossPhase _phase = BossPhase.Phase1Hover;
    private float _phaseTimer;
    // 行为参数：Phase1 = 悬停 + 冲撞；Phase2（HP < 50%）= 悬停 + 冲撞 + 酸液弹幕
}
```

**Phase 1（HP 100% → 50%）**：
- 在玩家上方 5 米悬停 2 秒，左右晃动
- 使用 sprite **Row 0 (Idle)** 动画 8 帧（4 FPS 循环）
- 然后向玩家位置直线冲撞 1 次（速度 12，持续 1 秒），冲撞期间切到 **Row 1 (Detect)** 4 帧（8 FPS）
- 冲撞结束回到 idle 悬停，循环

**Phase 2（HP 50% → 0%）**：
- 悬停 1.5 秒（更短）
- 使用 **Row 1 (Detect)** 4 帧作为悬停动画（眼睛长期瞪大，区分 Phase 1 视觉）
- 冲撞 1 次（速度 14）
- 悬停时每 1 秒发射 1 颗 `AcidProjectile` 投射物（伤害 12）—— 发射前摇切到 **Row 2 (Drip Acid)** 一次性播放 8 帧（10 FPS）
- 循环

**Defeated**：
- HP = 0 时触发：播 1 秒缩放 → 0 动画，掉落物品（详见下方），销毁

### Boss 不受重力 / 不需要落地
Rigidbody2D `gravityScale = 0`，重写 `UpdateGroundedState() { _isGrounded = true; }`（让基类逻辑别走重力）。
（Slime/Zombie 不受影响，只 override 这个 Boss 子类。）

### Boss 数值
| 维度 | 值 |
|---|---|
| MaxHealth | 400 |
| ContactDamage | 25 |
| DetectionRange | 30 |
| 阶段切换 HP | 200 |
| 冲撞速度 | Phase1=12, Phase2=14 |
| 酸液弹幕伤害 | 12 |
| 弹幕发射间隔 | 1 秒（仅 Phase 2 悬停期间） |
| 击杀奖励 | `Item_CorruptShard` ×3（新增材料） |

> 金币系统未实现，先掉物品。

### 召唤物视觉
`Item_SuspiciousEye` 直接复用 EyeOfCorruption sprite sheet 的 **frame 0**（Row 0 第 1 帧 idle 眼球）作为图标。
- 32×32 在 inventory slot 内 UI 会自动 scale-fit（参考 020 大尺寸物品的处理方式）
- 不需要单独的 16×16 图标

Boss 召唤流程：
1. `_summonPrefab` 实例化 → `BossSpawner` GameObject
2. `BossSpawner` 在玩家上方 8 米处实例化 `EyeOfCorruption.prefab`，自身销毁
3. Boss 开始 Phase 1

### 弹幕
Boss Phase 2 远程攻击使用 **sprite sheet 自带的酸滴**（不复用 021 的 MagicProjectileRed）：
- 新增 `AcidProjectile.prefab`，挂 021 的 `Projectile` 组件
- Sprite = sprite sheet 的 **frame 24**（Row 3 第 1 帧酸滴）
  - 静态单帧即可，不做飞行动画（保持轻量）
  - 如果时间允许，可加 SpriteRenderer 帧切换在 Row 3 的 4 帧间循环（10 FPS）—— **可选，不强求**
- 配置：
  - `Owner = Owner.Enemy`，`TargetLayer = Player`
  - `Damage = 12`，`Knockback = 5`
  - `gravityScale = 0`（不重力）
  - 命中或超时销毁（沿用 Projectile 既有逻辑）
- **不需要碰 Projectile.cs 源码**，021 已经做成可复用

> Row 4 (Acid Splash) 5 帧本任务**不消费**。后续做特效系统时可作为命中飞溅，023 先省略。

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

// === Enemies/Boss/EyeOfCorruption.cs === 新增（继承 Enemy）
public sealed class EyeOfCorruption : Enemy
{
    [Header("Boss")]
    [SerializeField] private float _hoverHeight = 5f;
    [SerializeField] private float _chargeSpeed = 12f;
    [SerializeField] private float _phase2ChargeSpeed = 14f;
    [SerializeField] private GameObject _acidProjectilePrefab;
    [SerializeField] private float _phase2ProjectileInterval = 1f;

    [Header("Phase Animations")]
    [SerializeField] private Sprite[] _idleFrames;       // Row 0 frames 0-7
    [SerializeField] private Sprite[] _detectFrames;     // Row 1 frames 8-11
    [SerializeField] private Sprite[] _dripAcidFrames;   // Row 2 frames 16-23

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
- 任务 021 Projectile（Phase 2 酸滴投射物复用 Projectile 组件）
- 任务 008 Inventory（消耗物品）
- 任务 018 SaveData（CreateNewGameDefault 加 Item_SuspiciousEye）

## 文件清单

### 新增
- `Assets/Scripts/Enemies/Boss/EyeOfCorruption.cs`
- `Assets/Scripts/Enemies/Boss/BossSpawner.cs`
- `Assets/Prefabs/Enemies/EyeOfCorruption.prefab`
- `Assets/Prefabs/Enemies/Spawners/EyeOfCorruptionSpawner.prefab`
- `Assets/Prefabs/Projectiles/AcidProjectile.prefab`
- `Assets/Data/Items/Item_SuspiciousEye.asset`（Consumable）
- `Assets/Data/Items/Item_CorruptShard.asset`（Material）

### 已落位（Claude 已确认）
- `Assets/Art/Sprites/Enemies/Boss/EyeOfCorruption/Eye Monster Sprite Sheet.png` —— Codex 负责切片配置：32×32 网格、Sprite Mode Multiple、Filter Point、Compression None、PPU 32

### 修改
- `Assets/Scripts/Items/ItemType.cs` — 新增 Consumable
- `Assets/Scripts/Items/ItemData.cs` — 新增 3 字段
- `Assets/Scripts/Player/PlayerCombat.cs` — OnAttack 分发器扩展
- `Assets/Scripts/Save/SaveData.cs` — `CreateNewGameDefault` 在 Slot[2] 增加 Item_SuspiciousEye ×1
- `Assets/Data/ItemDatabase.asset` — 注册 Item_SuspiciousEye / Item_CorruptShard

## 验收标准

### 召唤
- [ ] 新建存档进入 SampleScene，Slot[2] 是 Item_SuspiciousEye ×1（图标为 idle 眼球第 1 帧）
- [ ] 选中 Slot[2]，左键 → 玩家上方 8 米生成 Boss，物品 -1
- [ ] 物品数量为 0 后槽位变空，左键不再触发召唤
- [ ] 召唤冷却 1 秒生效（背包还有第二个 SuspiciousEye 时连点不连续召唤）

### Boss 行为
- [ ] Boss 出现后约 2 秒开始第一次冲撞（Phase 1 悬停时间）
- [ ] Phase 1 悬停时播放 Row 0 idle 动画（眼球扫视）
- [ ] 冲撞穿过玩家位置，造成 25 接触伤害
- [ ] HP 降到 200 时切到 Phase 2，悬停动画切换到 Row 1 detect（眼睛瞪大），视觉上明显区分
- [ ] Phase 2 发射酸液投射物前播放 Row 2 drip acid 前摇动画（8 帧）
- [ ] Phase 2 投射物约 1 秒 1 颗，命中玩家 12 伤
- [ ] Phase 2 冲撞速度比 Phase 1 明显更快
- [ ] HP 0 时缩放 → 0 动画后销毁
- [ ] 死亡掉落 3 个 Item_CorruptShard

### 综合
- [ ] Boss 不受地形阻挡（飞行）
- [ ] Boss 离开屏幕时不消失（不走 EnemySpawner 的 AliveCount 上限 —— Boss 不应被算进去）
- [ ] 玩家击杀 Boss 后再次召唤（如果背包有第二个）能正常生成新一轮
- [ ] 编译无错误、无警告
- [ ] MCP 截图：召唤瞬间 / Phase 1 冲撞 / Phase 2 弹幕 / 击杀掉落

## 注意事项

### Boss 不算进 EnemySpawner.AliveCount
EnemySpawner 用 HashSet 跟踪敌人计数。Boss 实例化时**不订阅** EnemySpawner，让它独立。
具体做法：`BossSpawner.Spawn` 直接 `Instantiate`，Boss 不被 EnemySpawner 知道。

### Phase 切换不要在 TakeDamage 内部完成
基类 `TakeDamage` 检查 HP=0 调 `Die`。Phase 切换写在 `UpdateBehavior` 里：每帧检查 `_currentHealth < MaxHealth/2 && _phase 还在 Phase1` 时切换。这样不耦合基类。

### 动画驱动方式
不使用 Animator Controller，直接代码切换 `SpriteRenderer.sprite`：
- 用一个 `float _animTime` 累加 `Time.deltaTime`
- 根据当前 phase + state 选 `Sprite[] currentFrames`，取 `currentFrames[(int)(_animTime * fps) % currentFrames.Length]` 赋给 sprite
- 简单可控，避免 Animator 状态机膨胀
- 与现有 Slime/Zombie 动画驱动方式保持一致（参考 022 zombie 实现）

### 召唤物使用范围
当前不限地形 —— 玩家任何地点都能召唤。后续如果要做"必须在地表 / 必须夜间"，给 ItemData 加约束字段，
本任务**不做**，避免提前抽象。

### 不做的事
- **不做 Boss 血条 UI** —— 留给独立 UI 任务
- **不做 Boss 战 BGM 切换** —— 等音频系统
- **不做多 Boss** —— 023 只做 EyeOfCorruption
- **不做 Boss 死亡奖励金币 / 经验** —— 只掉物品
- **不做 Boss 全屏震屏 / 受击效果** —— 留给特效任务
- **不消费 Row 4 (Acid Splash) 5 帧** —— 留给后续特效任务做命中飞溅

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/023-boss-and-summon.md`
写一份交付记录。

**交付记录额外要求**：
1. **MCP 截图**：召唤瞬间、Phase 1 idle 动画、Phase 2 detect 动画、Phase 2 酸液弹幕、击杀掉落
2. **战斗时长记录**：从召唤到击杀实测耗时（用骑士剑全程）
3. **Phase 切换日志**：MCP 打印 HP 跨越 200 时的切换事件
4. **素材署名**：在交付记录中明确写"Boss 素材来源：Elthen's Pixel Art Shop (itch.io)，免费授权"
