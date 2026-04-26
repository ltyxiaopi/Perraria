# 任务 016 - 主菜单场景

## 目标
新增主菜单场景作为游戏启动入口，包含 `Start Game` 和 `Quit Game` 两个按钮。
主菜单为 Build Settings 索引 0，游戏场景 `SampleScene` 索引 1。
点击 `Start Game` 加载游戏场景；点击 `Quit Game` 关闭程序。

> **文案约定**：本任务所有 UI 文本一律使用英文。项目当前只有 `LiberationSans SDF` 字体，
> 不含 CJK 字形；中文 UI 留待后续任务专门导入 CJK 字体后再启用。

## 接口签名

```csharp
[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private string _gameSceneName = "SampleScene";

    public void OnStartClicked();   // 加载游戏场景
    public void OnQuitClicked();    // 关闭程序（编辑器内退出 Play 模式，构建后 Application.Quit）
}
```

## 依赖
- 无（独立场景，不依赖游戏内任何模块）

## 文件清单
- `Assets/Scripts/UI/MainMenuController.cs` — 新增
- `Assets/Scenes/MainMenu.unity` — **通过 MCP 创建**：
  - 主相机（背景色与游戏一致即可）
  - Canvas（Screen Space - Overlay）
  - 标题文本 `Perraria`（屏幕上方居中，TMP，字号大）
  - `Start Game` 按钮（屏幕中间，TMP 文本 = `Start Game`）
  - `Quit Game` 按钮（在 Start Game 下方，TMP 文本 = `Quit Game`）
  - 空物体 `MainMenu` 挂 `MainMenuController`，绑定两个按钮引用
- Build Settings：MainMenu 为索引 0，SampleScene 为索引 1（**通过 MCP 修改 ProjectSettings/EditorBuildSettings.asset**）

## 验收标准
- [ ] 编辑器启动游戏时进入 MainMenu，不直接进入 SampleScene
- [ ] 主菜单标题文字 `Perraria` 显示在屏幕上方
- [ ] 两个按钮可见文本分别为 `Start Game` 和 `Quit Game`，无 tofu/缺字（用 `TMP_FontAsset.HasCharacter` 验证全部命中）
- [ ] 点击 `Start Game` 按钮 → 加载 SampleScene，玩家正常生成、地形正常显示
- [ ] 点击 `Quit Game` 按钮 → 编辑器中停止 Play；构建后退出程序
- [ ] 主菜单 UI 适配不同分辨率（CanvasScaler 用 `Scale With Screen Size`，参考 1920×1080）
- [ ] 切换到 SampleScene 后无残留主菜单 UI

## 注意事项
- **场景跳转用 `SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single)`**，单场景模式自动卸载主菜单
- **退出逻辑**：
  ```csharp
  #if UNITY_EDITOR
  UnityEditor.EditorApplication.isPlaying = false;
  #else
  Application.Quit();
  #endif
  ```
- **不在主菜单加载游戏存档**——任务 019 才接通「继续游戏」按钮，本任务只做新游戏入口
- **背景**：临时纯色或低饱和度图，未来可换成游戏画面截图，本任务不强求美化
- **按钮使用 TextMeshPro**，与项目其它 UI 保持一致

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/016-main-menu.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
