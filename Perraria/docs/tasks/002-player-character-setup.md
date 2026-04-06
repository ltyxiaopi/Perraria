# 任务 002 - 玩家角色场景搭建

## 目标
配置 knight.png 的导入参数，在 SampleScene 中搭建可运行的玩家角色 GameObject，使已有的 `PlayerController.cs` 能正常工作。搭建一个临时测试地面用于验证移动和跳跃。

## 前置条件
- `Assets/Scripts/Player/PlayerController.cs` 已存在
- `Assets/InputSystem_Actions.inputactions` 已存在（含 Player ActionMap，Move + Jump Action）
- `Assets/Art/Sprites/knight.png` 已存在（256x256，51 个子 Sprite，帧尺寸约 15x20 像素）

## 实现内容

### 1. 配置 knight.png 导入参数（通过 MCP）
- **Sprite Mode**: Multiple（保持不变）
- **Pixels Per Unit**: 16
- **Filter Mode**: Point（像素风不模糊）
- **Compression**: None（保持像素锐利）
- 切割方式保持 Automatic（当前已切出 51 帧，不需要重切）
- 应用设置后刷新 AssetDatabase

### 2. 配置 Ground Layer（通过 MCP）
- 在 TagManager 中找一个空的 User Layer 槽位，添加 "Ground" Layer

### 3. 创建测试地面（通过 MCP）
- 创建 GameObject，命名 "TestGround"
- 添加 `SpriteRenderer`，使用 Unity 内置白色方块 Sprite（`UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")` 或通过 `Sprite.Create` 创建纯色 Sprite），颜色设为深绿色 `(0.2, 0.5, 0.2, 1)`
- Transform Scale: `(30, 2, 1)`，Position: `(0, -3, 0)`
- 添加 `BoxCollider2D`
- 设置 Layer 为 "Ground"

### 4. 创建 Player GameObject（通过 MCP）
创建名为 "Player" 的 GameObject，Position `(0, 0, 0)`，Tag 设为 "Player"，配置以下组件：

**SpriteRenderer**
- Sprite: 使用 `knight_0`（idle 第一帧，路径 `Assets/Art/Sprites/knight.png`）
- Sorting Order: 1

**Rigidbody2D**
- Body Type: Dynamic
- Gravity Scale: 3
- Constraints: Freeze Rotation Z = true
- Collision Detection: Continuous
- Interpolate: Interpolate

**CapsuleCollider2D**
- Direction: Vertical
- 尺寸贴合角色 Sprite（根据 knight_0 的实际像素尺寸和 PPU 计算）

**PlayerInput**
- Actions Asset: 引用 `Assets/InputSystem_Actions.inputactions`
- Default Map: "Player"

**PlayerController**
- Move Speed: 6
- Jump Force: 10
- Ground Check Radius: 0.2
- Ground Layer: 选择 "Ground" Layer
- Sprite Renderer: 引用自身的 SpriteRenderer

**子对象 "GroundCheck"**
- 空 GameObject，作为 Player 的子对象
- Local Position: `(0, -0.6, 0)`（角色 Sprite 底部偏下）
- 赋值给 PlayerController 的 `_groundCheck` 字段

### 5. 配置摄像机
- Main Camera Position: `(0, 0, -10)`
- Background 颜色: 天蓝色 `(0.5, 0.8, 1, 1)`
- Orthographic Size: 7

## 文件清单
- 无新脚本文件
- 通过 MCP 修改: SampleScene.unity、TagManager.asset、knight.png 导入设置

## 验收标准
- [ ] knight.png 导入参数正确（Point filter、PPU=16、无压缩）
- [ ] "Ground" Layer 已创建
- [ ] Player GameObject 存在于场景中，包含上述所有组件且参数正确
- [ ] GroundCheck 子对象位于角色脚底
- [ ] TestGround 存在，Layer 为 "Ground"
- [ ] 运行游戏后按 A/D 可水平移动，按 Space 可跳跃
- [ ] 玩家站在地面上不穿透
- [ ] 控制台无报错

## 注意事项
- 所有操作通过 Unity MCP 完成，不要手动编辑 .unity / .asset 文件
- Sprite 是像素风格的占位素材，后续可能替换
- PlayerInput 的 Behavior 模式须与 PlayerController.cs 中的输入读取方式匹配（代码通过 `PlayerInput.actions.FindActionMap` 读取）
- 不要添加任何任务范围外的东西（动画、摄像机跟随脚本、额外组件等）
- 如果 MCP 操作遇到问题（如找不到内置 Sprite、组件赋值失败等），在 PR 中说明，不要自行变通绕过
