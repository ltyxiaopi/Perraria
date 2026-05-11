# 任务 024 - 昼夜循环系统

## 目标
1. 引入游戏内时间：5 个时段（Morning / Noon / Afternoon / Evening / DeepNight）循环。
2. 全局 Light 2D 强度 / 颜色随时段渐变（明亮的午、橙红的晚、深蓝的夜）。
3. 背景图随时段切换（晨曦、白昼、晚霞、星夜 4 张占位）。
4. 接入 022 已经预留的 `EnemySpawnEntry.AllowedTimes`：僵尸仅 Evening + DeepNight 生成，史莱姆全天。
5. 暂停菜单 / 存档系统正确处理时间（暂停冻结、存档保留时间进度）。

## 设计概要

### 时间模型：游戏内分钟数 / 一日总长
1 个游戏日 = **24 分钟现实时间**（Terraria 标准）。游戏内时间从 0:00 → 23:59 循环。
- Morning: 06:00 – 10:00（游戏内 4 小时 = 现实 4 分钟）
- Noon: 10:00 – 14:00
- Afternoon: 14:00 – 18:00
- Evening: 18:00 – 22:00
- DeepNight: 22:00 – 02:00（跨日，覆盖 0:00）+ 02:00 – 06:00 也算 DeepNight

实际枚举上 5 段，其中 DeepNight 占 8 小时（02:00-06:00 + 22:00-02:00），符合"深夜更长"的玩法直觉。

### 数据结构
```csharp
// === World/Time/TimeOfDay.cs === （注意：022 的 TimeOfDayMask 复用同一概念）
public enum TimeOfDay : byte
{
    Morning = 0,
    Noon = 1,
    Afternoon = 2,
    Evening = 3,
    DeepNight = 4,
}

// === World/Time/WorldClock.cs ===
public sealed class WorldClock : MonoBehaviour
{
    public const float DayLengthSeconds = 24f * 60f;   // 24 真实分钟 = 1440 秒

    public static WorldClock Instance { get; private set; }

    [SerializeField] private float _gameMinutesPerSecond = 1f;   // 1 游戏分钟 / 1 真实秒（24 分钟一日）

    public float CurrentGameMinutes { get; private set; }   // 0 ~ 1440
    public TimeOfDay CurrentTime { get; private set; }
    public event Action<TimeOfDay, TimeOfDay> OnTimeOfDayChanged;   // (prev, next)

    public void SetTime(float minutes);   // 调试用 / 读档用
    private void Update();   // 仅 Time.timeScale > 0 时累加
}
```

### 全局光照
URP 2D 有 `Light2D` 组件。新建一个 `Global Light 2D` GameObject，由 `WorldLightingDirector` 控制：

```csharp
// === World/Time/WorldLightingDirector.cs ===
public sealed class WorldLightingDirector : MonoBehaviour
{
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D _globalLight;
    [SerializeField] private LightingPreset[] _presets;   // 5 个时段每个 1 个

    [System.Serializable]
    public struct LightingPreset
    {
        public TimeOfDay Time;
        public Color Color;
        public float Intensity;
    }

    private void Update()   // 每帧从 WorldClock.CurrentGameMinutes 插值
    {
        // 找到当前 minutes 落在哪两个预设之间，Lerp Color + Intensity
    }
}
```

**预设值参考**：
| 时段 | 起始 | Color | Intensity |
|---|---|---|---|
| Morning | 06:00 | (1.0, 0.95, 0.85) | 0.85 |
| Noon | 10:00 | (1.0, 1.0, 0.95) | 1.0 |
| Afternoon | 14:00 | (1.0, 0.9, 0.8) | 0.95 |
| Evening | 18:00 | (1.0, 0.6, 0.4) | 0.7 |
| DeepNight | 22:00 / 02:00 | (0.3, 0.4, 0.7) | 0.35 |

### 背景图切换
`BackgroundController`（挂在相机或天空层）持有 4 张背景 Sprite（Morning / Noon+Afternoon / Evening / DeepNight），
在时段切换时**淡入淡出**到目标图（持续 5 秒）。

```csharp
// === World/Time/BackgroundController.cs ===
public sealed class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _backgroundA;
    [SerializeField] private SpriteRenderer _backgroundB;   // 双层用于淡入淡出
    [SerializeField] private BackgroundEntry[] _entries;
    [SerializeField] private float _crossfadeSeconds = 5f;

    [System.Serializable]
    public struct BackgroundEntry
    {
        public TimeOfDay Time;
        public Sprite Background;
    }

    // 订阅 WorldClock.OnTimeOfDayChanged → 启动 crossfade 协程
}
```

**美术资源占位**：用户后续提供 4 张背景图。当前用纯色 sprite 占位（256x256 单色）：
- Morning: 浅橙色
- Noon/Afternoon: 浅蓝色
- Evening: 橙红色
- DeepNight: 深蓝色

### EnemySpawner 接入夜间生成
当前 `EnemySpawner.TryPickEntry` 加权随机抽取，本任务在加权前过滤：

```csharp
List<EnemySpawnEntry> candidates = new();
TimeOfDay currentTime = WorldClock.Instance != null ? WorldClock.Instance.CurrentTime : TimeOfDay.Noon;
TimeOfDayMask currentMask = TimeOfDayMaskFromTimeOfDay(currentTime);
foreach (var entry in _config.SpawnTable)
{
    if ((entry.AllowedTimes & currentMask) == 0) continue;
    candidates.Add(entry);
}
// 在 candidates 上加权随机
```

**Slime 配置**：AllowedTimes = All  
**Zombie 配置**：AllowedTimes = Evening | DeepNight  
（022 阶段已经填好这些 mask，本任务只需要让 EnemySpawner 真正读取）

### 暂停菜单
`PauseMenu` 设 `Time.timeScale = 0` 时 WorldClock.Update 不累加 → 时间冻结。**不需要额外改动**。

### 存档接入
`SaveData` 新增 `TimeSaveData`：

```csharp
[System.Serializable]
public sealed class TimeSaveData
{
    public float GameMinutes;
}
```

`SaveData` 根新增字段，`GameStateSnapshot.Capture/Apply` 加分支调用 `WorldClock.CurrentGameMinutes / SetTime`。
新建存档默认时间 = 360（06:00 Morning）。

## 接口签名

```csharp
// === World/Time/TimeOfDay.cs === 已在 022 创建 TimeOfDayMask
// 本任务新增 TimeOfDay 枚举（注意命名不冲突：Mask = flags，TimeOfDay = 单值）

// === World/Time/WorldClock.cs ===
// 见设计概要

// === World/Time/WorldLightingDirector.cs ===
// 见设计概要

// === World/Time/BackgroundController.cs ===
// 见设计概要

// === Enemies/EnemySpawner.cs === 修改
// TryPickEntry 加 AllowedTimes 过滤
private static TimeOfDayMask MaskFromTimeOfDay(TimeOfDay t) => t switch
{
    TimeOfDay.Morning => TimeOfDayMask.Morning,
    TimeOfDay.Noon => TimeOfDayMask.Noon,
    TimeOfDay.Afternoon => TimeOfDayMask.Afternoon,
    TimeOfDay.Evening => TimeOfDayMask.Evening,
    TimeOfDay.DeepNight => TimeOfDayMask.DeepNight,
    _ => TimeOfDayMask.None,
};

// === Save/SaveData.cs === 新增 TimeSaveData
public TimeSaveData Time;

// === Save/GameStateSnapshot.cs === 修改
// Capture/Apply 加 Time 分支
```

## 依赖
- 任务 022 EnemySpawner（AllowedTimes 字段已加，本任务接通过滤）
- 任务 014 EnemySpawner（基础架构）
- 任务 017 PauseMenu（Time.timeScale = 0 自动暂停时钟）
- 任务 018 SaveData（扩展 TimeSaveData）
- URP 2D Renderer（Light2D 组件）

## 文件清单

### 新增
- `Assets/Scripts/World/Time/TimeOfDay.cs`
- `Assets/Scripts/World/Time/WorldClock.cs`
- `Assets/Scripts/World/Time/WorldLightingDirector.cs`
- `Assets/Scripts/World/Time/BackgroundController.cs`
- `Assets/Scripts/Save/TimeSaveData.cs`

### 修改
- `Assets/Scripts/Enemies/EnemySpawner.cs` — TryPickEntry 加时段过滤
- `Assets/Scripts/Save/SaveData.cs` — 新增 Time 字段
- `Assets/Scripts/Save/GameStateSnapshot.cs` — Capture/Apply 加 Time 分支
- `Assets/Scripts/Save/SaveData.cs` 的 `CreateNewGameDefault` — Time = 360

### 美术（占位）
- `Assets/Art/Backgrounds/bg_morning.png`（256x256 浅橙）
- `Assets/Art/Backgrounds/bg_day.png`（256x256 浅蓝）
- `Assets/Art/Backgrounds/bg_evening.png`（256x256 橙红）
- `Assets/Art/Backgrounds/bg_night.png`（256x256 深蓝）

> **真实背景图由用户后续提供** —— Codex 实现后提醒用户。

### 场景配置（MCP）
- `SampleScene` 新增空物体 `WorldClock` 挂 `WorldClock` 组件
- `SampleScene` 新增 `Global Light 2D`，挂 `WorldLightingDirector`，预设值按设计概要表填
- `SampleScene` 新增 `BackgroundController` 挂在相机子物体（深度排序最深）
- `EnemySpawnerConfig.asset` 的 Slime / Zombie entry 重新检查 AllowedTimes 字段值（022 已填好）

## 验收标准

### 时间流逝
- [ ] 进入游戏后时间从 06:00 开始（默认）
- [ ] 24 真实分钟后回到 06:00（一日循环）
- [ ] 暂停菜单（Esc）打开后时间冻结，关闭后恢复
- [ ] WorldClock.CurrentTime 在 5 个时段间正确切换
- [ ] OnTimeOfDayChanged 事件触发顺序正确

### 光照
- [ ] Morning 偏暖（intensity 0.85）
- [ ] Noon 最亮（1.0）
- [ ] Evening 橙红（0.7）
- [ ] DeepNight 最暗（0.35），看清玩家但环境压抑
- [ ] 时段切换时光照**平滑过渡**（不是瞬切）

### 背景
- [ ] 4 张背景图按时段切换
- [ ] 切换时 5 秒淡入淡出，不闪屏
- [ ] 暂停时不切换（Time.timeScale=0 → 协程不推进）

### 敌人生成
- [ ] Evening + DeepNight 期间僵尸出现
- [ ] Morning + Noon + Afternoon 期间不出现僵尸（只刷史莱姆）
- [ ] 时段切换瞬间已存在的僵尸**不会自动消失**（只影响新生成）

### 存档
- [ ] 保存时记录当前游戏分钟数
- [ ] 读档后从存的时间继续
- [ ] 新建存档时间 = 360（Morning 起点）

### 综合
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] MCP 截图：5 张时段截图证明光照 / 背景视觉差异
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项

### WorldClock 单例
WorldClock 用 `static Instance` 是项目允许的轻量单例（项目还没有 GameManager）。Awake 里 `Instance = this`，OnDestroy 里清空。**不要做 DontDestroyOnLoad** —— 主菜单场景不需要时钟。
SampleScene 自己挂一个就行。

### 时间速度可调
`_gameMinutesPerSecond` 暴露在 Inspector，调试时可临时改成 60（一秒一小时）观察光照过渡。

### 光照过渡的插值
光照预设有 5 个时间点。Update 里：
1. 根据 `CurrentGameMinutes` 找到当前所在的两个预设区间（如 Noon → Afternoon）
2. 算出区间内的 t 值（0~1）
3. `Color.Lerp(presetA.Color, presetB.Color, t)` 和 `Mathf.Lerp(presetA.Intensity, presetB.Intensity, t)`
4. 写入 `_globalLight.color` 和 `_globalLight.intensity`

DeepNight 跨日要小心：22:00 → 02:00 → 06:00。建议把 5 个预设展开成 6 个锚点（22:00 DeepNight, 06:00 Morning, 10:00 Noon, 14:00 Afternoon, 18:00 Evening, 22:00 DeepNight），首尾一致。

### 背景图尺寸
占位用 256×256 即可，美术任务时替换为视差滚动多层背景（独立任务）。当前 BackgroundController 只支持单层。

### 不做的事
- **不做月相 / 季节** —— 留给后续扩展
- **不做天气**（雨 / 雪 / 沙暴） —— 独立天气系统任务
- **不做声音切换**（白天虫鸣 / 夜晚狼嚎） —— 等音频系统
- **不做特定时段才能合成的物品** —— 留给合成系统决定
- **不做强制睡觉跳过夜晚** —— 床合成出来后再做

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/024-day-night-cycle.md`
写一份交付记录。

**交付记录额外要求**：
1. **MCP 5 张截图**：5 个时段各 1 张
2. **僵尸时段过滤实测**：用调试加速时间，记录 Morning 是否真的不刷僵尸 / Evening 是否开始刷
3. **存档往返**：保存时记录 minutes，读档后验证恢复
4. **占位说明**：背景图为占位，等待用户提供
