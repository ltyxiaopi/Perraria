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

### 美术资源（已就位 — ThePixelGame 16x16 Platformer Zombie）

#### 现状
用户已将素材下载到 `Assets/Art/Zombie/`，原始文件名带空格 + 括号 + Left/Right 双向：
```
16x16 Platformer Zombie Attack Left.png        5120x1024  (5 frames × 1024×1024)
16x16 Platformer Zombie Attack Right.png       5120x1024
16x16 Platformer Zombie Death Left.png         2048x512   (4 frames × 512×512)
16x16 Platformer Zombie Death Right.png        2048x512
16x16 Platformer Zombie Encounter Left.png     2560x512   (5 frames × 512×512)
16x16 Platformer Zombie Encounter Right.png    2560x512
16x16 Platformer Zombie Hurt Left.png          1536x512   (3 frames × 512×512)
16x16 Platformer Zombie Hurt Right.png         1536x512
16x16 Platformer Zombie Idle Left.png          2048x512   (4 frames × 512×512)
16x16 Platformer Zombie Idle Right.png         2048x512
16x16 Platformer Zombie Move Left.png          2048x512   (4 frames × 512×512)
16x16 Platformer Zombie Move Right.png         2048x512
```

> **风格说明**：素材原生 16×16 像素设计，导出时按 32× 缩放（每帧画布 512×512，Attack 1024×1024 是为了容纳挥砍特效的水平扩展）。块状像素 + 黑描边，跟现有 Slime 风格统一。

#### Codex 需要做的资源处理（**实现 022 时一并完成**）

1. **重命名文件夹**：`Assets/Art/Zombie/` → `Assets/Art/Zombies/`（复数，跟 `Art/Slimes/` 对齐）

2. **删除 6 个 Left 版本**：基类 `Enemy.UpdateFacing` 已用 `SpriteRenderer.flipX` 翻转，Left 完全冗余

3. **重命名保留的 6 个 Right 版本**为 snake_case：
   | 旧文件名 | 新文件名 | 帧数 / 每帧尺寸 |
   |---|---|---|
   | `16x16 Platformer Zombie Idle Right.png` | `zombie_idle.png` | 4 × 512×512 |
   | `16x16 Platformer Zombie Move Right.png` | `zombie_move.png` | 4 × 512×512 |
   | `16x16 Platformer Zombie Attack Right.png` | `zombie_attack.png` | 5 × 1024×1024 |
   | `16x16 Platformer Zombie Hurt Right.png` | `zombie_hurt.png` | 3 × 512×512 |
   | `16x16 Platformer Zombie Death Right.png` | `zombie_death.png` | 4 × 512×512 |
   | `16x16 Platformer Zombie Encounter Right.png` | `zombie_encounter.png` | 5 × 512×512 |

4. **Sprite 导入设置**（每个 PNG 用相同设置 — 通过 MCP 改 .meta 或 TextureImporter）：
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: **Multiple**
   - Pixels Per Unit: **由 Codex 在 Unity 里调，命中下文"在世界中尺寸"目标即可**（参考起点：PPU=256，对应 1 帧 = 2 单位）
   - Filter Mode: **Point (no filter)** — 像素艺术必须
   - Compression: **None**
   - Generate Mip Maps: 关闭
   - 切片：Sprite Editor → Slice → Grid By Cell Size
     - Idle/Move/Hurt/Death/Encounter：Cell Size 512×512
     - Attack：Cell Size 1024×1024
     - Pivot: Center
   - 切片完成后逐帧执行 **Trim**（去掉透明 padding），避免每帧 sprite 包含大量空白导致碰撞 / 视觉中心错位

5. **prefab 静态 sprite**：用 `zombie_idle_0`（slicing 后第一帧）作为 `SpriteRenderer.sprite`，**本任务不上 Animator**（留给 028）

#### 在世界中尺寸目标

| 单位 | Slime | **Zombie 目标** | Player |
|---|---|---|---|
| 高度 | ~1.35 单位 | **~1.5 单位** | ~1.8 单位 |
| 宽度 | ~1.5 单位 | **~0.7 单位** | ~0.6 单位 |

Codex 在 Unity 里通过 PPU + prefab transform.scale 命中这个体型，截图验收。spec 早期写的 `scale 0.45` 是占位估算值，**以实际命中目标体型为准**。

#### 其余 5 套动画（attack / hurt / death / move / encounter）

本任务**只导入 + 切片**，prefab 不引用。留给 028（Animator 系统）统一接入 Slime + Zombie + Player 的动画状态机。

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

### 美术资源已就位
ThePixelGame 16×16 Platformer Zombie 套件已下载到 `Assets/Art/Zombie/`（详见上方"美术资源"段）。
**不再需要占位 sprite**，也不再产生 022.1 替换任务。Codex 实现 022 时一并完成文件夹重命名 / 文件清理 / 切片导入 / prefab 配置。

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
- **不做 Animator 状态机** —— 留给 028 统一给所有敌人 + 玩家上动画；022 prefab 仅用 `zombie_idle_0` 静态帧
- **不做僵尸抓取 / 啃咬动画** —— 同上，留给 028
- **不做不同变种**（沼泽僵尸 / 雪地僵尸） —— 等生态群落系统
- **不做僵尸群集行为** —— 单体 AI 即可
- **不做僵尸破坏方块**（Terraria 派生玩法）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/022-enemy-zombie.md`
写一份交付记录。

**交付记录额外要求**：
1. **MCP 截图**：僵尸追击 / 跨台阶 / 击杀掉落（用真实僵尸贴图，非占位）—— 只能截场景 / 相机视图，不能截 Editor 面板
2. **数值实测**：骑士剑普通攻击击杀僵尸需要的次数
3. **资源处理记录**（纯文字 + 必要时用 `Unity_RunCommand` 把数值打到 Console 截日志）：
   - 已重命名的 6 个文件（旧名 → 新名）
   - 已删除的 6 个 Left 文件
   - 最终 PPU 值（每个 PNG 的 `TextureImporter.spritePixelsToUnits`）
   - prefab `transform.localScale`、`SpriteRenderer.sprite` 引用名
   - 实测在世界中体型（宽 × 高，单位 = Unity units）—— 用 `Unity_RunCommand` 取 `BoxCollider2D.size * transform.localScale` 打 Log
3. **占位说明**：明确写"zombie sprite 待用户提供，当前为占位"
