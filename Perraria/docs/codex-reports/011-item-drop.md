# 任务 011 交付记录 - 掉落物实体

## 交付内容

- 新增 `Assets/Scripts/Items/ItemDrop.cs`
- 新增 `Assets/Prefabs/ItemDrop.prefab`
- 修改 `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- 在 `SampleScene` 中将 `PlayerBlockInteraction._itemDropPrefab` 绑定到 `ItemDrop.prefab`

## 实现说明

### ItemDrop

`ItemDrop` 负责场景掉落物的显示、浮动和自动拾取：

- `Initialize(ItemData, int)` 设置图标、数量、生成时间和浮动基准点
- `Update()` 使用 sin 波实现上下浮动
- `OnTriggerStay2D()` 使用 `other.GetComponent<Inventory>()` 检测玩家背包
- 拾取延迟 `0.5s`
- 支持全拾取 / 部分拾取 / 背包满时保留在地面

关键位置：

- `Assets/Scripts/Items/ItemDrop.cs:6-19` Inspector 字段与公开属性
- `Assets/Scripts/Items/ItemDrop.cs:21-30` 浮动动画
- `Assets/Scripts/Items/ItemDrop.cs:32-43` 初始化逻辑
- `Assets/Scripts/Items/ItemDrop.cs:45-77` 拾取逻辑

### PlayerBlockInteraction

`PlayerBlockInteraction` 挖掘完成后的掉落入口已从直接入包改为生成掉落物实体：

- 新增字段 `ItemDrop _itemDropPrefab`
- `ContinueMining()` 中将 `AddDropToInventory(minedCell)` 替换为 `SpawnItemDrop(minedCell)`
- `SpawnItemDrop()` 在方块中心实例化预制体并调用 `Initialize(dropItem, 1)`

关键位置：

- `Assets/Scripts/Player/PlayerBlockInteraction.cs:15` `_itemDropPrefab`
- `Assets/Scripts/Player/PlayerBlockInteraction.cs:154-172` 挖掘完成后的调用替换
- `Assets/Scripts/Player/PlayerBlockInteraction.cs:286-302` `SpawnItemDrop()`

## Prefab 与场景配置

### ItemDrop Prefab

已创建 `Assets/Prefabs/ItemDrop.prefab`，配置如下：

- `SpriteRenderer` 存在，`Order in Layer = 10`
- `Rigidbody2D` 为 `Kinematic`
- `CircleCollider2D` 为 `Trigger`
- `CircleCollider2D.radius = 1.5`
- `ItemDrop._spriteRenderer` 已绑定到自身 `SpriteRenderer`

### 场景绑定

`SampleScene` 中 `PlayerBlockInteraction._itemDropPrefab` 已完成绑定：

- `Assets/Scenes/SampleScene.unity:6357`

## Play 模式验收

已在 Play 模式做自动化验收。验证器在运行时：

1. 清空背包
2. 找到玩家附近一个可掉落的方块单元
3. 通过 `PlayerBlockInteraction` 的掉落生成路径生成掉落物
4. 检查图标、浮动、延迟拾取和自动入包

验收结果：

- `targetCell=(-1, 40, 0)`
- `targetBlockType=Dirt`
- `dropSpawned=True`
- `dropCount=1`
- `dropSpriteMatches=True`
- `bobMoved=True`
- `existsDuringDelay=True`
- `countDuringDelay=0`
- `pickupBlockedByDelay=True`
- `existsAfterDelay=False`
- `countAfterPickup=1`
- `pickedUpAfterDelay=True`

说明：

- 自动化验收调用的是 `PlayerBlockInteraction` 挖掘完成后的同一掉落生成路径，以验证掉落物实体和拾取链路；没有模拟鼠标长按采矿输入本身。

## Console 检查

- 未发现由本任务引入的新 Unity Console Error
- 当前仅存在一个既有 Warning：
  - `com.unity.ai.assistant` 的 `Account API did not become accessible within 30 seconds`
  - 属于编辑器联网告警，不是本任务代码引入

## 备注

- 工作区中的 `Assets/Scenes/SampleScene.unity` 在本任务开始前已经存在其他未提交改动；本次只在其中追加了 `PlayerBlockInteraction._itemDropPrefab` 的序列化绑定，没有回退其他现有修改。
