# 任务 017 - 暂停菜单

## 目标
游戏中按 ESC 弹出暂停菜单，遮罩游戏画面，`Time.timeScale = 0` 暂停玩法。
菜单提供：「继续游戏」「保存游戏」「返回主菜单」。
本任务实现菜单骨架与「继续 / 返回主菜单」的接通；
**「保存游戏」按钮先 disabled，由任务 019 启用**。

## 接口签名

```csharp
[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;       // 整体面板（默认 SetActive=false）
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _saveButton;           // 保存按钮（本任务保持 interactable=false）
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    public void Pause();         // 弹面板、Time.timeScale = 0、IsPaused = true
    public void Resume();        // 关面板、Time.timeScale = 1、IsPaused = false
    public void OnSaveClicked(); // 任务 019 实现，本任务可空方法或日志占位
    public void OnMainMenuClicked(); // Time.timeScale 恢复 + 加载 MainMenu
}
```

ESC 触发逻辑：用 `InputSystem` 的 `Keyboard.current.escapeKey.wasPressedThisFrame`，
按一次切换 Pause/Resume。**注意 `Time.timeScale = 0` 后 `Time.deltaTime = 0`，
但 `wasPressedThisFrame` 基于 unscaled time，不会失效**。

## 依赖
- 任务 016 MainMenu（返回主菜单跳转）

## 文件清单
- `Assets/Scripts/UI/PauseMenuController.cs` — 新增
- 场景配置（**通过 MCP 操作 SampleScene**）：
  - 在 Canvas 下新增 `PauseMenu/Panel`：
    - 全屏黑色半透明遮罩 Image（`color=(0,0,0,0.6)`，覆盖整屏）
    - 中央竖向布局：「继续游戏」「保存游戏」「返回主菜单」三个按钮（TMP 文本）
  - `PauseMenu` 物体挂 `PauseMenuController`，绑定面板和三个按钮
  - 默认 `Panel.SetActive(false)`，Inspector 中即设为隐藏
  - 「保存游戏」按钮 `interactable = false`

## 验收标准
- [ ] 游戏中按 ESC 出现暂停面板，玩家、敌人、生成器全部停止运动
- [ ] 暂停时再按 ESC 关闭面板，游戏恢复
- [ ] 点「继续游戏」按钮等价于按 ESC 关闭
- [ ] 点「返回主菜单」加载 MainMenu 场景，且 `Time.timeScale` 已被恢复为 1（避免主菜单按钮无响应）
- [ ] 「保存游戏」按钮显示但灰色不可点击
- [ ] 暂停时背包 / 快捷栏的快捷键（B、1-9）应失效，避免暂停时仍能交互（参见注意事项）
- [ ] 暂停面板覆盖在 HUD 之上（Canvas Sort Order 或 panel 在 HUD 之后渲染）

## 注意事项
- **`Time.timeScale = 0` 不会停止 `Update`**，只让 `Time.deltaTime = 0`，所以基于物理 / 协程的玩法会停，
  但 `Update` 中读取键盘的代码仍执行——这正是 ESC 能再次解暂停的前提
- **暂停时禁用其它输入**：本任务不强行改其它脚本逻辑，依赖事实「`Time.deltaTime=0` 让物理类玩法停下」即可；
  但是 `InventoryUI / HotbarUI / PlayerCombat` 的输入回调仍会触发——
  推荐方式：`PauseMenuController` 暂停时调用 `Cursor.lockState`、`EventSystem` 不变，
  其它脚本可在 `Update` 顶部判 `if (Time.timeScale == 0) return;` 或检查全局 `Pause.IsPaused`。
  **本任务最小实现**：在 PlayerController、PlayerCombat、PlayerBlockInteraction、InventoryUI、HotbarUI 的 `Update` 顶部加 `if (Time.timeScale <= 0f) return;`，
  其它脚本不动。
- **离开暂停回主菜单时务必 `Time.timeScale = 1`**，否则 MainMenu 场景的按钮 / 动画会卡住
- **不实现「保存游戏」按钮的真实逻辑**——任务 019 接通；本任务只做按钮存在 + disabled 状态
- **不做** 设置子菜单 / 音量调节 / 退出游戏（退出走「返回主菜单」即可）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/017-pause-menu.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
