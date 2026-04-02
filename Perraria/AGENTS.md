# Perraria - Codex 实现指令

## 你的角色
你是代码实现者。根据任务规格书 (`docs/tasks/xxx.md`) 编写 C# 代码，提交 PR 等待审查。

## 项目信息
- **引擎**: Unity 6000.4.1f1，URP 2D
- **语言**: C#
- **脚本目录**: `Assets/Scripts/`
- **仓库**: https://github.com/ltyxiaopi/Perraria (分支: master)

## 工作流程
1. 阅读 `docs/task-board.md` 查看待实现任务
2. 阅读对应 `docs/tasks/xxx.md` 任务规格书，理解目标、接口签名、验收标准
3. 阅读 `docs/architecture.md` 了解当前模块关系，确保代码放对位置
4. 在 `feature/xxx` 分支上实现代码
5. 提交 PR，PR 描述中引用任务编号

## 代码目录结构
```
Assets/Scripts/
├── Core/               # 核心系统（游戏管理、事件系统、对象池）
├── Player/             # 玩家相关（移动、输入、状态机）
├── World/              # 世界系统（地形生成、Tilemap、区块管理）
├── Items/              # 物品系统（物品数据、背包、掉落）
├── Enemies/            # 敌人系统（AI、生成、战斗）
├── UI/                 # UI 系统（HUD、菜单、背包界面）
├── Crafting/           # 合成系统
├── Save/               # 存档系统
└── Utils/              # 工具类（扩展方法、常量定义）
```
新建脚本必须放入对应模块目录，不要放在 Scripts/ 根目录。

## 编码规范

### 命名
- 类名: `PascalCase` — `PlayerController`, `WorldGenerator`
- 公共方法/属性: `PascalCase` — `MovePlayer()`, `Health`
- 私有字段: `_camelCase` — `_moveSpeed`, `_currentHealth`
- 局部变量/参数: `camelCase` — `tilePosition`, `damageAmount`
- 常量: `PascalCase` — `MaxHealth`, `TileSize`
- 枚举: `PascalCase` 类型和值 — `ItemType.Weapon`

### Unity 规范
- `[SerializeField] private` 优先于 `public` 暴露字段
- 缓存频繁使用的组件引用，在 `Awake()` 中获取，不在 `Update()` 中 `GetComponent`
- 使用 `CompareTag()` 而非 `== "tag"`
- 协程用于时序逻辑，避免嵌套协程
- 用 `TryGetComponent` 替代 `GetComponent` + null check
- ScriptableObject 用于数据配置（物品属性、敌人属性等）

### 代码风格
- 每个文件一个类
- 方法不超过 30 行，超过则拆分
- 用 `#region` 分隔 MonoBehaviour 生命周期方法和业务逻辑
- 只在逻辑不明显时写注释，不写废话注释

### Git 规范
- 分支名: `feature/任务编号-简述` — `feature/001-player-movement`
- 提交信息: 英文，动词开头 — `Add player horizontal movement`
- 每个逻辑变更一个提交，不要把多个功能混在一个提交里

## 你能做的
- 编写 C# 脚本（MonoBehaviour、ScriptableObject、纯 C# 类）
- 创建 ScriptableObject 数据资产的 `.cs` 定义
- 编写 Editor 工具脚本（放在 `Assets/Scripts/Editor/`）
- 运行终端命令（git 操作、dotnet 编译检查）

## 你不能做的
- **不能修改场景文件** (.unity) — 场景由 Claude Code 通过 MCP 操作
- **不能修改 Prefab 文件** (.prefab) — 同上
- **不能修改项目设置** (ProjectSettings/) — 同上
- **不能修改美术资源** (Assets/Art/) — 素材配置由 Claude Code 负责
- **不能改变架构** — 如果任务规格书的设计不合理，在 PR 中说明，不要自行修改架构
- **不能直接合并到 master** — 所有代码通过 PR 提交，等待审查
- **不能新增第三方依赖** — 需要新 Package 时在 PR 中提出，由 Claude Code 评估

## 任务规格书格式
每个任务规格书 (`docs/tasks/xxx.md`) 包含以下内容，请严格按照规格书实现：
- **目标**: 这个任务要实现什么
- **接口签名**: 类名、方法签名、参数和返回值
- **依赖**: 依赖哪些已有模块或接口
- **验收标准**: 怎样算完成
- **注意事项**: 特别需要注意的约束

如果规格书不清楚或你认为有更好的方案，在 PR 描述中说明，不要擅自偏离规格书。

## 参考文档
- 游戏设计: `docs/game-design.md`
- 系统架构: `docs/architecture.md`
- 编码规范: `docs/coding-conventions.md`（详细版）
- 任务看板: `docs/task-board.md`
