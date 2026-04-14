# 任务 006 - 挖掘视觉效果简化：整格透明度渐变

## 目标
将挖掘视觉效果从"Overlay + Shader 从顶部到底部裁切"改为"直接对目标 Tile 做整格 alpha 渐变"。挖掘过程中方块整体逐渐变透明，进度满时完全消失。

## 背景
当前 005 实现了一套相对复杂的视觉方案：隐藏原 Tile → 用 MiningProgressOverlay 的 SpriteRenderer + 自定义 Shader 裁切顶部。改为直接修改 Tilemap 上目标格子的颜色 alpha，效果更直观、代码更简单、维护成本更低。

## 设计概要

### 视觉效果
- 开始挖掘：方块完全不透明（alpha = 1）
- 挖掘过程中：alpha 从 1 线性降低到 0
- 挖掘完成：方块已完全透明，恢复颜色后设为 Air
- 挖掘中断：alpha 立即恢复为 1（方块恢复完整显示）

### 核心公式
```
alpha = 1 - miningProgress
tileColor = new Color(1, 1, 1, alpha)
```

## 变更清单

### `Assets/Scripts/Player/PlayerBlockInteraction.cs` — 修改

#### 移除
- 移除 `[SerializeField] private MiningProgressOverlay _miningOverlay;` 字段
- 移除 Awake / Start 中对 `_miningOverlay` 的 `GetComponentInChildren` 兜底查找
- 移除所有 `_miningOverlay` 的调用（`SetSprite`、`Show`、`Hide`、`SetProgress`）

#### 修改 `StartMining()`
- 不再将原 Tile 设为完全透明（移除 `SetTileColor(cell, transparent)`）
- 不再调用 overlay 的 `SetSprite` / `Show`
- 开始时直接设置 `_tileManager.SetTileColor(_miningCell, Color.white)`（alpha=1，无变化）

#### 修改 `ContinueMining()`
- 移除 overlay 的 `Show` / `SetProgress` 调用
- 每帧根据进度设置 Tile 颜色：
  ```
  float alpha = 1f - _miningProgress;
  _tileManager.SetTileColor(_miningCell, new Color(1f, 1f, 1f, alpha));
  ```

#### `ResetMining()` — 不变
- 已有 `_tileManager.ResetTileColor(_miningCell)` 恢复为 `Color.white`，逻辑不变

#### 挖掘完成分支 — 不变
- 已有 `minedCell` 缓存 → `ResetMining()` → `SetBlock(minedCell, Air)` 流程，不变

### `Assets/Scripts/Player/MiningProgressOverlay.cs` — 删除
- 整个文件删除，不再需要

### `Assets/Scripts/Player/MiningProgressOverlay.cs.meta` — 删除

### `Assets/Art/Shaders/MiningProgress.shader` — 删除
- 不再需要自定义 Shader

### `Assets/Art/Shaders/MiningProgress.shader.meta` — 删除

### `Assets/Art/Shaders/MiningProgress.mat` — 删除
- 不再需要覆盖层材质

### `Assets/Art/Shaders/MiningProgress.mat.meta` — 删除

### 场景配置 `SampleScene.unity`
- 删除 `Player/MiningProgressOverlay` 子 GameObject（连同 SpriteRenderer、MiningProgressOverlay 组件）
- `PlayerBlockInteraction` 上的 `_miningOverlay` 字段移除后场景序列化会自动丢弃该引用

## 不变的部分
- `TileManager.SetTileColor()` / `ResetTileColor()` / `GetBlockSprite()` — 保留不动（SetTileColor 和 ResetTileColor 仍被使用，GetBlockSprite 暂时无调用方但保留以备后续）
- `BlockDataRegistry` — 不变
- 挖掘逻辑（进度公式、中断重置、范围检查）— 不变
- 高亮逻辑 — 不变
- 右键放置 — 不变

## 依赖
- `TileManager.SetTileColor` ✅ 已实现，已包含 `SetTileFlags(TileFlags.None)` 解锁
- `TileManager.ResetTileColor` ✅ 已实现

## 验收标准
- [ ] 挖掘过程中方块整体逐渐变透明（alpha 从 1 渐变到 0）
- [ ] 挖掘完成时方块已完全透明，随后正确变为 Air
- [ ] 松开左键 → 方块立即恢复完全不透明
- [ ] 鼠标移到其他格子 → 原格子立即恢复完全不透明
- [ ] 目标格子移出交互范围 → 格子立即恢复完全不透明
- [ ] `MiningProgressOverlay.cs` 和相关 `.meta` 文件已删除
- [ ] `MiningProgress.shader` 和 `MiningProgress.mat` 及其 `.meta` 文件已删除
- [ ] 场景中 `Player/MiningProgressOverlay` 子对象已删除
- [ ] `PlayerBlockInteraction` 中不再有任何 `_miningOverlay` 相关代码
- [ ] 不引入新的控制台错误或警告
- [ ] 右键放置逻辑不受影响
- [ ] 高亮层正常显示

## 注意事项
- **TileFlags 解锁**：`SetTileColor` 中已包含 `SetTileFlags(TileFlags.None)`，确保 alpha 修改生效
- **删除场景子对象**：需通过 Unity MCP 删除 `Player/MiningProgressOverlay` GameObject，不要手动编辑 `.unity` 文件
- **Shaders 目录**：删除 shader 和 mat 后如果 `Assets/Art/Shaders/` 目录为空，保留目录即可（后续可能还会用到）
- **GetBlockSprite**：`TileManager.GetBlockSprite()` 当前无调用方，但保留不删，后续任务可能用到
