# 018-save-system

## 任务信息
- 任务: 018 - 存档系统数据结构与服务
- 规格书: `docs/tasks/018-save-system.md`
- 分支: `feature/018-save-system`

## 本次完成内容
- 新增存档数据结构 `SaveData`，包含 Player / Inventory / World / Spawner 子结构。
- 新增 `SaveSystem` 静态服务:
  - `SaveFilePath = Application.persistentDataPath/save.json`
  - `HasSave()` / `Save()` / `Load()` / `Delete()`
  - 保存时先写 `save.json.tmp`，已有存档时使用 `File.Replace`
  - 文件不存在时 `Load()` 返回 `null`
  - JSON 损坏时返回 `null` 并 `LogWarning`
  - Version 不匹配时 `LogWarning`，但继续尝试加载
- 新增 `GameStateSnapshot`:
  - `Capture()` 聚合 World / Player / Inventory / Spawner
  - `Apply(SaveData)` 按 World -> Player -> Inventory -> Spawner 顺序恢复
  - 通过 `Object.FindFirstObjectByType<T>()` 获取场景模块
- 修改世界模块:
  - `WorldGenerator.Seed`
  - `WorldGenerator.GenerateWorldWithSeed(int seed)`
  - 重生世界前清空 Tilemap，避免读档残留旧 tile
  - `TileManager` 使用 `HashSet<Vector2Int>` 跟踪玩家编辑差量
  - `TileManager.EnumerateChanges()` / `ApplyTileChanges()` / `ClearChangeTracking()`
- 修改状态模块:
  - `Inventory.CreateSnapshot()` / `RestoreFromSnapshot()`
  - `EnemySpawner.SpawnTimer`
  - `PlayerHealth.RestoreState()`
  - `PlayerController.FacingRight` / `RestoreState()`
- 更新 `SampleScene` 中 Player 的 `Inventory` 组件，绑定 `Assets/Data/ItemDatabase.asset`，确保读档时可通过 ItemId 找回 ItemData。

## 变更文件清单
- `Assets/Scripts/Save/SaveData.cs`
- `Assets/Scripts/Save/SaveData.cs.meta`
- `Assets/Scripts/Save/SaveSystem.cs`
- `Assets/Scripts/Save/SaveSystem.cs.meta`
- `Assets/Scripts/Save/GameStateSnapshot.cs`
- `Assets/Scripts/Save/GameStateSnapshot.cs.meta`
- `Assets/Scripts/World/WorldGenerator.cs`
- `Assets/Scripts/World/TileManager.cs`
- `Assets/Scripts/Items/Inventory.cs`
- `Assets/Scripts/Enemies/EnemySpawner.cs`
- `Assets/Scripts/Player/PlayerHealth.cs`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scenes/SampleScene.unity`
- `docs/codex-reports/018-save-system.md`

## 分模块验证日志
- SaveSystem 边界验证:
  - `SaveSystem missing: hasSave=False loadNull=True`
  - `SaveSystem save/load: hasSave=True loadedVersion=1 hp=70 fileExists=True`
  - `SaveSystem version mismatch load: loadedNull=False version=99`
  - `SaveSystem corrupt load: loadedNull=True`
- WorldGenerator / TileManager 差量验证:
  - `World module: scene=SampleScene seed=424242 setA=True setB=True before=Stone changes=2`
  - `World module: regenerated=Stone afterApplyA=Air afterApplyB=Dirt trackedAfterApply=2`
- Inventory 快照验证:
  - `Inventory module: slots=40 selected=5`
  - `Inventory module: slot0=1:7 slot5=3:12 selectedAfter=5`
- Unity Editor 编译:
  - `isCompilationSuccessful=True`
  - `isExecutionSuccessful=True`
  - `Final compile check: isPlaying=False scene=SampleScene timeScale=1 savePath=C:/Users/Administrator/AppData/LocalLow/DefaultCompany/Perraria\save.json`

## 完整往返自测脚本
```csharp
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();
        PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        Inventory inventory = Object.FindFirstObjectByType<Inventory>();
        TileManager tileManager = Object.FindFirstObjectByType<TileManager>();
        WorldGenerator worldGenerator = Object.FindFirstObjectByType<WorldGenerator>();
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Data/ItemDatabase.asset");
        ItemData dirt = database.GetItemById(1);
        ItemData stone = database.GetItemById(3);

        result.Log(
            "roundtrip script start: isPlaying={0} seed={1} playerFound={2} inventoryFound={3} tileManagerFound={4} spawnerFound={5}",
            EditorApplication.isPlaying,
            worldGenerator.Seed,
            playerController != null,
            inventory != null,
            tileManager != null,
            spawner != null);

        SaveSystem.Delete();
        playerController.RestoreState(new Vector3(123f, 45f, 0f), false);
        playerHealth.RestoreState(100, 100);
        playerHealth.TakeDamage(30);
        inventory.RestoreFromSnapshot(null);
        int dirtRemaining = inventory.AddItem(dirt, 7);
        int stoneRemaining = inventory.AddItem(stone, 12);
        inventory.SelectHotbar(1);

        Vector3Int[] airTiles =
        {
            new(10, 20, 0),
            new(11, 20, 0),
            new(12, 20, 0),
            new(13, 20, 0),
            new(14, 20, 0)
        };
        Vector3Int[] dirtTiles =
        {
            new(15, 20, 0),
            new(16, 20, 0),
            new(17, 20, 0)
        };

        foreach (Vector3Int tile in airTiles)
        {
            tileManager.SetBlock(tile, BlockType.Air);
        }

        foreach (Vector3Int tile in dirtTiles)
        {
            tileManager.SetBlock(tile, BlockType.Dirt);
        }

        spawner.SpawnTimer = 12.34f;

        SaveData snap1 = GameStateSnapshot.Capture();
        SaveSystem.Save(snap1);
        SaveData loaded = SaveSystem.Load();

        playerController.RestoreState(Vector3.zero, true);
        playerHealth.RestoreState(100, 100);
        inventory.RestoreFromSnapshot(null);
        tileManager.ClearChangeTracking();
        foreach (Vector3Int tile in airTiles.Concat(dirtTiles))
        {
            tileManager.SetBlock(tile, BlockType.Stone);
        }

        spawner.SpawnTimer = 0.5f;

        GameStateSnapshot.Apply(loaded);

        ItemStack slot0 = inventory.GetSlot(0);
        ItemStack slot1 = inventory.GetSlot(1);
        BlockType tile10 = tileManager.GetBlock(new Vector3Int(10, 20, 0));
        BlockType tile15 = tileManager.GetBlock(new Vector3Int(15, 20, 0));
        int trackedEdits = tileManager.EnumerateChanges().Count();
        long fileSize = new FileInfo(SaveSystem.SaveFilePath).Length;

        result.Log("position={0} expected (123.00, 45.00, 0.00)", playerController.transform.position);
        result.Log("facingRight={0} expected False", playerController.FacingRight);
        result.Log("hp={0}/{1} expected 70/100", playerHealth.CurrentHealth, playerHealth.MaxHealth);
        result.Log("inventory slot 0 itemId={0} count={1} expected 1/7", slot0.Item != null ? slot0.Item.ItemId : -1, slot0.Count);
        result.Log("inventory slot 1 itemId={0} count={1} expected 3/12", slot1.Item != null ? slot1.Item.ItemId : -1, slot1.Count);
        result.Log("inventory selected={0} expected 1 dirtRemaining={1} stoneRemaining={2}", inventory.SelectedHotbarIndex, dirtRemaining, stoneRemaining);
        result.Log("tile (10,20)={0} expected Air", tile10);
        result.Log("tile (15,20)={0} expected Dirt", tile15);
        result.Log("spawnTimer={0} expected 12.34", spawner.SpawnTimer.ToString("F2"));
        result.Log("file size bytes={0} expected <10000", fileSize);
        result.Log("world edits in JSON={0} expected 8 trackedAfterApply={1}", loaded.World.PlayerEdits.Count, trackedEdits);
        result.Log("hasSave={0} loadedNull={1} loadedVersion={2}", SaveSystem.HasSave(), loaded == null, loaded != null ? loaded.Version : -1);
    }
}
```

## 完整往返自测日志
- Play Mode 准备:
  - `Final ready: isPlaying=True scene=SampleScene seed=-1588184896`
- 往返脚本输出:
  - `roundtrip script start: isPlaying=True seed=-1588184896 playerFound=True inventoryFound=True tileManagerFound=True spawnerFound=True`
  - `position=(123.00, 45.00, 0.00) expected (123.00, 45.00, 0.00)`
  - `facingRight=False expected False`
  - `hp=70/100 expected 70/100`
  - `inventory slot 0 itemId=1 count=7 expected 1/7`
  - `inventory slot 1 itemId=3 count=12 expected 3/12`
  - `inventory selected=1 expected 1 dirtRemaining=0 stoneRemaining=0`
  - `tile (10,20)=Air expected Air`
  - `tile (15,20)=Dirt expected Dirt`
  - `spawnTimer=12.34 expected 12.34`
  - `file size bytes=4854 expected <10000`
  - `world edits in JSON=8 expected 8 trackedAfterApply=8`
  - `hasSave=True loadedNull=False loadedVersion=1`
- 测试后清理:
  - `Cleanup save file: hasSave=False`
- Console 检查:
  - `errorCount=0`
  - `warningCount=1`
  - 唯一 warning 来自 Unity AI Assistant 包账号 API: `Account API did not become accessible within 30 seconds...`
  - 存档系统最终往返流程没有 error / exception / 序列化 warning。

## Claude 审查重点
- `SaveSystem` 是否只负责 JSON 和文件读写，不硬编码具体模块字段。
- `SaveSystem.Save()` 是否先写 `.tmp`，已有存档时通过 `File.Replace` 覆盖。
- `SaveSystem.Load()` 对不存在文件、损坏 JSON、Version mismatch 的行为是否符合 spec。
- `GameStateSnapshot.Apply()` 顺序是否为 World -> Player -> Inventory -> Spawner。
- `WorldGenerator.GenerateWorldWithSeed()` 是否会清空 Tilemap 并用 seed 重生基础地形。
- `TileManager` 是否只保存 `_playerEdits` 差量，而不是 80000 个 tile 全量。
- `Inventory` 是否使用字符串 `ItemId` 存档，读档时通过 `ItemDatabase.GetItemById(int)` 恢复。
- `SampleScene` 中 Inventory 绑定 `ItemDatabase.asset` 是否合理且无 UI 接入。

## 已知说明
- `ItemData` 当前实际接口是 `int ItemId`，存档结构按 spec 使用 `string ItemId`；实现中保存为 `ItemId.ToString()`，空槽保存 `""`，读档时 `int.TryParse` 后查 `ItemDatabase`。
- 任务 spec 的文件清单写的是 `Assets/Scripts/Inventory/Inventory.cs`，项目实际路径是 `Assets/Scripts/Items/Inventory.cs`。
- Unity MCP 在本任务中曾因 domain reload 后 relay 卡住而超时；已重启 Unity Editor 后完成编译和 MCP 往返自测。
- 自测会临时写入 `Application.persistentDataPath/save.json`，最终已调用 `SaveSystem.Delete()` 清理。
