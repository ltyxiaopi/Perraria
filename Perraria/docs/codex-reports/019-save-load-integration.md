# 019-save-load-integration

## 任务信息
- 任务: 019 - 存档/读档接入 UI
- 规格书: `docs/tasks/019-save-load-integration.md`
- 分支: `feature/019-save-load-integration`

## 本次完成内容
- 新增 `GameManager` 和 `GameLaunchMode`，用静态 `PendingLaunchMode` 串起 MainMenu -> SampleScene 的 New Game / Continue 流程。
- `GameManager` 使用 `[DefaultExecutionOrder(100)]`，保证 `Start()` 晚于 `WorldGenerator.Start()`，Continue 时只调用 `SaveSystem.Load()` 和 `GameStateSnapshot.Apply(loaded)`。
- 主菜单新增 Continue 按钮，`MainMenuController.Start()` 根据 `SaveSystem.HasSave()` 控制显示。
- Start Game 显式设置 `PendingLaunchMode = NewGame`，Continue 显式设置 `PendingLaunchMode = ContinueSave`。
- 暂停菜单 Save 按钮改为可点击，点击后 `GameStateSnapshot.Capture()` + `SaveSystem.Save()`，按钮文案用 unscaled coroutine 临时显示 `Saved` 后恢复 `Save`。
- SampleScene 新增 `GameManager` 物体并绑定组件，PauseMenu 的 SaveButton 设置为可交互。
- MainMenu 场景新增 `ContinueButton`，位置在 Start 和 Quit 之间，文案为英文 `Continue`，字体使用 LiberationSans SDF。
- `PlayerHealth.RestoreState()` 恢复受伤血量时把回血计时器设为 `_regenDelay`，避免 Continue 后第一帧立即回血导致读档 HP 被运行态系统改写。

## 变更文件清单
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/GameManager.cs.meta`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/PauseMenuController.cs`
- `Assets/Scripts/Player/PlayerHealth.cs`
- `docs/codex-reports/019-save-load-integration.md`

## 完整流程自测日志

### 静态配置与字体
- `Font check: text=[Continue] font=[LiberationSans SDF] missing=[[]]`
- `Font check: text=[Save] font=[LiberationSans SDF] missing=[[]]`
- `Font check: text=[Saved] font=[LiberationSans SDF] missing=[[]]`
- `MainMenu static: controller=[True] continue=[True] activeSelf=[True] text=[Continue] font=[LiberationSans SDF]`
- `SampleScene static: gameManager=[True] saveButton=[True] saveInteractable=[True]`

### Unity Editor 编译
- `019 compile after restart retry: launchMode=[NewGame] hasSave=[False]`
- `019 compile after PlayerHealth restore guard: launchMode=[NewGame]`
- `019 final compile check: launchMode=[[NewGame]] hasSave=[[False]] timeScale=[[1]]`

### 流程 1: 清空存档 + 新游戏
- `Flow1 fresh MainMenu: scene=[MainMenu] hasSave=[False] continueActiveSelf=[False] continueActiveHierarchy=[False] launchMode=[NewGame]`
- `Flow1 fresh SampleScene: scene=[SampleScene] launchMode=[NewGame] seed=[-54879632] playerFound=[True] tileManagerFound=[True] gameManagerFound=[True]`

### 流程 2: 保存
- `Flow2 fresh saved: seed=[-54879632] pos=[(0.50, 33.59, 0.00)] hp=[75]/[100] slot0=[1]:[4] slot1=[3]:[6] selected=[1] set20=[True] set21=[True] tile20=[Air] tile21=[Dirt] edits=[2] spawnTimer=[9.87] hasSave=[True] fileBytes=[4205] saveText=[Saved] dirtRemaining=[0] stoneRemaining=[0]`
- `Flow2 feedback restored: isPaused=[True] timeScale=[0] saveTextAfterDelay=[Save] hasSave=[True]`

### 流程 3: 保存后回主菜单
- `Flow3 fresh MainMenu: scene=[MainMenu] hasSave=[True] continueActiveSelf=[True] continueActiveHierarchy=[True] launchMode=[NewGame]`

### 流程 4: 继续游戏
- `Flow4 fresh continue clicked: launchMode=[ContinueSave] sceneBeforeStep=[MainMenu] freezeTimeScale=[0]`
- `Flow4 fresh loaded: scene=[SampleScene] savedSeed=[-54879632] worldSeed=[-54879632] launchMode=[ContinueSave] timeScale=[0]`
- `Flow4 fresh state: pos=[(0.50, 33.59, 0.00)] expected=[(0.50, 33.59, 0.00)] hp=[75]/[100] facingRight=[False] slot0=[1]:[4] slot1=[3]:[6] selected=[1] tile20=[Air] tile21=[Dirt] spawnTimer=[9.87]`

### 流程 5: 新游戏不删除旧档，Continue 仍回旧档
- `Flow5 new game: scene=[[SampleScene]] launchMode=[[NewGame]] oldSeed=[[-54879632]] newSeed=[[-543085663]] seedChanged=[[True]] hasSave=[[True]]`
- `Flow5 continue old save: scene=[[SampleScene]] launchMode=[[ContinueSave]] savedSeed=[[-54879632]] continuedSeed=[[-54879632]] seedRestored=[[True]]`
- `Flow5 continue state retry: scene=[SampleScene] pos=[(0.50, 33.59, 0.00)] expected=[(0.50, 33.59, 0.00)] hp=[75] expected=[75] spawnTimer=[9.87] expected=[9.87] seed=[-54879632] expectedSeed=[-54879632] hasSave=[True]`

### 流程 6: 损坏存档容错
- `Flow6 corrupt setup: scene=[[MainMenu]] hasSave=[[True]] continueActiveSelf=[[True]] launchMode=[[NewGame]] fileBytes=[[12]]`
- `Flow6 corrupt continue: scene=[[SampleScene]] launchMode=[[NewGame]] worldFound=[[True]] worldSeed=[[-543085663]] playerFound=[[True]] warningCount=[[2]] errorCount=[[0]] hasSave=[[True]]`
- `Flow6 cleanup: hasSave=[[False]]`

### Console
- 自测流程内 `errorCount=0`。
- 损坏存档流程产生 `warningCount=2`，为预期 warning: `SaveSystem.Load()` 解析失败，以及 `GameManager` fallback 到新游戏。

## Claude 审查重点
- `GameManager` 是否只负责 launch mode 判断和调用 `GameStateSnapshot.Apply()`，没有绕过 018 的 Apply 顺序。
- `[DefaultExecutionOrder(100)]` 是否足以保证 Continue Apply 晚于 `WorldGenerator.Start()`。
- `MainMenuController` 的 Continue 按钮绑定、显示条件和 Start Game 防御性重置 `PendingLaunchMode` 是否正确。
- `PauseMenuController.OnSaveClicked()` 是否在暂停态 `timeScale=0` 下仍能保存并用 `WaitForSecondsRealtime` 恢复按钮文案。
- MainMenu / SampleScene 的 serialized references 是否完整，UI 文案是否全英文且 LiberationSans SDF 字符命中。
- `PlayerHealth.RestoreState()` 的回血计时器处理是否符合读档后保持存档 HP 的预期。

## 已知说明
- MCP Play Mode 后台验证中，读取 Continue 结果时会在点击 Continue 后立即设置 `Time.timeScale=0` 并 `EditorApplication.Step()`，用于避免物理、敌人生成器或回血系统在检查前继续推进状态。
- 流程 2 保存位置使用当前生成后的玩家落点 `(0.50, 33.59, 0.00)`，避免把玩家放到空中后物理系统改写位置。
- 流程 6 自测结束调用 `SaveSystem.Delete()` 清理了临时损坏存档文件。
- 工作区存在与本任务无关的未跟踪武器贴图资源，未纳入本次提交。
