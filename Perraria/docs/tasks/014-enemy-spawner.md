# 任务 014 - 敌人生成器（EnemySpawner）

## 目标
围绕玩家自动生成史莱姆：按时间间隔、以玩家为中心、在镜头外的地表上刷怪，尊重并发上限。
玩家走到哪，敌人就在附近刷新，形成持续的战斗循环。替代手动摆放和 `EnemyDebugInput`。

## 接口签名

```csharp
// 1) 单个敌人条目（支持未来多种敌人，用权重抽取）
[System.Serializable]
public sealed class EnemySpawnEntry
{
    public GameObject Prefab;       // 必须带 Enemy 组件
    public float Weight = 1f;       // 加权随机
}

// 2) 配置资源（ScriptableObject，数值调优在资源里）
[CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "Perraria/Enemy Spawner Config")]
public sealed class EnemySpawnerConfig : ScriptableObject
{
    public float SpawnInterval = 3f;        // 两次尝试间隔（秒）
    public int MaxConcurrent = 8;           // 全局并发上限
    public float MinSpawnDistance = 14f;    // 相对玩家最小水平距离
    public float MaxSpawnDistance = 22f;    // 最大水平距离
    public float SkyRaycastHeight = 20f;    // 从玩家上方多少米向下发射地表射线
    public float RaycastDistance = 60f;     // 射线最大距离
    public LayerMask GroundLayer;           // 地面层
    public List<EnemySpawnEntry> SpawnTable;
}

// 3) 生成器本体（挂到场景空物体上）
[DisallowMultipleComponent]
public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnerConfig _config;
    [SerializeField] private Camera _mainCamera;    // 用于镜头外判断
    [SerializeField] private Transform _playerTransform;  // 若空则运行时 FindWithTag("Player")

    public int AliveCount { get; }
    public void TriggerSpawnAttempt();  // 调试/外部触发用
}
```

## 依赖
- 任务 012 Enemy（`OnDied` 事件，用于维护 AliveCount）
- 任务 003 WorldGenerator / TileManager（已经把地面放在 `Ground` Layer，可用 Physics2D.Raycast 命中）

## 文件清单
- `Assets/Scripts/Enemies/EnemySpawnEntry.cs` — 新增
- `Assets/Scripts/Enemies/EnemySpawnerConfig.cs` — 新增（ScriptableObject）
- `Assets/Scripts/Enemies/EnemySpawner.cs` — 新增
- `Assets/Data/Enemies/EnemySpawnerConfig.asset` — **通过 MCP 创建**：
  - SpawnTable 单条目：Slime.prefab，Weight=1
  - GroundLayer 选中 `Ground`
  - 其他字段用默认值
- 场景配置（**通过 MCP 操作**）：
  - 新增空物体 `EnemySpawner` 挂 `EnemySpawner` 组件，绑定 config + 相机
  - **移除**场景内预放的 2 只手摆史莱姆（由生成器接管）
  - **移除**玩家身上的 `EnemyDebugInput` 组件（任务 013 已确认"014 出来后清理"）
- `Assets/Scripts/Debug/EnemyDebugInput.cs` + `.meta` — 删除整个脚本和 Debug 目录（如目录为空）

## 验收标准
- [ ] 空场景启动后 3 秒内出现第 1 只史莱姆（或按 SpawnInterval）
- [ ] 史莱姆数量稳定维持在 `MaxConcurrent`（默认 8）以下，不超刷
- [ ] 生成点永远在当前相机视口之外（屏幕内不突然冒出敌人）
- [ ] 生成点落在实心方块的正上方一格（通过 Raycast 命中地面）
- [ ] 生成点与玩家的水平距离 ∈ [MinSpawnDistance, MaxSpawnDistance]（默认 14~22）
- [ ] 史莱姆被玩家击杀后 `AliveCount` 正确减 1，后续可以补刷
- [ ] SpawnTable 为空或找不到合法生成点时，本次尝试直接跳过，不报错
- [ ] 禁用 `EnemySpawner` 组件后停止生成；重新启用后恢复

## 注意事项
- **镜头外判断**：`Vector3 viewport = _mainCamera.WorldToViewportPoint(candidatePos); bool inView = viewport.x ∈ [0,1] && viewport.y ∈ [0,1] && viewport.z > 0`；只在 `!inView` 时允许生成
- **寻找地表**：
  1. 随机选 `sign ∈ {-1, +1}` 和 `distance ∈ [Min, Max]`，候选 X = `player.x + sign * distance`
  2. 从 `(candidateX, player.y + SkyRaycastHeight)` 向下 Raycast `RaycastDistance`，命中 GroundLayer 的最上方瓦片
  3. 命中点 y + 0.5（半格偏移，避免生成在瓦片内部卡住），x 用 `Mathf.Floor(candidateX) + 0.5f` 对齐瓦片中心
  4. 最多重试 N 次（建议 6 次），全部失败就跳过本轮
- **AliveCount 维护**：生成时把 Enemy 引用加到 HashSet，订阅 `OnDied` 回调从 Set 里移除；Set 容量 = AliveCount
- **不要用 FindObjectsOfType 每帧遍历敌人数量**——通过事件订阅维护本地计数
- **生成的史莱姆从 SpawnTable 权重抽取**：累加权重随机一个条目，即使当前表里只有 1 条也走抽取逻辑（为后续多敌人铺路）
- **Physics2D.Raycast 性能**：单次尝试仅 1 次 Raycast，每 3 秒 1 次，可忽略
- **玩家引用**：Awake 时若 `_playerTransform` 为空，`FindWithTag("Player")` 缓存；如果玩家死亡被销毁（未来）需要处理——本任务先不做
- **不做的事**：
  - 生物群系/时段差异化生成（未来接 GameManager 再扩）
  - 敌人去重（远离玩家的敌人不回收）
  - 首次生成前的预热 spawn burst

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/014-enemy-spawner.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
