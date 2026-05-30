# 任务 029 - 砍树系统交付记录

## 任务信息
- 任务: `docs/tasks/029-tree-chopping.md`
- 分支: `feature/029-tree-chopping`
- 完成日期: 2026-05-30
- 执行方式: Codex 实现 + Unity MCP 自测

## 本次完成内容
- 新增 `BlockType.Wood = 4` / `BlockType.Leaves = 5`。
- `TileRegistry` 新增 Wood / Leaves tile 绑定，并创建:
  - `Assets/Art/Tilesets/Tiles/WoodTile.asset`
  - `Assets/Art/Tilesets/Tiles/LeavesTile.asset`
- 新建物品:
  - `Item_Wood`: itemId 36, `ItemType.Block`, `PlaceBlockType = Wood`, max stack 99, icon = `oak_log_side.png`
  - `Item_Sapling`: itemId 37, `ItemType.Material`, `PlaceBlockType = Air`, max stack 99, icon = `sapling_oak.png`
- `BlockDataRegistry` 新增 `DropChance`，并配置:
  - Dirt/Grass/Stone: chance 1, 原掉落不变
  - Wood: hardness 0.4, drop `Item_Wood`, chance 1
  - Leaves: hardness 0.15, drop `Item_Sapling`, chance 0.1
- `PlayerBlockInteraction.SpawnItemDrop` 接入 `DropChance` 概率分支。
- `WorldGenerator` 在 `CarveCaves(surfaceHeights)` 后、`RenderToTilemap()` 前调用 `PlantTrees(surfaceHeights)`；只使用已 `Random.InitState(seed)` 的 `UnityEngine.Random`。
- `BlockDataRegistryCsvImporter` 重建数组时保留已有 `DropChance`，避免 CSV 导入清掉 Leaves 的 0.1。
- 三张 PNG 已通过 MCP 配置为 Single Sprite、PPU 16、Point filter、Uncompressed、mipmap off、Clamp。

## 授权确认
- 素材来源: OpenGameArt `16x16 Block Texture Set`
- 页面: https://opengameart.org/content/16x16-block-texture-set
- 作者: `ARoachIFoundOnMyPillow`
- 页面标注许可证: `CC0`
- 处理: CC0 无需署名，本次未创建 `docs/credits.md`。

## 变更文件
- `Assets/Scripts/World/BlockType.cs`
- `Assets/Scripts/World/TileRegistry.cs`
- `Assets/Scripts/World/BlockDataRegistry.cs`
- `Assets/Scripts/Editor/BlockDataRegistryCsvImporter.cs`
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- `Assets/Scripts/World/WorldGenerator.cs`
- `Assets/Data/BlockDataRegistry.csv`
- `Assets/Data/BlockDataRegistry.asset`
- `Assets/Data/TileRegistry.asset`
- `Assets/Resources/ItemDatabase.asset`
- `Assets/Art/Tilesets/Tiles/WoodTile.asset`
- `Assets/Art/Tilesets/Tiles/LeavesTile.asset`
- `Assets/Data/Items/Item_Wood.asset`
- `Assets/Data/Items/Item_Sapling.asset`
- `Assets/Art/Tilesets/blocks/oak_log_side.png`
- `Assets/Art/Tilesets/blocks/oak_leaves.png`
- `Assets/Art/Tilesets/blocks/plants/sapling_oak.png`
- `docs/codex-reports/029-tree-chopping.md`
- `docs/codex-reports/029-tree-chopping-images/01-generated-oak-trees.png`
- `docs/codex-reports/029-tree-chopping-images/02-chopped-tree-drops-inventory.png`

## 验证结果
- 编译/Console:
  - `AssetDatabase.Refresh()` 编译成功。
  - 最终清空 Console 后重新刷新，`Unity_GetConsoleLogs(Error,Warning)` 返回 `errorCount=0`, `warningCount=0`。
- Registry + CSV 保留:
  - `Wood hardness=0.4, drop=Item_Wood, chance=1`
  - `Leaves hardness=0.15, drop=Item_Sapling, chanceBeforeCsv=0.1, chanceAfterCsv=0.1`
  - `Dirt/Grass/Stone chances=1/1/1`
- 世界生成确定性:
  - seed `29029`
  - 第一次树方块数 `329`
  - 第二次树方块数 `329`
  - 快照对比 `equal=True`
  - treeBases `26`
  - sample `x:height = 10:6,44:4,61:6,98:6,102:5,111:6,149:5,153:5`
- 存档零代码改动验证:
  - 通过 `TileManager.SetBlock` 模拟玩家砍掉 `(10,132)` 和 `(10,133)` 两格。
  - 保存的 tile changes 数量 `2`。
  - 同 seed 重生成后 `ApplyTileChanges(changes)`，重放快照 `replayMatches=True`。
- 掉落统计:
  - `SpawnItemDrop` 树叶尝试 `200` 次，掉 `Item_Sapling` `26` 次，命中率 `13%`，配置概率 `0.1`。
  - `SpawnItemDrop` 木块尝试 `25` 次，掉 `Item_Wood` `25` 次。
  - Dirt/Grass/Stone 单次掉落回归: `1/1/1`。
- MCP 截图:
  - `docs/codex-reports/029-tree-chopping-images/01-generated-oak-trees.png`
  - `docs/codex-reports/029-tree-chopping-images/02-chopped-tree-drops-inventory.png`

## Claude Code 重点检查项
- `WorldGenerator.PlantTrees` 的随机调用顺序和调用位置是否符合存档确定性要求。
- `BlockDataRegistryCsvImporter` 是否确实保留 `DropChance`。
- `PlayerBlockInteraction` 的概率分支是否只影响配置为小于 1 的掉落。
- 没有修改存档系统代码。

## 已知说明
- 斧头仍按规格保留为 Weapon，不参与挖掘；木/叶通过低硬度支持徒手或镐子砍。
- 树叶不会衰减，砍断树干后 floating 块按规格保留。
- 实现过程中 Unity Connect 曾产生账号 token 网络错误；最终验证前已清空 Console，重新刷新后 Error/Warning 为 0/0。
