# Perraria v1.0 - 系统架构

## 模块总览
```
┌─────────────────────────────────────────────┐
│                 GameManager                  │
│           (游戏状态、初始化流程)              │
└──────┬──────────┬──────────┬────────────────┘
       │          │          │
┌──────▼──┐ ┌────▼────┐ ┌──▼──────────┐
│  World  │ │ Player  │ │   Enemy     │
│ 世界生成 │ │ 移动/战斗│ │  AI/生成    │
│ Tilemap │ │ 输入处理 │ │  掉落物     │
└──────┬──┘ └────┬────┘ └──┬──────────┘
       │         │         │
┌──────▼─────────▼─────────▼──────────┐
│              Items                   │
│      物品数据、背包、掉落拾取         │
└──────────────────┬──────────────────┘
                   │
            ┌──────▼──────┐
            │     UI      │
            │ HUD、背包   │
            └─────────────┘
```

## 模块职责

### GameManager (`Core/`)
- 游戏初始化流程：生成世界 → 生成玩家 → 启动敌人生成器
- 游戏状态管理（Playing / Paused / GameOver）
- 全局单例，其他模块通过它获取引用

### World (`World/`)
- **BlockType** (enum): 方块类型定义（Air, Dirt, Grass, Stone）✅ 已实现
- **WorldData**: 纯 C# 世界数据类，2D BlockType 数组，带安全边界访问 ✅ 已实现
- **WorldGenerator**: 程序化地形生成（Perlin Noise 地表 + 洞穴），Tilemap 批量渲染，玩家出生点 ✅ 已实现
- **TileRegistry** (ScriptableObject): BlockType → TileBase 映射 ✅ 已实现
- **TileManager**: Tilemap 读写接口，方块的放置/破坏/查询（待实现）
- 依赖: 无

### Core (`Core/`)
- **CameraFollow**: LateUpdate 平滑摄像机跟随 ✅ 已实现
- **GameManager**: 游戏状态管理（待实现）

### Player (`Player/`)
- **PlayerController**: 移动（行走、跳跃），接收输入 ✅ 已实现
- **PlayerBlockInteraction**: 鼠标挖掘/放置方块，范围检查，高亮光标（待实现）
- **PlayerCombat**: 攻击敌人（待实现）
- **PlayerHealth**: 生命值管理、受击、死亡（待实现）
- 依赖: World（TileManager 挖掘/放置方块）、Items（背包操作）

### Items (`Items/`)
- **ItemDatabase** (ScriptableObject): 全部物品定义（ID、名称、图标、类型、堆叠上限）
- **Inventory**: 背包数据管理（增删查改）
- **ItemDrop**: 场景中的掉落物实体，靠近玩家自动拾取
- 依赖: 无（被 Player、Enemy、UI 依赖）

### Enemy (`Enemies/`)
- **EnemyController**: 基础 AI（检测玩家 → 移动 → 接触伤害）
- **EnemyHealth**: 生命值、受击、死亡掉落
- **EnemySpawner**: 敌人生成逻辑（位置、频率、上限）
- 依赖: Items（死亡掉落）、Player（检测距离）

### UI (`UI/`)
- **HUDManager**: 生命值条、快捷栏显示
- **InventoryUI**: 背包界面开关、物品拖拽
- **HotbarUI**: 快捷栏选择、物品切换
- 依赖: Items（读取背包数据）、Player（读取生命值）

## 数据流方向
```
Input → Player → World (挖掘/放置)
                → Enemy (攻击)
                → Items (拾取/使用)
                → UI (状态更新)

Enemy → Player (伤害)
      → Items (掉落)

World → Items (方块掉落)
```

## 关键技术决策

| 决策 | 方案 | 原因 |
|------|------|------|
| 地形 | Unity Tilemap | 2D 方块游戏标准方案，性能好，编辑器支持完善 |
| 地形生成 | Perlin Noise | 简单可控，适合 v1.0 |
| 物品数据 | ScriptableObject | Unity 原生方案，Inspector 友好，不需要外部数据库 |
| 输入系统 | Unity Input System (New) | 项目已引入，支持多设备 |
| UI | Unity UI (uGUI) | 成熟稳定，社区资源丰富 |
| 通信方式 | 直接引用 + 事件 | v1.0 复杂度低，不需要事件总线 |
| 碰撞优化 | CompositeCollider2D | 合并 Tilemap 碰撞体，避免 80K 独立碰撞体 |
| 像素渲染 | Pixel Perfect Camera | PPU=16, 480x270 参考分辨率，消除亚像素偏移 |
| 玩家物理 | 零摩擦 PhysicsMaterial2D | 防止玩家卡在瓦片边缘 |
