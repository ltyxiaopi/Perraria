# 017-pause-menu

## 任务信息
- 任务: 017 - 暂停菜单
- 规格书: `docs/tasks/017-pause-menu.md`
- 分支: `feature/017-pause-menu`

## 本次完成内容
- 新增 `PauseMenuController`，实现:
  - `Keyboard.current.escapeKey.wasPressedThisFrame` 切换暂停/恢复
  - `Pause()` 设置面板显示、`Time.timeScale = 0`、`IsPaused = true`
  - `Resume()` 设置面板隐藏、`Time.timeScale = 1`、`IsPaused = false`
  - `OnMainMenuClicked()` 先恢复 `Time.timeScale = 1`，再用 `LoadSceneMode.Single` 加载 `MainMenu`
  - `OnEnable` / `OnDisable` 绑定与解绑按钮事件
  - `OnDisable` / `OnDestroy` 防御性恢复 `Time.timeScale = 1` 并隐藏面板
  - `Save` 按钮本任务保持 disabled，`OnSaveClicked()` 为空实现
- 通过 Unity MCP 配置 `SampleScene`:
  - 在 `Canvas` 末尾新增 `PauseMenu`
  - `PauseMenu/Panel` 为全屏半透明黑色遮罩，`color=(0,0,0,0.6)`
  - 中央 `VerticalLayoutGroup` 包含 `Resume` / `Save` / `Main Menu` 三个按钮
  - 三个按钮文本均使用 `LiberationSans SDF`
  - `Panel.activeSelf=False`
  - `SaveButton.interactable=False`
- 暂停时屏蔽其它输入:
  - `PlayerController.Update()`
  - `PlayerCombat.Update()`
  - `PlayerBlockInteraction.Update()`
  - `InventoryUI.Update()`
  - `HotbarUI` 当前没有 `Update()` 且不直接读取键盘输入；热键切换在 `PlayerBlockInteraction.Update()` 中，已被暂停 guard 覆盖。

## 变更文件清单
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/UI/PauseMenuController.cs`
- `Assets/Scripts/UI/PauseMenuController.cs.meta`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- `Assets/Scripts/UI/InventoryUI.cs`
- `docs/codex-reports/017-pause-menu.md`

## 运行态自测日志
- 编译检查:
  - `isCompilationSuccessful=True`
  - `isExecutionSuccessful=True`
- 静态场景配置:
  - `Static scene=SampleScene`
  - `controller=PauseMenu`
  - `panelActiveSelf=False`
  - `hudSibling=2`
  - `pauseMenuSibling=3`
  - `canvasChildren=4`
  - `resume=ResumeButton/True`
  - `save=SaveButton/False`
  - `main=MainMenuButton/True`
  - `text labels=Resume:LiberationSans SDF,Save:LiberationSans SDF,Main Menu:LiberationSans SDF`
- MainMenu 到 SampleScene:
  - `isPlaying=True`
  - `activeScene=MainMenu`
  - 调用 `MainMenuController.OnStartClicked()`
  - 后续检查 `activeScene=SampleScene`
  - `Player=found`
  - `HUD=found`
  - `PauseMenu=found`
- ESC 暂停/恢复路径:
  - 使用 Input System 队列 `KeyboardState(Key.Escape)`，并执行 `PauseMenuController.Update()` 路径
  - `ESC path before: paused=False panel=False timeScale=1 keyboard=Keyboard`
  - `ESC path after first press: wasPressed=True paused=True panel=True timeScale=0`
  - `ESC path after second press: wasPressed=True paused=False panel=False timeScale=1`
- Resume 按钮:
  - 暂停前 `paused=True panel=True timeScale=0`
  - `ResumeButton.onClick.Invoke()`
  - 恢复后 `paused=False panel=False timeScale=1`
- Main Menu 按钮:
  - 点击前 `scene=SampleScene paused=True timeScale=0`
  - 点击后先记录 `timeScale=1`
  - 后续检查 `activeScene=MainMenu timeScale=1 mainMenuController=found`
- Save 按钮:
  - `activeInHierarchy=True`
  - `interactable=False`
  - 程序化调用 `onClick` 后 `paused=True timeScale=0`
  - 说明: 真实 UI 点击因 `interactable=False` 不会触发
- 字体覆盖:
  - `Resume` 使用 `LiberationSans SDF`, `missing=[]`
  - `Save` 使用 `LiberationSans SDF`, `missing=[]`
  - `Main Menu` 使用 `LiberationSans SDF`, `missing=[]`
- 暂停时输入屏蔽:
  - `Time.timeScale=0`
  - 暂停时模拟 `Tab`: `inventoryOpenBefore=False inventoryOpenAfter=False`
  - 暂停时模拟 `Digit2`: `hotbarBefore=0 hotbarAfter=0`
  - 说明: 当前项目背包切换键为 `Tab`，不是 `B`
- HUD 覆盖顺序:
  - `hudSibling=2`
  - `pauseMenuSibling=3`
  - `PauseMenu` 在同一 Canvas 内位于 HUD 后渲染
- 玩家/敌人/生成器暂停:
  - `Pause overlay: scene=SampleScene isPaused=True panelActive=True timeScale=0`
  - `playerBody=found`
  - `enemy=Slime(Clone)`
  - `spawner=EnemySpawner`
  - 敌人与生成器使用 `Time.deltaTime` 驱动；暂停时 `timeScale=0`
- 重复流程:
  - `MainMenu -> SampleScene -> Pause -> MainMenu -> SampleScene`
  - 返回 MainMenu 后 `timeScale=1`
  - 再次 Start Game 后 `scene=SampleScene timeScale=1 player=found pauseMenu=found`
- Console:
  - 项目相关编译和运行命令均 `isCompilationSuccessful=True`
  - `errorCount=0`
  - `warningCount=1`
  - 唯一 warning 来自 Unity AI Assistant 包: `Account API did not become accessible within 30 seconds...`
  - 该 warning 与本任务代码、场景和运行态流程无关；本任务未产生 error/exception。

## Claude 审查重点
- `PauseMenuController` 是否严格使用 Input System 的 `Keyboard.current.escapeKey.wasPressedThisFrame`。
- `OnMainMenuClicked()` 是否先恢复 `Time.timeScale = 1`，再 `SceneManager.LoadScene(_mainMenuSceneName, LoadSceneMode.Single)`。
- `OnDisable` / `OnDestroy` 是否能防御性恢复 `Time.timeScale = 1`。
- `SaveButton.interactable` 是否保持 `false`，且未实现真实保存逻辑。
- `SampleScene` 中 `PauseMenu` 是否位于 HUD 后，`Panel` 是否默认隐藏。
- 指定输入脚本是否只做最小暂停 guard，没有引入全局 Pause 单例。

## 已知说明
- `HotbarUI` 当前没有 `Update()`，也不直接读键盘；热键切换来自 `PlayerBlockInteraction.Update()`，本任务已在该入口阻断暂停态数字键。
- 当前项目背包开关键是 `Tab`；自测按现有实现验证暂停态 `Tab` 不会打开背包。
- Unity Console 的 1 条 warning 来自 Unity AI Assistant 包账号 API 网络状态，不是项目代码或本任务变更产生。
