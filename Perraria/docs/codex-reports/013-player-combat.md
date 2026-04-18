# 任务 013 交付记录 - PlayerCombat + 剑武器

## 任务信息

- 任务编号：013
- 任务名称：PlayerCombat + 剑武器
- 实现分支：`feature/013-player-combat`

## 本次完成内容

### 1. 剑图导入配置

- 通过 Unity MCP 配置 `Assets/Art/Sprites/Weapons/weapon_knight_sword.png`
- 最终导入参数：
  - `Pixels Per Unit = 16`
  - `Filter Mode = Point`
  - `Compression = None`
  - `Sprite Mode = Single`
  - `Pivot = Bottom Center`
- 读到的 Sprite 数据：
  - `rect = (10, 29)`
  - `pivot = (5, 0)`

### 2. ItemData 扩展

- 在 `Assets/Scripts/Items/ItemData.cs` 末尾追加了武器字段，未打乱现有字段顺序：
  - `_weaponDamage`
  - `_weaponRange`
  - `_swingArcDegrees`
  - `_swingDuration`
  - `_knockbackForce`
- 同步新增只读属性：
  - `WeaponDamage`
  - `WeaponRange`
  - `SwingArcDegrees`
  - `SwingDuration`
  - `KnockbackForce`

### 3. Knight Sword 物品数据

- 通过 Unity MCP 创建 `Assets/Data/Items/Item_KnightSword.asset`
- 按仓库现有目录结构放在 `Assets/Data/Items`，没有新开 `Assets/ScriptableObjects/Items`
- 配置值如下：
  - `ItemId = 5`
  - `ItemName = "Knight Sword"`
  - `Type = Weapon`
  - `MaxStackSize = 1`
  - `WeaponDamage = 10`
  - `WeaponRange = 1.4`
  - `SwingArcDegrees = 100`
  - `SwingDuration = 0.35`
  - `KnockbackForce = 6`
  - `Icon = weapon_knight_sword`
- 已注册到 `Assets/Data/ItemDatabase.asset`

### 4. Enemy 受击扩展

- 修改 `Assets/Scripts/Enemies/Enemy.cs`
- 保留旧签名：
  - `TakeDamage(int damage)`
- 新增签名：
  - `TakeDamage(int damage, Vector2 knockbackDir, float force)`
- 新增受击效果：
  - 受击击退 `AddForce(..., ForceMode2D.Impulse)`
  - 0.1 秒闪白协程
- 旧签名会转调到新签名

### 5. PlayerCombat

- 新增 `Assets/Scripts/Player/PlayerCombat.cs`
- 实现内容：
  - 读取当前 hotbar 选中物品，只在 `ItemType.Weapon` 时允许攻击
  - 根据鼠标方向计算起始角 / 结束角
  - 使用 `Quaternion.Slerp` 驱动挥砍旋转
  - 每帧 `Physics2D.OverlapCircleAll` 查候选敌人
  - 用 `HashSet<Enemy>` 保证单次挥砍同一敌人只受击一次
  - 命中后传入击退方向和击退力度
  - 根据 hotbar 选中项动态显示/隐藏武器 Sprite
  - `WeaponPivot.localPosition.x` 跟随玩家 `flipX` 做镜像

### 6. Player 场景配置

- 通过 Unity MCP 在 `Player` 下创建：
  - `WeaponPivot`
  - `WeaponPivot/Weapon`
- 给 `Weapon` 挂了 `SpriteRenderer`
- 给 `Player` 挂了 `PlayerCombat`
- 绑定了：
  - `_weaponPivot`
  - `_weaponRenderer`
  - `_mainCamera`
  - `_inventory`
  - `_enemyLayer`
- 同时给场景内默认 hotbar 预放：
  - `Slot 0 = Knight Sword x1`
  - `Slot 1 = Dirt x20`

### 7. 输入资源

- 检查了现有输入资源 `Assets/InputSystem_Actions.inputactions`
- 当前仓库里 `Player/Attack` action 已经存在，并且已绑定：
  - `<Mouse>/leftButton`
- 因为规格要求已经满足，所以没有额外制造无意义 diff，只做了存在性校验

### 8. 与左键挖矿的兼容

- 修改 `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- 当当前选中物品是 `Weapon` 时：
  - 不进入左键挖矿逻辑
  - 清除高亮
  - 右键逻辑仍然保留在 `PlayerBlockInteraction` 路径里

## 变更文件

- `Assets/Art/Sprites/Weapons/weapon_knight_sword.png`
- `Assets/Art/Sprites/Weapons/weapon_knight_sword.png.meta`
- `Assets/Data/ItemDatabase.asset`
- `Assets/Data/Items/Item_KnightSword.asset`
- `Assets/Scripts/Enemies/Enemy.cs`
- `Assets/Scripts/Items/ItemData.cs`
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Scenes/SampleScene.unity`

## 自测结果

通过 Unity MCP 做了运行态自动化自测，最终结果为：

- `weaponVisibleOnSwordSlot=True`
- `pivotMirrorsLeft=True`
- `pivotMirrorsRight=True`
- `swingingAfterClick=True`
- `singleHitPerSwing=True`
- `outsideArcUnaffected=True`
- `flashWhiteDuringHit=True`
- `flashRestored=True`
- `knockbackApplied=True`
- `weaponHiddenOnBlockSlot=True`
- `noSwingOnBlockSlot=True`
- `blockSlotDidNotDamage=True`

说明：

- 为了稳定验证闪白，测试时对临时生成的史莱姆 SpriteRenderer 先施加了灰色 tint，再观察命中后短暂变白、随后恢复
- 自测使用的是运行态真实输入事件注入，验证的是 `Attack` action -> `PlayerCombat` -> 扇形命中链路

## Claude 审查重点

- `PlayerCombat` 是否仍然严格只对 `ItemType.Weapon` 生效
- `Enemy.TakeDamage` 旧签名转调是否保持兼容
- `PlayerBlockInteraction` 的“持武器时不挖矿”是否符合预期边界
- `SampleScene` 中 `PlayerCombat`、`WeaponPivot`、`Weapon` 和 hotbar 默认物品是否都已正确序列化

## 已知说明

- 本次没有修改 `Assets/InputSystem_Actions.inputactions` 文件内容，因为仓库当前已经满足 `Attack + Mouse/Left Button` 的规格要求
- 当前控制台仍会出现一个既有编辑器联网 warning：
  - `Account API did not become accessible within 30 seconds`
  - 该 warning 来自 `com.unity.ai.assistant`，不是本任务代码引入
