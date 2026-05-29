# 任务 025 - 昼夜循环系统

## 目标
1. 引入游戏内时间：5 个时段（Morning / Noon / Afternoon / Evening / DeepNight）循环。
2. 全局 Light 2D 强度 / 颜色随时段渐变（明亮的午、橙红的晚、深蓝的夜）。
3. **代码渐变天空**：全屏天空层，顶/底两色随时段在锚点间平滑 `Color.Lerp`（晨橙→午蓝→暮红→夜深蓝），切换无闪屏。
4. **三层视差云**：玩家移动时云以远小于角色的幅度横向滚动（视差系数 0.08~0.25），无缝循环，云色随时段 tint 同步渐变。
5. **夜空（星空 + 月亮）**：深夜星空图淡入、月亮显现；白天/黄昏淡出隐藏。
6. 接入 022 已经预留的 `EnemySpawnEntry.AllowedTimes`：僵尸仅 Evening + DeepNight 生成，史莱姆全天。
7. 暂停菜单 / 存档系统正确处理时间（暂停冻结、存档保留时间进度）。

## 美术资源前置检查
- **是否需要新素材**：是 —— 已完成搜索 + 用户下载 + Claude 看图确认 + 目录规整。
- **素材来源**：均来自 Free Game Assets / CraftPix（royalty-free，可商用，同一作者，风格统一）。
  - 云：[Free Pixel Sky with Parallax Clouds](https://craftpix.net/freebies/free-pixel-sky-with-parallax-clouds-for-2d-games/)
  - 夜空：[Free Moon Pixel Game Backgrounds](https://craftpix.net/freebies/free-moon-pixel-game-backgrounds/)
- **已规整落位**（`Assets/Art/Backgrounds/`）：
  ```
  clouds/        cloud_far.png  cloud_mid.png  cloud_near.png   ← 本任务用：三层透明白云
  night/         starfield.png  moon.png                        ← 本任务用：星空底图 + 透明月亮
  weather_clouds/  sunset/ azure/ overcast/                     ← 不动，预留未来天气系统
  ```
- **素材规格（已用 alpha 探针确认）**：
  | 文件 | 尺寸 | 透明 | 内容 |
  |---|---|---|---|
  | `clouds/cloud_far.png` | 576×324 | ✅透明 | 高空稀疏小云（最远、最淡）|
  | `clouds/cloud_mid.png` | 576×324 | ✅透明 | 中景云丘 |
  | `clouds/cloud_near.png` | 576×324 | ✅透明 | 近景大云团（最近、最亮）|
  | `night/starfield.png` | 576×324 | ❌不透明 | 深紫夜空 + 星星（星点嵌在底图里，无法单独抠出）|
  | `night/moon.png` | 576×324 | ✅透明 | 居中白色满月 |
- **关键限制**：`starfield.png` 的星星与深紫底**绑定不可分离**，所以星空作为**整张图层在深夜淡入**（而非独立透明星点层叠加）。

## 设计概要

### 渲染层次（从远到近，sortingOrder 由低到高，均在游戏世界之下）
```
1. SkyGradient 底色   _skyFill   (全屏纯色, 颜色随时段 Lerp)
2. SkyGradient 顶渐变 _skyTop    (顶部垂直渐变叠加, 颜色随时段 Lerp)
3. 星空层 starfield              (深夜 alpha 淡入; 视差≈0 钉相机)
4. 月亮层 moon                   (深夜 alpha 淡入; 视差≈0 钉相机, 偏上)
5. 远云 cloud_far                (视差 0.08, tint 随时段)
6. 中云 cloud_mid                (视差 0.15, tint 随时段)
7. 近云 cloud_near               (视差 0.25, tint 随时段)
─────────────────────────────────
   游戏世界 (角色/地形, 视差 1.0)
```

### 时间模型：游戏内分钟数 / 一日总长
1 个游戏日 = **24 分钟现实时间**（Terraria 标准）。游戏内时间从 0:00 → 23:59 循环。
- Morning: 06:00 – 10:00（游戏内 4 小时 = 现实 4 分钟）
- Noon: 10:00 – 14:00
- Afternoon: 14:00 – 18:00
- Evening: 18:00 – 22:00
- DeepNight: 22:00 – 02:00（跨日，覆盖 0:00）+ 02:00 – 06:00 也算 DeepNight

实际枚举上 5 段，其中 DeepNight 占 8 小时，符合"深夜更长"的玩法直觉。

### 数据结构
```csharp
// === World/Time/TimeOfDay.cs === （注意：022 的 TimeOfDayMask 复用同一概念）
public enum TimeOfDay : byte
{
    Morning = 0, Noon = 1, Afternoon = 2, Evening = 3, DeepNight = 4,
}

// === World/Time/WorldClock.cs ===
public sealed class WorldClock : MonoBehaviour
{
    public const float DayLengthSeconds = 24f * 60f;   // 24 真实分钟 = 1440 秒
    public static WorldClock Instance { get; private set; }
    [SerializeField] private float _gameMinutesPerSecond = 1f;   // 1 游戏分钟 / 1 真实秒

    public float CurrentGameMinutes { get; private set; }   // 0 ~ 1440
    public TimeOfDay CurrentTime { get; private set; }
    public event Action<TimeOfDay, TimeOfDay> OnTimeOfDayChanged;   // (prev, next)

    public void SetTime(float minutes);   // 调试用 / 读档用
    private void Update();   // 仅 Time.timeScale > 0 时累加
}
```

### 渐变采样工具（天空/光照/云 tint/夜空 alpha 共用）
```csharp
// === World/Time/GradientSampler.cs ===
// 把"按 AtMinutes 升序锚点 + 当前 minutes → 区间内 Lerp"逻辑抽成静态工具，
// 供 SkyGradient / WorldLightingDirector / 云 tint / 夜空 alpha 复用，避免四处重复。
// 必须正确处理跨日环绕（最后一个锚点 → 第一个锚点 + 1440）。
public static class GradientSampler
{
    public static Color SampleColor(IReadOnlyList<float> atMinutes, IReadOnlyList<Color> values, float minutes);
    public static float SampleFloat(IReadOnlyList<float> atMinutes, IReadOnlyList<float> values, float minutes);
}
```

### 全局光照
URP 2D `Light2D`，由 `WorldLightingDirector` 每帧按 `WorldClock.CurrentGameMinutes` 采样锚点 Lerp 出 Color/Intensity。

```csharp
// === World/Time/WorldLightingDirector.cs ===
public sealed class WorldLightingDirector : MonoBehaviour
{
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D _globalLight;
    [SerializeField] private LightingKey[] _keys;   // 锚点，见颜色表

    [System.Serializable]
    public struct LightingKey { public float AtMinutes; public Color Color; public float Intensity; }

    private void Update();   // GradientSampler 采样 → _globalLight.color / .intensity
}
```

### 天空渐变（SkyGradient）
全屏天空层钉在相机上，**顶色 + 底色两段垂直渐变**，两色各自随时段 Lerp。同时统一管理**星空 / 月亮的 alpha**与**云 tint 的输出**（集中时间采样，避免分散）。

```csharp
// === World/Time/SkyGradient.cs ===
public sealed class SkyGradient : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _skyFill;   // 全屏纯色底（地平线/底色）
    [SerializeField] private SpriteRenderer _skyTop;    // 顶部垂直渐变叠加（白→透明渐变 sprite, tint 成顶色）
    [SerializeField] private SpriteRenderer _starfield; // night/starfield.png, 深夜 alpha 淡入
    [SerializeField] private SpriteRenderer _moon;      // night/moon.png, 深夜 alpha 淡入
    [SerializeField] private ParallaxCloudLayer[] _cloudLayers;   // 三层云, 接收 tint

    [SerializeField] private SkyColorKey[] _skyKeys;        // 天空顶/底色锚点
    [SerializeField] private CloudTintKey[] _cloudTintKeys; // 云 tint 锚点
    [SerializeField] private NightAlphaKey[] _nightKeys;    // 星空/月亮 alpha 锚点

    [System.Serializable] public struct SkyColorKey   { public float AtMinutes; public Color TopColor; public Color HorizonColor; }
    [System.Serializable] public struct CloudTintKey  { public float AtMinutes; public Color Tint; }
    [System.Serializable] public struct NightAlphaKey { public float AtMinutes; public float Alpha; }

    private void Update();
    // 1. 天空: Lerp TopColor→_skyTop.color, HorizonColor→_skyFill.color
    // 2. 夜空: Lerp Alpha → _starfield.color.a 与 _moon.color.a
    // 3. 云: Lerp Tint → 每个 _cloudLayers[i].SetTint(tint)
}
```

**实现要点**：
- `_skyTop` 需要一张"白色垂直渐变"sprite（顶 alpha=1 → 底 alpha=0）。**Codex 用代码生成 `Texture2D`（如 4×256 纵向 alpha 渐变）** 转 Sprite，不要额外美术文件。
- `_skyFill / _skyTop / _starfield / _moon` 都是**相机子物体**，缩放铺满相机视野，视差 0（永远填满屏幕）。
- `_starfield` 不透明：深夜 `alpha=1` 时会盖住代码渐变天空（深夜本就要星空图，符合预期）；过渡区半透明混合，自然衔接。
- `_moon` 位置可在 Inspector 设（建议偏上居中），深夜显现。

### 三层视差云（ParallaxCloudLayer）
三个云层各持 `clouds/` 一张透明云，钉相机，按相机水平位移做视差滚动 + 无缝循环；tint 由 SkyGradient 推入。

```csharp
// === World/Time/ParallaxCloudLayer.cs ===
public sealed class ParallaxCloudLayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _copyA;   // 两份同图首尾相接做无限循环
    [SerializeField] private SpriteRenderer _copyB;
    [SerializeField] private Transform _camera;
    [SerializeField] private float _parallaxFactor = 0.15f;  // 云相对玩家的移动比例（越小越远/越慢）
    [SerializeField] private float _windSpeed = 0.2f;        // 额外自动飘移 (units/秒)

    private float _layerWidth;   // 一张云的世界宽度 (Awake 从 sprite.bounds 取)

    // LateUpdate:
    //   float scroll  = _camera.position.x * (1f - _parallaxFactor) + _windSpeed * Time.time;
    //   float wrapped = Mathf.Repeat(scroll, _layerWidth);
    //   _copyA.localPosition.x = -wrapped;
    //   _copyB.localPosition.x = -wrapped + _layerWidth;
    //   (root 跟随相机 X, 使云始终覆盖视野)

    public void SetTint(Color c) { _copyA.color = c; _copyB.color = c; }
}
```

**视差含义**：`_parallaxFactor` = 云相对玩家的视觉移动比例。`0.1` 即"玩家走 10 米云只移 1 米"——正是"幅度比人物小很多"。三层制造纵深：

| 云层 | 素材 | _parallaxFactor |
|---|---|---|
| 远云 | `clouds/cloud_far.png` | 0.08 |
| 中云 | `clouds/cloud_mid.png` | 0.15 |
| 近云 | `clouds/cloud_near.png` | 0.25 |

> **无缝循环**：CraftPix 标称 "fully looping layers"。Codex 自测须横向移动相机长距离确认无硬缝；双拷贝 `_copyA/_copyB` 已消除大部分接缝，必要时导入设置 Wrap Mode = Repeat。

### 颜色锚点表（6 锚点，首尾一致循环；初始值，全部 Inspector 可调）

> 按游戏分钟升序；DeepNight 跨日，故 02:00(120) 与 22:00(1320) 取同值保证循环连续。

| 时刻 | 分钟 | 天空顶 | 天空底 | 云 Tint | 光照 Color / Int | 夜空 Alpha |
|---|---|---|---|---|---|---|
| 02:00 | 120 | (0.06,0.08,0.20) | (0.12,0.15,0.32) | (0.30,0.36,0.55) | (0.30,0.40,0.70)/0.35 | 1.0 |
| 05:00 | 300 | (0.20,0.22,0.42) | (0.45,0.40,0.55) | (0.55,0.55,0.65) | (0.55,0.55,0.70)/0.50 | 1.0 |
| 06:00 | 360 | (0.55,0.55,0.80) | (0.98,0.80,0.65) | (1.00,0.92,0.82) | (1.00,0.95,0.85)/0.85 | 0.0 |
| 10:00 | 600 | (0.40,0.55,0.92) | (0.62,0.78,0.98) | (1.00,1.00,1.00) | (1.00,1.00,0.95)/1.00 | 0.0 |
| 14:00 | 840 | (0.42,0.56,0.90) | (0.65,0.78,0.95) | (1.00,0.98,0.95) | (1.00,0.90,0.80)/0.95 | 0.0 |
| 18:00 | 1080 | (0.45,0.35,0.55) | (0.92,0.50,0.38) | (1.00,0.62,0.45) | (1.00,0.60,0.40)/0.70 | 0.0 |
| 20:00 | 1200 | (0.10,0.12,0.28) | (0.20,0.20,0.40) | (0.45,0.45,0.62) | (0.45,0.50,0.75)/0.45 | 1.0 |
| 22:00 | 1320 | (0.06,0.08,0.20) | (0.12,0.15,0.32) | (0.30,0.36,0.55) | (0.30,0.40,0.70)/0.35 | 1.0 |

> 夜空 alpha：白天(06:00–18:00)=0，黄昏(18:00→20:00)淡入到 1，深夜保持 1，清晨(05:00→06:00)淡出到 0。星空 + 月亮共用这条 alpha 曲线。跨 22:00→02:00 环绕同前。

### EnemySpawner 接入夜间生成
`EnemySpawner.TryPickEntry` 加权前过滤：
```csharp
TimeOfDay currentTime = WorldClock.Instance != null ? WorldClock.Instance.CurrentTime : TimeOfDay.Noon;
TimeOfDayMask currentMask = MaskFromTimeOfDay(currentTime);
foreach (var entry in _config.SpawnTable)
{
    if ((entry.AllowedTimes & currentMask) == 0) continue;
    candidates.Add(entry);
}
// 在 candidates 上加权随机
```
**Slime**：AllowedTimes = All　**Zombie**：AllowedTimes = Evening | DeepNight（022 已填好 mask，本任务只需让 EnemySpawner 真正读取）

### 暂停菜单
`Time.timeScale = 0` 时 WorldClock.Update 不累加 → 时间冻结；云的 `_windSpeed * Time.time` 也随之冻结。**不需要额外改动**。

### 存档接入
```csharp
[System.Serializable]
public sealed class TimeSaveData { public float GameMinutes; }
```
`SaveData` 根新增 `Time` 字段；`GameStateSnapshot.Capture/Apply` 加分支调用 `WorldClock.CurrentGameMinutes / SetTime`。新建存档默认时间 = 360（06:00 Morning）。

## 接口签名
```csharp
// World/Time/TimeOfDay.cs          — 新增 TimeOfDay 枚举（与 022 的 TimeOfDayMask flags 区分）
// World/Time/WorldClock.cs         — 见设计概要
// World/Time/GradientSampler.cs    — 静态采样工具，处理跨日环绕
// World/Time/WorldLightingDirector.cs — 锚点 AtMinutes 浮点驱动
// World/Time/SkyGradient.cs        — 天空两色 + 星空/月亮 alpha + 云 tint 统一管理
// World/Time/ParallaxCloudLayer.cs — 视差滚动 + 无缝循环 + SetTint

// Enemies/EnemySpawner.cs — 修改：TryPickEntry 加 AllowedTimes 过滤
private static TimeOfDayMask MaskFromTimeOfDay(TimeOfDay t) => t switch
{
    TimeOfDay.Morning => TimeOfDayMask.Morning,
    TimeOfDay.Noon => TimeOfDayMask.Noon,
    TimeOfDay.Afternoon => TimeOfDayMask.Afternoon,
    TimeOfDay.Evening => TimeOfDayMask.Evening,
    TimeOfDay.DeepNight => TimeOfDayMask.DeepNight,
    _ => TimeOfDayMask.None,
};

// Save/SaveData.cs          — 新增 TimeSaveData Time;
// Save/GameStateSnapshot.cs — Capture/Apply 加 Time 分支
```

## 依赖
- 022 EnemySpawner（AllowedTimes 字段已加）/ 014 EnemySpawner（基础架构）
- 017 PauseMenu（timeScale=0 自动暂停时钟）/ 018 SaveData（扩展 TimeSaveData）
- 003 CameraFollow（云/天空/夜空层需引用主相机做钉随与视差）
- URP 2D Renderer（Light2D）

## 文件清单

### 新增（脚本）
- `Assets/Scripts/World/Time/TimeOfDay.cs`
- `Assets/Scripts/World/Time/WorldClock.cs`
- `Assets/Scripts/World/Time/GradientSampler.cs`
- `Assets/Scripts/World/Time/WorldLightingDirector.cs`
- `Assets/Scripts/World/Time/SkyGradient.cs`
- `Assets/Scripts/World/Time/ParallaxCloudLayer.cs`
- `Assets/Scripts/Save/TimeSaveData.cs`

### 修改
- `Assets/Scripts/Enemies/EnemySpawner.cs` — TryPickEntry 加时段过滤
- `Assets/Scripts/Save/SaveData.cs` — 新增 Time 字段 + `CreateNewGameDefault` 设 Time = 360
- `Assets/Scripts/Save/GameStateSnapshot.cs` — Capture/Apply 加 Time 分支

### 美术（已就位，仅需导入配置）
- `Assets/Art/Backgrounds/clouds/cloud_far.png`、`cloud_mid.png`、`cloud_near.png` — Sprite(Single)，透明云
- `Assets/Art/Backgrounds/night/starfield.png` — Sprite(Single)，不透明星空底
- `Assets/Art/Backgrounds/night/moon.png` — Sprite(Single)，透明月亮
- 导入参数：Filter = Point（像素风），Compression = None；云层 Wrap Mode = Repeat（备无缝循环）；PPU 按相机视野调，保证单张云宽 ≥ 相机视野宽
- 天空垂直渐变贴图：**代码生成**（无需美术文件）

> `weather_clouds/` 本任务不导入（不在场景引用、不配置），原样保留给未来天气系统。

### 场景配置（MCP）
- `SampleScene` 新增 `WorldClock`（挂 WorldClock）
- `SampleScene` 新增 `Global Light 2D`（挂 WorldLightingDirector，锚点按颜色表填）
- 相机下建背景树：`SkyGradient`（_skyFill + _skyTop + _starfield + _moon 四个 SpriteRenderer 子物体）+ 3 个 `ParallaxCloudLayer`（各双拷贝 SpriteRenderer）
  - sortingOrder 按"渲染层次"表从低到高排，全部低于游戏世界
  - SkyGradient 引用四个渲染器 + 三个云层，填三组锚点
- `EnemySpawnerConfig.asset` 复查 Slime / Zombie 的 AllowedTimes（022 已填）

## 验收标准

### 时间流逝
- [ ] 进入游戏时间从 06:00 开始；24 真实分钟一日循环
- [ ] 暂停（Esc）冻结时间，关闭恢复
- [ ] CurrentTime 5 段正确切换，OnTimeOfDayChanged 顺序正确

### 光照
- [ ] Morning 0.85 / Noon 1.0 / Evening 0.7 / DeepNight 0.35；时段切换**平滑过渡**不瞬切

### 天空渐变
- [ ] 天空上下垂直渐变（顶深底亮）
- [ ] 时段切换天空顶/底色平滑过渡，无闪屏；夜深蓝、正午蓝、黄昏橙红，与光照一致

### 夜空（星空 + 月亮）
- [ ] 白天/正午看不到星空和月亮（alpha=0）
- [ ] 黄昏→深夜星空与月亮平滑淡入；清晨平滑淡出
- [ ] 深夜可见星星 + 月亮，整体与深蓝夜空协调

### 视差云
- [ ] 玩家左右移动时三层云横向滚动且**幅度明显小于角色**（远云最慢、近云稍快）
- [ ] 长距离移动云无缝循环，无硬缝/空白
- [ ] 云色随时段 tint 渐变（正午白、黄昏橙、深夜暗蓝）
- [ ] 暂停时云停止飘移；玩家静止时云仍以 `_windSpeed` 缓慢飘（设 0 则静止）

### 敌人生成
- [ ] Evening + DeepNight 刷僵尸；Morning/Noon/Afternoon 只刷史莱姆
- [ ] 时段切换瞬间已存在僵尸不自动消失（只影响新生成）

### 存档
- [ ] 保存记录当前游戏分钟；读档恢复；新建存档 = 360

### 综合
- [ ] 编译无错误无警告；不引入新控制台错误
- [ ] MCP 截图：5 时段各 1 张（体现光照/天空色/云色/夜空差异）
- [ ] **视差证明**：玩家移动前后两帧（或 GIF），标注角色位移 vs 云位移
- [ ] 符合 `coding-conventions.md`

## 注意事项

### WorldClock 单例
`static Instance`，Awake 设置 / OnDestroy 清空，**不做 DontDestroyOnLoad**（主菜单不需要时钟，SampleScene 自挂）。

### 时间速度可调
`_gameMinutesPerSecond` 暴露 Inspector，调试设 60（一秒一小时）观察过渡。

### 锚点插值与跨日环绕
天空/光照/云 tint/夜空 alpha 全部复用 `GradientSampler`。跨 22:00→02:00 环绕：找区间时，若 minutes 大于末锚点或小于首锚点，在"末锚点→首锚点+1440"区间插值。

### 背景缩放与相机
天空/云/夜空层钉为相机子物体，按 `Camera.orthographicSize` 算铺满缩放。相机 orthographicSize 以场景实际为准（历史为 7），**按实际值算，不硬编码**。

### 星空不透明的处理
`starfield.png` 不透明，深夜 alpha=1 时盖住代码渐变天空属预期（深夜即用星空图）。**不要**试图抠出透明星点层（深紫底有渐变，抠图不干净）。

### 不做的事
- 不做月相 / 季节 / 星星闪烁动画（月亮为静态满月）
- 不做天气（雨/雪/阴天云切换）——`weather_clouds/` 三套云为此预留，独立天气任务
- 不做声音切换 / 强制睡觉跳夜 / 特定时段合成
- 不存在旧 `BackgroundController` 单层方案（原草案作废，本任务直接用 SkyGradient + 夜空层 + ParallaxCloudLayer）

## 交付记录（Codex 必填）
完成并自测通过后，**push 分支前**在 `docs/codex-reports/025-day-night-cycle.md` 写交付记录。

**额外要求**：
1. **MCP 5 张截图**：5 时段各 1（体现光照 + 天空色 + 云色 + 夜空）
2. **视差证明**：玩家移动前后两帧（或 GIF），标注角色位移 vs 云位移
3. **夜空淡入淡出**：黄昏→深夜→清晨各 1 张，证明星空/月亮 alpha 过渡
4. **僵尸时段过滤实测**：加速时间，记录 Morning 不刷 / Evening 开始刷
5. **存档往返**：保存记录 minutes，读档验证恢复
6. **无缝循环说明**：横向长距离移动后云层是否有硬缝
