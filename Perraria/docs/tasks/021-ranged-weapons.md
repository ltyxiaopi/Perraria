# 任务 021 - 远程武器系统（弓 + 法杖 + 投掷斧）

## 目标
引入"投射物 (Projectile)"组件，让玩家可以使用：
1. **弓**（Item_Bow / Item_Bow2）：消耗 Item_Arrow 弹药，发射受重力影响的箭矢
2. **法杖**（Item_GreenMagicStaff / Item_RedMagicStaff）：直接发射魔法弹（无重力，无弹药消耗 —— 走法力或冷却）
3. **投掷斧**（Item_ThrowingAxe）：消耗物品本身（武器即弹药），抛物线弹道，命中后停在地形上变可拾取掉落物

为后续 Boss 远程攻击、敌人远程 AI 提供可复用的 `Projectile` 组件。

## 设计概要

### 远程武器子分类（不扩 ItemType）
不要在 `ItemType` 枚举里新增 `RangedWeapon`。沿用 `Type=Weapon`，靠 `ItemData` 上的字段区分远近：

```csharp
public enum WeaponSubType : byte
{
    Melee  = 0,   // 默认值，013/020 的剑、斧、锤、矛全部走这里
    BowLike = 1,  // 需要弹药 ItemData 引用
    Staff   = 2,  // 不需要弹药，走冷却
    Throwable = 3, // 武器本身即投射物，使用即消耗
}
```

`ItemData` 新增字段：

```csharp
[SerializeField] private WeaponSubType _weaponSubType = WeaponSubType.Melee;
[SerializeField] private GameObject _projectilePrefab;     // 远程必填
[SerializeField] private ItemData _ammoItem;               // 仅 BowLike 用，弹药 ItemData
[SerializeField] private float _projectileSpeed = 12f;     // 初速
[SerializeField] private float _projectileLifetime = 5f;   // 超时销毁
[SerializeField] private bool _projectileGravity = true;   // 弓/投掷斧 = true，法杖 = false
[SerializeField] private float _attackCooldown = 0.5f;     // 远程武器冷却（不复用 SwingDuration，因为没有挥砍动作）
```

### Projectile 组件
通用投射物，敌人和 Boss 也会用：

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Projectile : MonoBehaviour
{
    public enum Owner { Player, Enemy }

    private int _damage;
    private float _knockbackForce;
    private LayerMask _hitTargetLayer;   // Player 投射物 = Enemy 层；敌人投射物 = Player 层
    private LayerMask _terrainLayer;     // Ground 层，命中地形即停
    private Owner _owner;
    private float _lifetimeRemaining;
    private bool _isStuck;               // 命中地形后是否停留（投掷斧 true，箭可选 true，魔法弹 false）
    private ItemData _pickupItemOnStick; // 投掷斧专用：变成 ItemDrop 的物品

    public void Launch(Vector2 direction, float speed, ProjectileLaunchParams p);
    private void OnCollisionEnter2D(Collision2D collision);   // 命中地形或目标
}

public struct ProjectileLaunchParams
{
    public int Damage;
    public float Knockback;
    public float Lifetime;
    public bool UseGravity;
    public Owner Owner;
    public LayerMask TargetLayer;
    public LayerMask TerrainLayer;
    public bool StickOnTerrain;
    public ItemData PickupItemOnStick;   // null = 不可拾取
}
```

### PlayerCombat 远程攻击分支
当前 `PlayerCombat.OnAttack` 只走挥砍。改造为分发器：

```csharp
public void OnAttack(InputAction.CallbackContext ctx)
{
    if (!ctx.performed || ...) return;
    if (!TryGetSelectedWeapon(out ItemData weaponItem)) return;

    switch (weaponItem.WeaponSubType)
    {
        case WeaponSubType.Melee:     StartSwing(weaponItem); break;
        case WeaponSubType.BowLike:   TryFireBow(weaponItem); break;
        case WeaponSubType.Staff:     TryFireStaff(weaponItem); break;
        case WeaponSubType.Throwable: TryThrow(weaponItem); break;
    }
}
```

冷却用现成的 `IsSwinging` 协程模式扩展为通用 `_attackCooldownTimer` 字段，所有子类型共享一个冷却。

### 弹药消耗
`TryFireBow`：
1. 检查 `_inventory` 是否包含 `weaponItem.AmmoItem` 至少 1 个
2. 不够 → 不发射，可以播 UI 提示音（本任务不做音效）
3. 够 → `_inventory.RemoveItem(weaponItem.AmmoItem, 1)`，发射 1 枚箭

> Inventory 当前是否有 `RemoveItem(ItemData, int)` API？需要 Codex 确认；如缺失，本任务新增。

### 投掷斧消耗
`TryThrow`：
1. 拿到当前选中槽 → 发射投射物，`PickupItemOnStick = weaponItem`
2. `_inventory.RemoveFromSlot(_inventory.SelectedHotbarIndex, 1)` 消耗 1 把
3. 命中地形 → 停下并变成 `ItemDrop`（`weaponItem ×1`），玩家走过去捡回来

### 资产清单（Codex 通过 MCP 创建）

#### 远程武器
| 资产名 | 贴图 | SubType | Damage | Speed | Cooldown | Gravity | Ammo | Stick |
|---|---|---|---|---|---|---|---|---|
| Item_Bow | weapon_bow | BowLike | 12 | 14 | 0.5 | true | Item_Arrow | false |
| Item_Bow2 | weapon_bow_2 | BowLike | 16 | 16 | 0.6 | true | Item_Arrow | false |
| Item_GreenMagicStaff | weapon_green_magic_staff | Staff | 10 | 10 | 0.4 | false | (空) | false |
| Item_RedMagicStaff | weapon_red_magic_staff | Staff | 14 | 12 | 0.45 | false | (空) | false |
| Item_ThrowingAxe | weapon_throwing_axe | Throwable | 14 | 11 | 0.7 | true | (自身) | true |

#### 投射物 Prefab
- `Assets/Prefabs/Projectiles/ArrowProjectile.prefab`：使用 `weapon_arrow.png`，Rigidbody2D Dynamic，CapsuleCollider2D，Projectile 组件
- `Assets/Prefabs/Projectiles/MagicProjectileGreen.prefab`：临时绿色圆点（可用纯色 sprite 或后续美术补），Projectile 组件，Gravity=0
- `Assets/Prefabs/Projectiles/MagicProjectileRed.prefab`：同上，红色
- `Assets/Prefabs/Projectiles/ThrowingAxeProjectile.prefab`：使用 `weapon_throwing_axe.png`，飞行中持续旋转（角速度 720°/s）

#### 弹药
| 资产名 | 贴图 | Type | MaxStack |
|---|---|---|---|
| Item_Arrow | weapon_arrow | Material | 99 |

> Item_Arrow 设为 Material 类型（不是 Weapon），纯弹药不可独立装备。

## 接口签名

```csharp
// === Items/WeaponSubType.cs ===
public enum WeaponSubType : byte
{
    Melee = 0,
    BowLike = 1,
    Staff = 2,
    Throwable = 3,
}

// === Items/ItemData.cs === 新增字段（已在设计概要列出）

// === Combat/Projectile.cs ===
// 见设计概要

// === Combat/ProjectileLaunchParams.cs === 同上

// === Player/PlayerCombat.cs === 修改
//   - OnAttack 分发到 4 个子方法
//   - 新增 _attackCooldownTimer 字段，所有攻击类型共享冷却
//   - 新增 TryFireBow / TryFireStaff / TryThrow / FireProjectile（共用核心）
```

## 依赖
- 任务 008 Inventory（需 `RemoveItem(ItemData, int)`，如缺失则本任务补）
- 任务 011 ItemDrop（投掷斧命中地形变 ItemDrop）
- 任务 012 Enemy（投射物命中敌人调 `TakeDamage(damage, knockback, force)`）
- 任务 013 PlayerCombat（OnAttack 分发器改造）
- 任务 020 武器扩展（已建立 ItemData → ItemDatabase 注册流程）

## 文件清单

### 新增
- `Assets/Scripts/Items/WeaponSubType.cs`
- `Assets/Scripts/Combat/Projectile.cs`
- `Assets/Scripts/Combat/ProjectileLaunchParams.cs`
- `Assets/Prefabs/Projectiles/ArrowProjectile.prefab`（MCP 创建）
- `Assets/Prefabs/Projectiles/MagicProjectileGreen.prefab`
- `Assets/Prefabs/Projectiles/MagicProjectileRed.prefab`
- `Assets/Prefabs/Projectiles/ThrowingAxeProjectile.prefab`
- `Assets/Data/Items/Item_Bow.asset`、`Item_Bow2.asset`、`Item_GreenMagicStaff.asset`、`Item_RedMagicStaff.asset`、`Item_ThrowingAxe.asset`、`Item_Arrow.asset`

### 修改
- `Assets/Scripts/Items/ItemData.cs` — 新增 6 个字段（SubType, ProjectilePrefab, AmmoItem, Speed, Lifetime, Gravity, Cooldown）
- `Assets/Scripts/Items/Inventory.cs` — 如缺 `RemoveItem(ItemData, int)` 则补
- `Assets/Scripts/Player/PlayerCombat.cs` — 分发器改造
- `Assets/Data/ItemDatabase.asset` — 追加远程武器和弹药条目

### 资产导入设置（MCP）
- `weapon_bow.png` / `weapon_bow_2.png` / `weapon_arrow.png` / `weapon_green_magic_staff.png` / `weapon_red_magic_staff.png` 切片：PPU=16, Point, No Compression, Pivot=Bottom（弓） / Center（箭、法杖）, **Trim**

## 验收标准

### 弓
- [ ] 装备 Item_Bow，背包有 ≥1 支 Arrow，左键发射 1 支箭，Arrow 数量 -1
- [ ] 背包没有 Arrow 时左键不发射，无报错
- [ ] 箭沿鼠标方向飞，受重力下坠形成抛物线
- [ ] 箭命中 Slime 造成 12 伤 + 击退（Item_Bow2 = 16 伤）
- [ ] 箭命中地形（Ground 层）后自动销毁（StickOnTerrain=false 不留物）
- [ ] 箭超时（5 秒未命中）销毁
- [ ] 0.5 秒冷却内无法连射

### 法杖
- [ ] Green 法杖发射绿色魔法弹，Red 发射红色，无重力直线飞行
- [ ] 不消耗任何物品（无弹药字段）
- [ ] 命中 Slime 造成对应伤害（绿 10 / 红 14），击退轻微（默认 KnockbackForce=4）
- [ ] 命中地形或超时销毁
- [ ] 0.4 / 0.45 秒冷却

### 投掷斧
- [ ] 装备 Item_ThrowingAxe（背包内仅 1 把），左键投掷，物品 -1
- [ ] 飞行途中可见旋转（720°/s）
- [ ] 命中 Slime → 造成 14 伤 + 击退，斧头销毁（不留物，已对敌生效）
- [ ] 命中地形 → 停在该位置生成 `ItemDrop(Item_ThrowingAxe, 1)`，玩家拾取后回到背包
- [ ] 投掷后冷却 0.7 秒生效

### 综合
- [ ] 所有远程武器在切换时正确显示在 WeaponPivot/Weapon（沿用 020 的 Renderer 方案）
- [ ] 切回近战武器后挥砍 / 冷却恢复正常（没有被远程冷却卡住）
- [ ] Projectile 组件复用：把 ArrowProjectile.prefab 给 Boss 之后能直接 Launch（不需要新代码）
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误

## 注意事项

### 投射物层级 / Layer 设置
- 新增 `Projectile` Layer，玩家投射物和敌人投射物都用同一个 Layer
- Physics2D Layer 矩阵：Projectile 不和 Player Layer 碰撞、不和 Projectile 自己碰撞，只和 Enemy / Ground 碰撞（敌方反过来）
- 投射物 Rigidbody2D 用 `Continuous` 碰撞检测，避免高速箭矢穿模

### 法力系统的位置
本任务**不引入法力值** —— 法杖只走冷却。法力系统等到玩家属性扩展任务（独立的 PlayerStats 任务）再加，
那时候在 ItemData 加 `_manaCost` 字段，PlayerCombat 检查 PlayerStats.CurrentMana 即可，不需要重写本任务的分发器。

### 投掷斧"命中即停"的实现
不要用 `Rigidbody2D.bodyType = Static`（会让 Collider 变 trigger 失效）。做法：
1. 检测到命中 Ground 层 → 把 `Rigidbody2D.linearVelocity = Vector2.zero` 且 `gravityScale = 0`
2. 把斧子父级设为命中点附近的 GameObject 或不设父级（直接放着）
3. 旋转锁住（`freezeRotation = true`）
4. 销毁自身 Projectile 脚本（避免再触发 OnCollisionEnter2D）
5. 在原地实例化 `ItemDrop(Item_ThrowingAxe, 1)`，然后 `Destroy(gameObject)`

### 箭的 Pivot
`weapon_arrow.png` 的 Pivot 必须是箭头方向 —— 用 `Custom` 把 Pivot 移到箭尾（这样 transform.right 指向箭头方向）。
飞行时 `transform.rotation = Quaternion.FromToRotation(Vector3.right, velocity.normalized)`。

### Spear 不做远程
`Item_Spear` 已在 020 按近战处理（长矛挥砍），021 不动它。如果未来玩家觉得"长矛应该能投掷"，新增 `Item_Javelin` 资产做投掷型，不要把现有 Spear 转换。

### Item_ThrowingAxe 首次创建
原计划在 020 按近战处理后保留，已决定划入本任务统一做投掷弹道 —— 020 不创建此资产，021 首次按 `WeaponSubType.Throwable` 配置。`weapon_throwing_axe.png` 的切片设置（PPU=16, Point, Pivot=Center, Trim）也在本任务一并完成。

### 不做的事
- **不做法力 / 蓝量条** —— 等 PlayerStats
- **不做箭矢轨迹预览**
- **不做暴击 / 穿透** —— Projectile 命中即终止
- **不做敌人远程攻击** —— Boss 任务（023）会用同一个 Projectile 组件
- **不做弓的拉弦动画 / 法杖咏唱动画**

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/021-ranged-weapons.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。

**交付记录额外要求**：
1. **MCP 截图**：弓射出 / 法杖发射 / 投掷斧命中地形变 ItemDrop 三张
2. **测试日志**：弹药消耗（射 5 支箭后 Arrow 数量从 N 变 N-5）、投掷斧拾取回来（背包数量恢复）
3. **冷却切换无 bug 验证**：远程切近战切远程，冷却字段不串
