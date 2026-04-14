# Codex 交付记录 - 任务 005 方块挖掘耗时系统

## 任务信息
- 任务编号: 005
- 任务规格: `docs/tasks/005-mining-duration.md`
- 完成时间: 2026-04-14
- 执行方式: 直接编写脚本/Shader + Unity MCP 配置场景和资产

## 本次完成内容

### 规格书调整
- 将 `docs/tasks/005-mining-duration.md` 从“整格 Tilemap alpha 简化方案”改回“真正从顶部到底部透明”的实现方案
- 视觉方案改为:
  - 挖掘开始时临时隐藏原 tile
  - 使用 `MiningProgressOverlay` 显示目标方块自己的 sprite
  - 通过 Shader 按 `_Progress` 裁掉 sprite 顶部区域
  - 挖掘中断或完成时恢复原 tile 颜色并隐藏 overlay

### 脚本文件
- `Assets/Scripts/World/BlockDataRegistry.cs`
  - 保持方块硬度表实现不变，继续提供 `GetHardness(BlockType type)`
- `Assets/Scripts/World/TileManager.cs`
  - 保留 `SetTileColor()` / `ResetTileColor()`，用于临时隐藏原 tile 与恢复颜色
  - 新增 `GetBlockSprite(Vector3Int tilemapPos)`，供 overlay 读取目标格子的 sprite
- `Assets/Scripts/Player/MiningProgressOverlay.cs`
  - 保留 overlay 组件
  - 使用 `MaterialPropertyBlock` 更新 `_Progress`
  - 通过 `SetSprite()` 接收目标方块 sprite
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`
  - 恢复 `_miningOverlay` 字段
  - `Awake()` / `Start()` 中增加兜底查找: 若 Inspector 未绑定，则自动从玩家子对象中查找 `MiningProgressOverlay`
  - 开始挖掘时:
    - 读取目标格子 sprite
    - 将原 tile 设为透明
    - 显示 overlay 并设置 `_Progress=0`
  - 持续挖掘时:
    - 每帧刷新 overlay 位置
    - 每帧更新 `_Progress`
  - 挖掘完成时:
    - 先恢复原 tile 颜色
    - 再隐藏 overlay / 清空状态
    - 最后 `SetBlock(cell, Air)`
  - 挖掘中断时:
    - 恢复原 tile 颜色
    - 隐藏 overlay
    - 重置进度
  - 修复挖掘完成分支:
    - 之前 `ResetMining()` 会先清空 `_miningCell`
    - 导致后续 `SetBlock(_miningCell, Air)` 命中错误坐标
    - 现已改为先缓存 `minedCell`，再 `ResetMining()`，最后 `SetBlock(minedCell, Air)`
  - 调整高亮逻辑:
    - 开始挖掘当前格子时主动 `ClearHighlight()`
    - 挖掘同一格子期间不再绘制 `HighlightTile`
    - 避免高亮遮盖挖掘可视效果
- `Assets/Scripts/Editor/BlockDataRegistryCsvImporter.cs`
  - 修复 `ImportDefault()` 调用缺少 `registry` 参数导致的编译错误
  - 该错误会阻塞 Unity 脚本域刷新，因此一并修正，确保 005 的场景与序列化检查可以继续进行
- `Assets/Scripts/Player/MiningProgressOverlay.cs`
  - 补充 `EnsureInitialized()` 兜底初始化
  - `Show()` / `SetSprite()` / `Hide()` / `SetProgress()` 都会确保 `_spriteRenderer` 与 `_propertyBlock` 已创建
  - 避免运行期或 MCP 调试时因 `MaterialPropertyBlock` 尚未初始化而抛出空引用
- `Assets/Scripts/World/TileManager.cs`
  - 在 `SetTileColor()` 中先执行 `SetTileFlags(tilemapPos, TileFlags.None)`
  - 根因是地形 Tile 资源启用了 `LockColor`
  - 如果不先解锁，`SetColor(..., transparent)` 对原 tile 不生效，视觉上会表现为“overlay 顶部透明，但底下仍露出完整原 tile”

### Shader / 材质文件
- `Assets/Art/Shaders/MiningProgress.shader`
  - 最终改为基于 URP 17 官方 `Sprite-Unlit-Default` 结构的 2D Sprite Unlit Shader
  - 在官方路径上追加自定义逻辑:
    - `UV.y > (1 - _Progress)` 的顶部区域输出 `alpha = 0`
    - 其余区域继续输出原始 sprite 像素
  - 这次重写的原因:
    - 之前使用的是更接近通用 unlit 的 pass 结构
    - 在 URP 2D Renderer + SpriteRenderer 下表现不稳定
    - 现版本已按用户反馈实现真正“从顶部到底部逐渐透明”
  - `Assets/Art/Shaders/MiningProgress.mat`
  - 继续作为 overlay 材质，绑定 `Perraria/MiningProgress`

### 场景配置（SampleScene.unity）
- 恢复 `Player/MiningProgressOverlay` 子对象
- `MiningProgressOverlay` 组件绑定 `SpriteRenderer`
- `SpriteRenderer` 使用 `MiningProgress.mat`
- `SpriteRenderer.sortingOrder = 2`，高于地形 Tilemap 与高亮 Tilemap
- `PlayerBlockInteraction` 场景数据中补回 `_miningOverlay` 引用
- 将误写成 `9` 的主相机 `orthographic size` 恢复为 `7`

## 变更文件
- 修改任务规格 1 个: `docs/tasks/005-mining-duration.md`
- 修改脚本 4 个: `TileManager.cs`、`PlayerBlockInteraction.cs`、`MiningProgressOverlay.cs`、`BlockDataRegistryCsvImporter.cs`
- 修改 Shader 1 个: `MiningProgress.shader`
- 修改场景 1 个: `SampleScene.unity`
- 修改交付记录 1 个: `docs/codex-reports/005-mining-duration.md`

## 验证结果
- Unity MCP 已确认场景中存在 `MiningProgressOverlay` 对象
- Unity MCP 已确认 overlay 使用 `MiningProgress` 材质，`sortingOrder = 2`，默认 `enabled = false`
- `SampleScene.unity` 中已包含:
  - `MiningProgressOverlay` GameObject
  - `MiningProgressOverlay` MonoBehaviour
  - `PlayerBlockInteraction._miningOverlay` 引用
- 已根据用户最终反馈确认:
  - 真实的“从顶部到底部逐渐透明，最后消失”效果已经实现
- 本轮未完成一次完整 Play 模式人工挖掘验收
- Unity MCP 在本轮后期对部分类反射/编译检查不稳定，导致以下两项未做成最终自动化确认:
  - `TileManager.GetBlockSprite()` 进入运行域后的反射校验
  - `PlayerBlockInteraction` 新字段在 `SerializedObject` 中的最终自动读取
- 代码文件与场景 YAML 已同步到最新实现，剩余风险主要在 Unity 编辑器域刷新时机，不在源文件逻辑本身

## Claude Code 重点检查项
- 重点检查 `Assets/Scripts/Player/PlayerBlockInteraction.cs`
  - `_miningOverlay` 的自动查找兜底是否足够可靠
  - 完成挖掘时 `ResetMining()` 与 `SetBlock(..., Air)` 的先后顺序是否符合预期
  - 挖掘时隐藏高亮的策略是否是当前最合适方案，还是应改为“边框式高亮”以便与挖掘效果并存
- 重点检查 `Assets/Art/Shaders/MiningProgress.shader`
  - 顶部裁切透明逻辑是否符合“从顶部到底部逐渐消失”
  - 基于 URP 官方 `Sprite-Unlit-Default` 改写后，是否还会触发 2D SRP Batcher / SpriteRenderer 兼容性问题
- 重点检查 `Assets/Scripts/World/TileManager.cs`
  - `SetTileFlags(tilemapPos, TileFlags.None)` 对现有 Tilemap 工作流是否有副作用
  - 是否需要在 `ResetTileColor()` 中恢复某种 TileFlags，还是保持当前解锁状态即可
- 重点检查 `Assets/Scenes/SampleScene.unity`
  - `MiningProgressOverlay` 对象和 `_miningOverlay` 引用是否都已正确落盘
  - 主相机 `orthographic size` 是否确实恢复为 `7`
- 建议最终在 Play 模式手测:
  - Dirt 约 0.5 秒完全消失
  - Stone 约 2 秒完全消失
  - 松手 / 移格 / 超距时原方块立即恢复完整显示
  - 高亮层与 overlay 同时存在时排序是否正确

## 已知说明
- 工作区仍有与任务 004 相关的未提交文件和 `Assets/_Recovery.meta`，本次未回退
- 编辑器域刷新在本轮后期不稳定，个别 Unity MCP 反射检查超时或返回不一致；源文件与场景文件已手动对齐到最终实现
- `Assets/Scripts/Editor/BlockDataRegistryCsvImporter.cs` 与 `Assets/Data/BlockDataRegistry.csv` 的导表工具本轮不是核心功能目标，但由于其编译错误会影响 Unity 域刷新，因此已顺手修复
- 用户最终确认效果已达到“从上到下逐渐透明最后消失”；若 Claude 复查，请重点回看本轮最后几处修复而不是只看初版 overlay 方案
