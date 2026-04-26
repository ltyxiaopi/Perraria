# 任务 018 - 存档系统数据结构与服务

## 目标
设计可扩展的存档数据结构，提供 `SaveSystem` 静态服务做 JSON 序列化和文件读写。
**本任务只实现数据层和服务层，不接 UI**——UI 接入由任务 019 完成。
本任务提交后，Codex 应通过 MCP 编写自测代码验证序列化往返正确。

## 接口签名

存档采用「按模块分类的子结构」组织，未来加新模块（敌人、时间、天气）只需扩展根 `SaveData` 字段：

```csharp
// 1) 根存档对象
[System.Serializable]
public sealed class SaveData
{
    public int Version = 1;          // 存档版本号，便于以后做兼容
    public string SavedAtIso;        // ISO 8601 时间字符串，调试用
    public PlayerSaveData Player;
    public InventorySaveData Inventory;
    public WorldSaveData World;
    public SpawnerSaveData Spawner;
    // 未来扩展位（不要现在就加，但留好命名空间）：
    // public EnemiesSaveData Enemies;
    // public TimeSaveData Time;
}

// 2) Player 子结构
[System.Serializable]
public sealed class PlayerSaveData
{
    public Vector3 Position;
    public int CurrentHealth;
    public int MaxHealth;
    public bool FacingRight;
}

// 3) Inventory 子结构（连同快捷栏 + 选中槽）
[System.Serializable]
public sealed class InventorySaveData
{
    public List<InventorySlotSaveData> Slots;   // 长度 = Inventory.TotalSlots
    public int SelectedHotbarIndex;
}

[System.Serializable]
public sealed class InventorySlotSaveData
{
    public string ItemId;     // 空槽位用 ""，避免 null
    public int Count;
}

// 4) World 子结构（按 seed + 玩家修改 diff 节省体积）
[System.Serializable]
public sealed class WorldSaveData
{
    public int Seed;                                      // 世界生成种子
    public List<WorldTileChange> PlayerEdits;             // 玩家挖掉/放置的方块差量
}

[System.Serializable]
public sealed class WorldTileChange
{
    public int X;             // WorldData 数组坐标
    public int Y;
    public int BlockType;     // (int)BlockType 枚举值，0 = Air
}

// 5) Spawner 子结构
[System.Serializable]
public sealed class SpawnerSaveData
{
    public float SpawnTimer;     // 距离下一次刷怪的剩余秒数
}

// 6) 静态服务
public static class SaveSystem
{
    public const string SaveFileName = "save.json";
    public static string SaveFilePath { get; }   // = Path.Combine(Application.persistentDataPath, SaveFileName)

    public static bool HasSave();                    // 文件存在性
    public static void Save(SaveData data);          // JSON 序列化 + 写文件（覆盖）
    public static SaveData Load();                   // 读文件 + JSON 反序列化；文件不存在返回 null
    public static void Delete();                     // 调试用：删除存档
}
```

## 关键改动：让现有模块支持持久化

为了让 SaveData 能正确捕获和恢复状态，需要给现有模块添加最小限度的"快照 / 还原"接口：

### WorldGenerator
当前 `_seedX / _seedY = Random.Range(0, 10000)`，**没有暴露 seed**。本任务需要：
- 把生成种子改成单一 `int Seed` 字段：`_seedX = (Seed % 10000); _seedY = ((Seed / 10000) % 10000);`
  或者直接用 `Random.InitState(Seed)` 后再 `Random.Range`
- 新增 `public int Seed { get; private set; }`
- 新增 `public void GenerateWorldWithSeed(int seed)`——读档时用指定 seed 重新生成

### TileManager
- 新增 `IEnumerable<WorldTileChange> EnumerateAllTiles()` —— 遍历 WorldData，与初始程序生成相比的差量；
  **简化方案**：保存全部 200×400=80000 个 tile 体积太大，所以用差量。
  实现方式：`WorldGenerator` 生成完毕后调用 `TileManager.SnapshotInitialState()` 拷贝一份 baseline，
  存档时遍历 WorldData 与 baseline 对比，只输出不同的格子
- 或者更简单：让 `TileManager` 维护一个 `HashSet<Vector2Int>` 跟踪 SetBlock 调用过的位置——
  **这个方案更优**，避免 baseline 内存占用
- 新增 `void ApplyTileChanges(IEnumerable<WorldTileChange> changes)` —— 读档时用

### Inventory
- 现有 `ItemStack` / 槽位结构应该已经能直接序列化；如果 `Inventory` 内部用 ScriptableObject 引用 ItemData，
  存档时需要存 `ItemData.Id` 字符串，读档时通过 `ItemDatabase.GetItemById(id)` 查回
- 新增 `InventorySaveData CreateSnapshot()` 和 `void RestoreFromSnapshot(InventorySaveData)`

### EnemySpawner
- 暴露 `_spawnTimer` 的 getter/setter（或包装方法）

### PlayerHealth / PlayerController
- `PlayerHealth` 已有 `CurrentHealth / MaxHealth / Heal / TakeDamage`，缺一个直接 set 方法用于读档：
  新增 `void RestoreState(int currentHealth, int maxHealth)` 内部强制设置
- `PlayerController` 朝向：通过 `SpriteRenderer.flipX` 推断或暴露 `bool FacingRight { get; }`

## 依赖
- 任务 010 PlayerHealth、任务 008 Inventory、任务 003 WorldGenerator/TileManager、任务 014 EnemySpawner

## 文件清单
- `Assets/Scripts/Save/SaveData.cs` — 新增（含所有子结构）
- `Assets/Scripts/Save/SaveSystem.cs` — 新增（静态服务）
- `Assets/Scripts/Save/GameStateSnapshot.cs` — 新增（提供 `Capture()` / `Apply(SaveData)` 静态方法，
  集中调用各模块的 snapshot/restore 接口；这样未来扩展只改这个文件）
- 修改 `Assets/Scripts/World/WorldGenerator.cs` —— 暴露 `Seed`、新增 `GenerateWorldWithSeed`
- 修改 `Assets/Scripts/World/TileManager.cs` —— 跟踪玩家修改、提供差量枚举/应用接口
- 修改 `Assets/Scripts/Inventory/Inventory.cs` —— 新增 `CreateSnapshot/RestoreFromSnapshot`
- 修改 `Assets/Scripts/Enemies/EnemySpawner.cs` —— 暴露 `SpawnTimer` 读写
- 修改 `Assets/Scripts/Player/PlayerHealth.cs` —— 新增 `RestoreState`
- 修改 `Assets/Scripts/Player/PlayerController.cs` —— 暴露 `FacingRight`（如未暴露）

## 验收标准
- [ ] `SaveSystem.Save(data)` 在 `Application.persistentDataPath/save.json` 写入合法 JSON 文件
- [ ] `SaveSystem.Load()` 文件不存在时返回 null（不抛异常）
- [ ] `SaveSystem.HasSave()` 与文件存在状态一致
- [ ] **JSON 往返一致**：通过 MCP 写自测代码：
  1. 玩家移动到某位置、修改若干 tile、捡几个物品、消耗一些 HP、刷一会儿怪
  2. `var snap = GameStateSnapshot.Capture();`
  3. `SaveSystem.Save(snap);`
  4. 重新加载场景（或重置游戏状态）
  5. `var loaded = SaveSystem.Load();`
  6. `GameStateSnapshot.Apply(loaded);`
  7. 验证：玩家位置、HP、背包、地形修改、刷怪计时器全部还原
- [ ] World 差量正确：未被玩家修改的 tile 不进 SaveData，体积 <10KB（对照全量 80000 tile 应该明显更小）
- [ ] 读档时 `WorldGenerator` 使用存档 seed 重生成基础地形，`TileManager.ApplyTileChanges` 覆盖玩家修改
- [ ] 改变 `SaveData.Version` 到非 1 后再 `Load()` 应该警告但暂不阻止（为以后版本升级留扩展空间）
- [ ] 控制台无序列化警告 / 错误

## 注意事项
- **JSON 库选择**：用 `JsonUtility.ToJson(data, prettyPrint: true)` / `JsonUtility.FromJson<SaveData>`，
  Unity 自带、零依赖；不要引入 Newtonsoft
- **JsonUtility 限制**：
  - 不支持 Dictionary（这就是 InventorySlotSaveData 用 List 而非 Dictionary 的原因）
  - 不支持 polymorphism、不能直接序列化抽象基类——所有字段都是具体类
  - 序列化字段必须 public 或 `[SerializeField] private`
- **可扩展性**：
  - 新增"敌人保存"时只需：(1) 加 `EnemiesSaveData` 类 (2) 在 `SaveData` 加字段 (3) 在 `GameStateSnapshot.Capture/Apply` 加分支
  - 不要在 `SaveSystem` 里硬编码"哪些字段要存"——所有字段集合都通过 `SaveData` 反射可见
- **Version 字段**：当前 = 1；未来加敌人时改 = 2，`Load` 时如果 version 比当前代码低，可以做迁移（不在本任务实现）
- **写文件原子性**：先写到 `save.json.tmp` 再 `File.Replace` 重命名，避免写一半崩溃留下损坏文件
- **不实现**：
  - 多存档槽位（决定为单存档）
  - 自动保存（决定为仅手动）
  - 敌人 / 掉落物存档（暂不保存，未来扩）
  - 加密 / 反作弊（决定为 JSON 明文）
  - UI 集成（任务 019）

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/018-save-system.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。

**自测要求**：交付记录里必须包含 MCP 自测脚本的完整往返验证日志，
证明 Capture → Save → Load → Apply 后所有字段一致。
