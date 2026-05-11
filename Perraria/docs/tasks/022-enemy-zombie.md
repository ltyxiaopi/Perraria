# 任务 022 - 僵尸敌人

## 目标
基于现有 `Enemy` 抽象基类新增第二种敌人 `Zombie`：地面行走式 AI（区别于 Slime 的跳跃），
更高血量、更高接触伤害，作为夜间主力敌人。

**夜间限定生成**留 hook 给 024 完成 —— 本任务把僵尸做出来 + 加进 EnemySpawner 表，先全天可生成；
024 上线后再加时段过滤。

## 设计概要

### 与 Slime 的差异
| 维度 | Slime | Zombie |
|---|---|---|
| 移动 | 跳跃（垂直 + 水平脉冲） | 行走（持续水平速度） |
| HP | 20 | 50 |
| 接触伤害 | 10 | 15 |
| 检测范围 | 10 | 12 |
| 体积 | scale 0.3 | scale 0.45（人形比球形显眼） |
| 掉落 | Item_SlimeGel ×1 | Item_RottenFlesh ×1（新增）|
| 出现时段 | 全天（020 阶段） | 全天（020 阶段）→ 仅夜间（024 起）|

### 行走 AI（区别于跳跃）
僵尸不跳，但要能上 1 格台阶（避免被一格地形卡住）：
1. `Chasing` 状态下持续给 `Rigidbody2D.linearVelocity.x = direction * walkSpeed`
2. 前方 0.5 米检测到 1 格高的方块 → 给一个**小跳**（垂直速度 = 4f）跨越障碍
3. 落差检测：脚下 2 格无地面 → 仍然走出去（自由下落，体现僵尸"不在乎死活"的设定）
4. 玩家在头顶 / 不可达时不挣扎（不会无脑跳）

### 阻挡检测
```
方块检测点（_obstacleCheckRange = 0.5 沿朝向方向）
   ┃
   ┣━━ 0 格高（脚下）：墙体 → 触发跨越（如下文检查头顶净空）
   ┃
   ┗━━ 1 格高（头顶）：墙体 → 不能跨越，停下转身（或单纯停顿，避免抽搐）
```

具体公式（伪代码）：
```csharp
Vector2 origin = (Vector2)transform.position + Vector2.right * direction * 0.5f;
bool blockedAtFeet = Physics2D.OverlapCircle(origin, 0.15f, _groundLayer);
bool blockedAtHead = Physics2D.OverlapCircle(origin + Vector2.up * 1f, 0.15f, _groundLayer);
if (blockedAtFeet && !blockedAtHead && _isGrounded) Hop();   // 跨 1 格台阶
else if (blockedAtFeet && blockedAtHead) /* 卡死，停一会儿，朝向再次更新 */;
```

### 接触伤害（同 Enemy 基类）
不重写 `OnCollisionStay2D` —— 基类已经处理。仅调高 `_contactDamage = 15`。

### 死亡掉落
新增 `Item_RottenFlesh`（Material，icon 暂复用 zombie sprite 或留空），死亡掉 1 个。

### 夜间生成 hook（给 024 用）
`EnemySpawnEntry` 新增 1 个字段：

```csharp
[System.Serializable]
public sealed class EnemySpawnEntry
{
    public GameObject Prefab;
    public float Weight = 1f;
    public TimeOfDayMask AllowedTimes = TimeOfDayMask.All;   // 新增；024 之前默认 All
}

[System.Flags]
public enum TimeOfDayMask : byte
{
    None = 0,
    Morning = 1 << 0,
    Noon = 1 << 1,
    Afternoon = 1 << 2,
    Evening = 1 << 3,
    DeepNight = 1 << 4,
    All = Morning | Noon | Afternoon | Evening | DeepNight,
}
```

`EnemySpawner` **暂时不读这个字段**（因为 WorldClock 还没出现），但配置上：
- Slime entry → AllowedTimes = All
- Zombie entry → AllowedTimes = Evening | DeepNight   ← 数据已经准备好

024 上线时只需要在 `EnemySpawner.TryPickEntry` 里加一行 `if (!entry.AllowedTimes.HasFlag(currentTime)) continue;`，
不需要再改 022 的代码。

> 这是任务规范允许的"为已知近期需求保留 1 个字段"，不算过度设计 —— 因为 024 已确定要做且字段就是它要用的。

## 接口签名

```csharp
// === Enemies/Zombie.cs ===
public sealed class Zombie : Enemy
{
    [Header("Zombie Walk")]
    [SerializeField] private float _walkSpeed = 2.5f;
    [SerializeField] private float _stepUpVerticalImpulse = 4f;
    [SerializeField] private float _obstacleProbeDistance = 0.5f;
    [SerializeField] private float _stuckRecoverDelay = 1f;

    private float _stuckTimer;

    protected override void UpdateBehavior()
    {
        if (_state != EnemyState.Chasing) return;
        if (_playerTransform == null) return;

        float direction = Mathf.Sign(_playerTransform.position.x - transform.position.x);
        Vector2 origin = (Vector2)transform.position + Vector2.right * direction * _obstacleProbeDistance;
        bool blockedFeet = Physics2D.OverlapCircle(origin, 0.15f, _groundLayer);
        bool blockedHead = Physics2D.OverlapCircle(origin + Vector2.up * 1f, 0.15f, _groundLayer);

        if (blockedFeet && blockedHead)
        {
            // 卡墙：停顿 _stuckRecoverDelay，期间 velocity = 0
            _stuckTimer += Time.deltaTime;
            _rigidbody2D.linearVelocity = new Vector2(0f, _rigidbody2D.linearVelocity.y);
            return;
        }

        _stuckTimer = 0f;
        _rigidbody2D.linearVelocity = new Vector2(direction * _walkSpeed, _rigidbody2D.linearVelocity.y);

        if (blockedFeet && !blockedHead && _isGrounded)
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, _stepUpVerticalImpulse);
        }
    }
}

// === Enemies/EnemySpawnEntry.cs === 修改
public sealed class EnemySpawnEntry
{
    public GameObject Prefab;
    public float Weight = 1f;
    public TimeOfDayMask AllowedTimes = TimeOfDayMask.All;   // 新增
}

// === Enemies/TimeOfDayMask.cs === 新增（024 也会用到）
[System.Flags]
public enum TimeOfDayMask : byte { ... }
```

## 依赖
- 任务 012 Enemy（基类，新建子类）
- 任务 014 EnemySpawner（扩展 SpawnEntry 字段；本任务只加字段，不写过滤逻辑）
- 任务 011 ItemDrop（死亡掉落复用）

## 文件清单

### 新增
- `Assets/Scripts/Enemies/Zombie.cs`
- `Assets/Scripts/Enemies/TimeOfDayMask.cs`
- `Assets/Prefabs/Enemies/Zombie.prefab`（MCP 创建）
- `Assets/Data/Items/Item_RottenFlesh.asset`

### 修改
- `Assets/Scripts/Enemies/EnemySpawnEntry.cs` — 新增 `AllowedTimes` 字段
- `Assets/Data/Enemies/EnemySpawnerConfig.asset` — 表里追加 Zombie entry，Slime/Zombie 各自填好 AllowedTimes（虽然 022 阶段不读）
- `Assets/Data/ItemDatabase.asset` — 注册 Item_RottenFlesh

### 美术资源（用户提供）
僵尸贴图 **暂时使用占位**：用紫色 Slime 的 sprite 临时复用，scale 0.45 + 改 SpriteRenderer.color 为暗绿色 (0.4, 0.7, 0.3)，作为视觉占位。
**真实僵尸贴图由用户后续找资源放进 `Assets/Art/Sprites/Enemies/zombie_*.png`**，找到后开个独立小任务（022.1）替换。Codex 不要尝试自己画。

> Codex 实现完成后**主动提醒用户找贴图** —— 不要自己生成 / AI 画。

## 验收标准

### 行为
- [ ] 玩家进入 12 米检测范围 → 僵尸开始走向玩家
- [ ] 玩家在僵尸路径上放 1 格高的 Stone → 僵尸跨过去
- [ ] 玩家在僵尸路径上放 2 格高的 Stone → 僵尸停下不抖动
- [ ] 僵尸走到悬崖边继续走出去自由下落，不会预判停下
- [ ] 玩家爬到僵尸不可达的高台上 → 僵尸不无脑乱跳

### 数值
- [ ] 默认 50 HP，10 击（每次 5 伤）才能击杀骑士剑普通攻击
- [ ] 接触玩家造成 15 伤（PlayerHealth 实测）
- [ ] 击杀掉落 1 个 Item_RottenFlesh

### 生成
- [ ] EnemySpawnerConfig 包含 Zombie entry，Weight=1
- [ ] 当前阶段（024 之前）僵尸全天可生成，Slime / Zombie 比例约 1:1
- [ ] AliveCount 上限正常，僵尸 + 史莱姆共享 8 只全局上限

### 综合
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] MCP 截图：僵尸跨台阶 + 玩家与僵尸近战交战
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项

### 僵尸贴图占位说明
本任务**不要求美术资源** —— 占位 sprite 即可。用户会后续提供真实贴图，到时候 022.1 替换。
Codex 在交付记录里**明确写明"等待用户提供 zombie 贴图"**。

### TimeOfDayMask 枚举位置
放在 `Enemies/` 目录还是 `World/Time/`（024 用）？— **放在 `Enemies/`**，因为 022 先用。
024 创建 `World/Time/WorldClock.cs` 时可以引用同一个枚举（共用 namespace 或加 using）。

### 卡墙抖动的处理
单纯设 `velocity.x = 0` 不够 —— 朝向（flipX）每帧重算可能在卡墙时反复翻转。
基类 `UpdateFacing` 用 `velocity.x` 判定，velocity=0 时不翻转（已经判过 epsilon），所以本任务不动基类，只需要在卡墙时确实把 velocity 归 0。

### 不要重写 Die / SpawnDrop
基类逻辑已正确，僵尸只配置 `_dropItem = Item_RottenFlesh, _dropCount = 1` 即可。

### 不做的事
- **不做夜间限定生成** —— 留给 024，本任务只加字段
- **不做僵尸抓取 / 啃咬动画** —— 占位 sprite 阶段不做
- **不做不同变种**（沼泽僵尸 / 雪地僵尸） —— 等生态群落系统
- **不做僵尸群集行为** —— 单体 AI 即可
- **不做僵尸破坏方块**（Terraria 派生玩法）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/022-enemy-zombie.md`
写一份交付记录。

**交付记录额外要求**：
1. **MCP 截图**：僵尸追击 / 跨台阶 / 击杀掉落
2. **数值实测**：骑士剑普通攻击击杀僵尸需要的次数
3. **占位说明**：明确写"zombie sprite 待用户提供，当前为占位"
