# 任务 013 - 玩家战斗系统（PlayerCombat + 剑武器）

## 目标
玩家选中快捷栏中的剑后，按鼠标左键朝鼠标方向挥砍，扇形范围内对多目标各造成 1 次伤害。
敌人受击闪白 + 击退反馈。为后续武器（斧/锤/弓/法杖）扩展打好数据驱动地基。

## 接口签名

```csharp
// 1) ItemData 扩展武器字段（仅 ItemType.Weapon 时填写）
public sealed class ItemData : ScriptableObject
{
    // ...existing fields...
    [SerializeField] private int _weaponDamage;          // 基础伤害
    [SerializeField] private float _weaponRange;         // 命中半径（世界单位）
    [SerializeField] private float _swingArcDegrees;     // 扇形开口角度（默认 100）
    [SerializeField] private float _swingDuration;       // 单次挥砍总耗时（秒）= 攻击冷却
    [SerializeField] private float _knockbackForce;      // 命中击退冲量

    public int WeaponDamage => _weaponDamage;
    public float WeaponRange => _weaponRange;
    public float SwingArcDegrees => _swingArcDegrees;
    public float SwingDuration => _swingDuration;
    public float KnockbackForce => _knockbackForce;
}

// 2) 玩家战斗组件（Player 上挂载）
[RequireComponent(typeof(Inventory))]
public sealed class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform _weaponPivot;          // Player/WeaponPivot 子物体
    [SerializeField] private SpriteRenderer _weaponRenderer;  // Player/WeaponPivot/Weapon 子物体
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Inventory _inventory;

    [Header("Combat")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _attackSpeedMultiplier = 1f; // 角色属性占位
    [SerializeField] private float _damageMultiplier = 1f;      // 角色属性占位

    [Header("Pivot")]
    [SerializeField] private float _pivotOffsetX = 0.2f;        // 相对 Player 的水平偏移

    public bool IsSwinging { get; }

    // 由 PlayerInput 转发；Input Action 名 "Attack"，Mouse/Left
    public void OnAttack(InputAction.CallbackContext ctx);
}

// 3) Enemy 受击扩展：TakeDamage 增加击退参数；老签名保留转调
public abstract class Enemy : MonoBehaviour
{
    public void TakeDamage(int damage);                                      // 保留，转调下方
    public void TakeDamage(int damage, Vector2 knockbackDir, float force);   // 新增
    // 内部：受击闪白协程（修改 SpriteRenderer.color，0.1s 后复原）
}
```

## 依赖
- 任务 007 ItemData（扩展字段）
- 任务 008 Inventory（读取当前选中槽位 → ItemStack）
- 任务 009 Hotbar UI（已实现选中索引切换）
- 任务 012 Enemy（TakeDamage 已存在，本任务扩展签名）
- Unity Input System（已在 PlayerInput 中接入）

## 文件清单
- `Assets/Scripts/Items/ItemData.cs` — 扩展武器字段（**修改**）
- `Assets/Scripts/Player/PlayerCombat.cs` — 新增，挥砍主逻辑
- `Assets/Scripts/Enemies/Enemy.cs` — TakeDamage 加击退参数 + 闪白协程（**修改**）
- `Assets/Settings/Input/PlayerInputActions.inputactions` — 新增 `Attack` Action（Mouse/Left Button），Player Action Map（**修改**）
- `Assets/ScriptableObjects/Items/Item_KnightSword.asset` — 通过 MCP 创建：Type=Weapon, Damage=10, Range=1.4, Arc=100, Duration=0.35, Knockback=6, MaxStack=1, Icon=weapon_knight_sword 切片后的 Sprite
- `Assets/Art/Sprites/Weapons/weapon_knight_sword.png` 导入设置：PPU=16，Filter=Point，Compression=None，Pivot=Bottom，Sprite Mode=Single（**通过 MCP 配置**）
- 场景 Player 子物体结构（**通过 MCP 创建**）：
  ```
  Player
  ├── GroundCheck
  └── WeaponPivot (Transform, localPos=(0.2, 0, 0))
      └── Weapon (SpriteRenderer, sprite=weapon_knight_sword, sortingOrder 比 Player 高 1)
  ```

## 验收标准
- [ ] 选中非武器槽位（方块/空槽）时，左键无挥砍、无冷却消耗
- [ ] 选中剑槽位时，左键触发挥砍，右键仍走 PlayerBlockInteraction 放置
- [ ] WeaponPivot 持武器时 `_weaponRenderer.enabled=true`，切走武器槽位则隐藏
- [ ] 挥砍弧线沿鼠标方向：起始角=鼠标角-Arc/2，结束角=鼠标角+Arc/2，用 Slerp 在 Duration 内完成
- [ ] WeaponPivot 的 localPosition.x 跟随玩家 flipX 镜像（0.2 ↔ -0.2）
- [ ] 单次挥砍对扇形范围内每个敌人最多伤害 1 次（HashSet 去重）
- [ ] 命中后敌人沿"敌人位置 - 玩家位置"方向被推开，且 SpriteRenderer 闪白 0.1s 后复原
- [ ] 挥砍中再次按下左键不重复触发
- [ ] LayerMask 只命中 Enemy 层，不会误伤玩家
- [ ] 击杀史莱姆掉落物品正常（沿用任务 012 Enemy.SpawnDrop）

## 注意事项
- **挥砍判定时机**：在 Slerp 旋转过程中，每帧用 `Physics2D.OverlapCircleAll(player.position, range, _enemyLayer)` 取候选，再用 `Vector2.Angle(swingDirection, enemy-player)` 过滤是否在扇形内。命中过的敌人 instanceID 加入 HashSet，单次挥砍内不再重复命中。
- **闪白实现**：协程切 `SpriteRenderer.color = Color.white`（强一点用 (1, 1, 1, 1) 配合保留原色变量），0.1s 后还原。先不要引入材质切换/shader，太重。
- **击退实现**：`enemy.GetComponent<Rigidbody2D>().AddForce(dir * force, ForceMode2D.Impulse)`。注意 Slime 是 Dynamic Rigidbody，不需要额外配置。
- **数值最终公式**：
  - 伤害 = `Mathf.RoundToInt(WeaponDamage * _damageMultiplier)`
  - 单次挥砍冷却 = `SwingDuration / _attackSpeedMultiplier`（multiplier 越大越快）
- **Pivot 偏移**：手臂大致在玩家肚脐略偏前的位置，0.2 是初始值，挥砍效果不对再调；左/右翻转用 flipX 同步而不是 negative scale。
- **不引入 Animator**：纯代码 Slerp 旋转 + Sprite 显示/隐藏，避免动画状态机开销。
- **角色属性占位**：`_attackSpeedMultiplier` 和 `_damageMultiplier` 现在硬编码 1f；未来 PlayerStats 系统接入时只需赋值，不动 PlayerCombat 逻辑。
- **EnemyDebugInput 保留**：在 014 EnemySpawner 出来之前，PlayerCombat 配合 EnemyDebugInput 一起测试很方便，不要在本任务删它。
- **不做的事**：弧线 trail 特效、攻击音效、暴击、连击、武器耐久、多武器同时切换 —— 都留给后续任务。

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/013-player-combat.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
