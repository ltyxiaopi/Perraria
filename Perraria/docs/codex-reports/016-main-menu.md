# 016-main-menu

## 任务信息
- 任务: 016 - 主菜单场景
- 规格书: `docs/tasks/016-main-menu.md`
- 分支: `feature/016-main-menu`

## 本次完成内容
- 新增 `Assets/Scripts/UI/MainMenuController.cs`，实现：
  - `OnStartClicked()` 使用 `SceneManager.LoadScene("SampleScene", LoadSceneMode.Single)`
  - `OnQuitClicked()` 使用 `#if UNITY_EDITOR` 在编辑器内退出 Play，构建后调用 `Application.Quit()`
  - `OnEnable` / `OnDisable` 绑定和解绑开始、退出按钮点击事件
- 通过 Unity MCP 创建 `Assets/Scenes/MainMenu.unity`：
  - 主相机背景色与 `SampleScene` 一致
  - `Canvas` 使用 `Screen Space - Overlay`
  - `CanvasScaler` 使用 `Scale With Screen Size`，参考分辨率 `1920x1080`
  - 标题 `Perraria` 使用 `TextMeshProUGUI`
  - 「开始游戏」「退出游戏」按钮使用 TMP 文本，放在竖向布局容器中
  - 空物体 `MainMenu` 挂 `MainMenuController` 并绑定两个按钮
  - `EventSystem` 使用 `InputSystemUIInputModule`
- 更新 `ProjectSettings/EditorBuildSettings.asset`：
  - `MainMenu.unity` 为 index 0
  - `SampleScene.unity` 为 index 1
- 修订：将两个按钮文案从中文改为英文 `Start Game` / `Quit Game`，原因是项目当前仅有 `LiberationSans SDF`，不含 CJK 字形；改为英文以避免运行时出现 tofu 方块。

## 变更文件清单
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/MainMenuController.cs.meta`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/MainMenu.unity.meta`
- `ProjectSettings/EditorBuildSettings.asset`
- `docs/codex-reports/016-main-menu.md`

## 运行态自测日志
- Play 从 `MainMenu` 进入：
  - `isPlaying=True`
  - `Active scene=MainMenu`
  - `Title=Title`
  - `StartButton=StartButton`
  - `QuitButton=QuitButton`
- 主菜单静态配置：
  - `CanvasScaler mode=ScaleWithScreenSize`
  - `refRes=(1920,1080)`
  - `match=0.5`
  - `Title text=Perraria`
  - `Title fontSize=96`
- 英文按钮文本与字形覆盖：
  - `StartButton text=Start Game`
  - `QuitButton text=Quit Game`
  - `StartButton font=LiberationSans SDF`
  - `QuitButton font=LiberationSans SDF`
  - `StartButton missing=[]`
  - `QuitButton missing=[]`
- 点击「开始游戏」后：
  - `Active scene=SampleScene`
  - `Player=Player`
  - `Grid=Grid`
  - `HUD=HUD`
  - `HealthBar=HealthBar`
  - `Heart sprites=heartDisplay_0 x5`
  - 主菜单对象卸载：`Title` / `StartButton` 查询结果为空
- 点击「退出游戏」后：
  - `EditorApplication.isPlaying=False`
- 控制台检查：
  - `errorCount=0`
  - `warningCount=0`
- 分辨率适配检查：
  - 标题锚点 `anchorMin/max=(0.5,1.0)`，顶部居中
  - 按钮容器锚点 `anchorMin/max=(0.5,0.5)`，中间居中
  - 按钮容器使用 `VerticalLayoutGroup + ContentSizeFitter`
  - 结合 `CanvasScaler Scale With Screen Size` 满足 1280×720 / 2560×1440 的自适应要求

## Claude 审查重点
- `MainMenuController` 是否严格使用：
  - `SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single)`
  - `#if UNITY_EDITOR` 条件编译退出逻辑
- `MainMenu.unity` 是否完整包含：
  - `Main Camera`
  - `Canvas`
  - `EventSystem + InputSystemUIInputModule`
  - `Title`
  - `StartButton`
  - `QuitButton`
  - `MainMenuController` 绑定
- `EditorBuildSettings.asset` 是否保证：
  - `MainMenu` index 0
  - `SampleScene` index 1
- 切到 `SampleScene` 后是否无残留 `MainMenu` UI

## 已知说明
- 分辨率适配未额外做美术背景，只按规格书保持纯色背景。
- 分辨率适配主要通过 `CanvasScaler + centered anchors + vertical layout` 验证；未额外引入复杂的 GameView 反射脚本去改编辑器分辨率。
