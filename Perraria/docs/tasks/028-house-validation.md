# 任务 028 - 房屋构建判定【骨架版】

> ⚠️ **实施前必须补全**
> 本规范是**远期骨架版**，仅记录目标 / 依赖 / 验收骨架。**真正轮到 027 实施前，必须重新检查并补全**：
> - [ ] 房间合法性的具体规则（最小尺寸、墙体材质、门数量、家具最低数量）
> - [ ] 检测算法（Flood Fill 围合检测 vs 标记墙体的反向查找）
> - [ ] 房屋有效后的回报（NPC 入住？仅视觉提示？属性 buff？）
> - [ ] UI 反馈（按键查询当前房间状态？右上角小图标？）
> - [ ] 与 026 家具系统的具体接口
>
> **执行 Codex 实现前，由 Claude Code 重新审视并产出完整版规范替换本文件。**

## 目标
1. 引入"房间"的概念：由墙体 / 地板 / 天花板（普通 Block）+ 门（Furniture）+ 家具（至少 1 椅 + 1 光源 + 1 桌 / 床）围合的封闭空间。
2. 玩家按下查询键（暂定 H）→ 检测当前所站位置是否在合法房间内，UI 反馈结果（是 / 否 + 缺什么）。
3. 房屋合法的初步用途：
   - 视觉小图标提示（"这是合法房间"）
   - 为后续 NPC 入住系统铺路
   - **本任务不做 NPC 入住** —— 留给独立 NPC 任务

## 设计概要（粗略）

### 房间合法性规则（Terraria 借鉴）
**最小空间**：60 格但 ≤ 750 格（防止"全世界一个房间"）  
**封闭性**：所有边界格子（围合区域的外边）必须是非 Air 的方块或门  
**地板**：底部至少 1 格高度的实心 Block  
**门**：至少 1 个门型 Furniture（可通行的家具）  
**家具**：至少 1 个椅子 + 1 个桌子 / 床（"工作位 + 休息位"二选一）+ 1 个光源 Furniture（火把 / 灯笼）

> 这些规则细节 027 实施前重新评估，可能简化（"最小尺寸 + 围合 + 1 张床"即可）。

### 检测算法
**Flood Fill 围合检测**：从玩家所在格开始，向 8 方向扩散，遇到非 Air 方块或 Furniture 边界停止。
- 如果扩散过程中超出 750 格 → 返回 OpenSpace
- 如果扩散完成且包围区域 ≥ 60 格 → 返回封闭区域
- 在区域内查询 FurnitureRegistry，统计椅子 / 床 / 门 / 光源数量
- 全部满足 → ValidRoom，否则 InvalidRoom + 缺失项列表

### 数据结构
```csharp
public enum RoomValidationResult
{
    OpenSpace,        // 不封闭
    TooSmall,
    TooLarge,
    MissingDoor,
    MissingChair,
    MissingTableOrBed,
    MissingLight,
    Valid,
}

public struct RoomCheckReport
{
    public RoomValidationResult Result;
    public int CellCount;
    public int ChairCount;
    public int TableCount;
    public int BedCount;
    public int DoorCount;
    public int LightCount;
}

public static class RoomValidator
{
    public static RoomCheckReport CheckRoomAt(Vector2Int origin, TileManager tileManager, FurnitureRegistry registry);
}
```

### UI 反馈
按 H 键 → 屏幕右上角弹出 `RoomCheckPanel`（持续 3 秒）：
```
✅ Valid Room
   ChairCount: 1, TableCount: 1, DoorCount: 1, LightCount: 1, CellCount: 80
```
或：
```
❌ Missing Light
   ChairCount: 1, TableCount: 1, DoorCount: 1, LightCount: 0
```

## 依赖（候选）
- 任务 003 WorldGenerator / TileManager（用 GetBlock 做 Flood Fill）
- 任务 027 Furniture / FurnitureRegistry（查询家具清单）
- 任务 015 PlayerHealthUI（右上角 UI 锚点参考）

## 文件清单（候选）
- `Assets/Scripts/World/Rooms/RoomValidator.cs`
- `Assets/Scripts/World/Rooms/RoomCheckReport.cs`
- `Assets/Scripts/World/Rooms/RoomValidationResult.cs`
- `Assets/Scripts/UI/RoomCheckPanel.cs`
- `Assets/Scripts/Player/PlayerRoomQuery.cs`（处理 H 键输入）

## 验收标准（骨架）
- [ ] 玩家在野外按 H → 显示 OpenSpace
- [ ] 玩家造一个 5×5 全封闭石头房间（无家具）按 H → MissingDoor / MissingChair...
- [ ] 加上门 + 1 椅 + 1 床 + 1 灯 → Valid
- [ ] 房间 < 60 格 → TooSmall
- [ ] 房间 > 750 格 → TooLarge
- [ ] 拆掉一面墙后再按 H → OpenSpace
- [ ] Flood Fill 性能：单次检测 < 50ms（4096 格地图上）

## 注意事项
- **027 实施前必须做的事**：
  1. 与用户确认房间规则细节（最小尺寸 / 必需家具）
  2. 是否引入"光源 Furniture"（如 Item_Torch）—— 这可能是个独立小任务
  3. UI 提示风格（弹窗 vs 持续小图标）
  4. 决定本任务是否包含 NPC 入住的"地基"（如 NPC 占用某个房间的标记）
- **不做的事（027 范围内）**：
  - NPC 入住逻辑（独立任务）
  - 房间命名 / 玩家自定义标签
  - 多个房间的可视化地图
  - 房间属性 buff（如休息回血）—— 留给后续生命系统扩展

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/028-house-validation.md`
写一份交付记录。

---

> 📝 **重要提醒**（贴顶强调）：本文件是骨架版，027 实施前 Claude Code 必须重新走一轮：
> 1. 重新审视房间规则（可能要简化到 MVP：封闭 + 床即可）
> 2. 确认是否需要新增"光源 Furniture"前置任务
> 3. 与用户对齐房屋系统的"用途"（NPC？仅视觉？属性 buff？）
> 4. 用完整版规范替换本文件再交给 Codex
