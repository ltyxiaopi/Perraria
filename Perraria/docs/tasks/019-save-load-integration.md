# 任务 019 - 存档/读档接入 UI

## 目标
把任务 018 的 `SaveSystem` 接入主菜单和暂停菜单：
- 暂停菜单「保存游戏」按钮启用，点击 → `SaveSystem.Save(GameStateSnapshot.Capture())`
- 主菜单根据存档存在性显示「继续游戏」按钮（无存档则隐藏）
- 点「继续游戏」 → 加载 SampleScene → 场景启动后调用 `GameStateSnapshot.Apply(SaveSystem.Load())` 还原状态
- 点「开始游戏」（新游戏） → 加载 SampleScene → 不调用 Apply（保留默认随机 seed 生成）

## 接口签名

```csharp
// GameManager 是已有或本任务新增的入口控制器（建议挂在 SampleScene 一个空 GameObject）
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    // 静态字段：上一次主菜单选择的"启动模式"，跨场景传递
    public static GameLaunchMode PendingLaunchMode = GameLaunchMode.NewGame;

    // 在所有依赖模块（WorldGenerator、Player、Inventory、Spawner）Awake 完成后调用
    private void Start();        // 检查 PendingLaunchMode，若为 ContinueSave 则 Apply 存档
}

public enum GameLaunchMode
{
    NewGame,
    ContinueSave,
}
```

主菜单和暂停菜单按钮的接通：

```csharp
// MainMenuController 扩展
[SerializeField] private Button _continueButton;   // 任务 016 没加，本任务新增

private void Start()
{
    _continueButton.gameObject.SetActive(SaveSystem.HasSave());
}

public void OnContinueClicked()
{
    GameManager.PendingLaunchMode = GameLaunchMode.ContinueSave;
    SceneManager.LoadScene(_gameSceneName);
}

public void OnStartClicked()  // 已有，确保设置 NewGame
{
    GameManager.PendingLaunchMode = GameLaunchMode.NewGame;
    SceneManager.LoadScene(_gameSceneName);
}
```

```csharp
// PauseMenuController 扩展
public void OnSaveClicked()
{
    SaveData snap = GameStateSnapshot.Capture();
    SaveSystem.Save(snap);
    // 可选：UI 反馈（按钮文字临时变成 "已保存 ✓" 1 秒后还原）
}
```

## 依赖
- 任务 016 MainMenu、任务 017 PauseMenu、任务 018 SaveSystem + GameStateSnapshot

## 文件清单
- `Assets/Scripts/Core/GameManager.cs` — 新增（如已存在则扩展），含 `PendingLaunchMode` 静态字段和 `Start()` 加载逻辑
- 修改 `Assets/Scripts/UI/MainMenuController.cs` —— 加「继续游戏」按钮和点击处理
- 修改 `Assets/Scripts/UI/PauseMenuController.cs` —— 启用「保存游戏」按钮并接通 Save
- MCP 场景配置：
  - SampleScene 加 `GameManager` 物体（如未加）
  - MainMenu 场景加「继续游戏」按钮（在「开始游戏」下方），绑定 `OnContinueClicked`
  - SampleScene 暂停菜单「保存游戏」按钮 `interactable = true`，绑定 `OnSaveClicked`

## 启动流程

```
[MainMenu]
  - Start() 检查 SaveSystem.HasSave()，决定是否显示「继续游戏」
  - 点「开始游戏」: PendingLaunchMode = NewGame, LoadScene("SampleScene")
  - 点「继续游戏」: PendingLaunchMode = ContinueSave, LoadScene("SampleScene")
  - 点「退出游戏」: 退出

[SampleScene]
  - WorldGenerator.Awake() / Start() 等正常初始化（默认随机 seed 生成）
  - GameManager.Start() 在所有 Awake 之后执行：
    - if (PendingLaunchMode == ContinueSave):
        var data = SaveSystem.Load();
        if (data != null) GameStateSnapshot.Apply(data);   // 内部会用 data.World.Seed 重生成地形
    - else: 不做任何事（新游戏使用 WorldGenerator 默认随机 seed）

[暂停中]
  - 点「保存游戏」: SaveSystem.Save(GameStateSnapshot.Capture())
  - 点「返回主菜单」: Time.timeScale = 1, LoadScene("MainMenu")
```

## 验收标准
- [ ] **新游戏流程**：MainMenu → 开始游戏 → SampleScene 正常生成（无存档时「继续游戏」按钮不显示）
- [ ] **保存流程**：游戏中按 ESC → 点「保存游戏」 → `Application.persistentDataPath/save.json` 写入；
      控制台无错误
- [ ] **保存后回主菜单**：保存后点「返回主菜单」 → MainMenu 出现「继续游戏」按钮
- [ ] **读档流程**：MainMenu → 继续游戏 → SampleScene 加载，玩家位置 / HP / 背包 / 地形修改全部还原
- [ ] **读档后保存计时器恢复**：EnemySpawner 的计时器从存档值继续，不会一加载就刷一波
- [ ] **新游戏覆盖旧档**：有存档时点「开始游戏」生成新世界，`save.json` 文件**不被自动删除**
      （玩家仍可点「继续游戏」回到旧档；保存只在玩家显式点保存时发生）
- [ ] **GameManager 启动时机**：必须确保 `WorldGenerator.Start()` 已生成默认地形之后再 Apply，
      否则 Apply 里的 `GenerateWorldWithSeed` 会和默认 Start 冲突——
      可通过 ScriptExecutionOrder 调优 GameManager 在 WorldGenerator 之后执行，
      或者 GameManager.Start() 用一帧延迟（`yield return null`）

## 注意事项
- **静态字段跨场景传值**：`GameManager.PendingLaunchMode` 是 `static`，场景切换不丢；
  但要注意首次启动时默认值（`NewGame`）符合预期
- **Apply 顺序**：`GameStateSnapshot.Apply` 内部应该按 World → Player → Inventory → Spawner 顺序，
  避免玩家先被瞬移到旧位置但地形还没生成
- **重生地形**：Apply 时 `WorldGenerator.GenerateWorldWithSeed(data.World.Seed)` 会清除当前 tilemap 重画——
  确认 `WorldGenerator` 的"清空 + 重画"是幂等的，不会留下旧 tile 残影
- **保存后 UI 反馈**（可选但推荐）：「保存游戏」按钮文字临时变成 `已保存 ✓`，1 秒后变回 `保存游戏`；
  实现简单：按钮文字组件 + 协程
- **读档失败处理**：若 `SaveSystem.Load()` 返回 null（文件被手动删了 / JSON 损坏），
  GameManager 应回退为 NewGame 行为并 `Debug.LogWarning`，不让游戏卡死
- **不实现**：
  - 删除存档按钮 / UI
  - 多存档管理
  - 自动保存（退出 / 死亡时）
  - 存档损坏的 UI 提示对话框

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/019-save-load-integration.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。

**自测要求**：交付记录里必须演示完整路径：
新游戏 → 玩 → 保存 → 返回主菜单 → 继续游戏 → 验证状态还原。
