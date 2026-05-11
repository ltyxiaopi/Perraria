# 任务 020 - 近战武器扩展 + 木镐工具

## 目标
1. 把 `Assets/Art/Sprites/Weapons/` 下的近战武器（共 27 张贴图，去掉远程相关的）批量接进 `ItemData` / `ItemDatabase`，
   每把武器配置独立的伤害 / 攻速 / 击退数值，玩家从快捷栏切换不同武器获得明显差异化体验。
2. 新增 `Tool` 类型物品「木镐 (Item_Pickaxe_Wood)」，装备后挖掘速度倍率提升（×1.5）；
   未装备工具时仍保留手挖（×1.0），不阻塞玩家。
3. 玩家**新建存档**时初始背包带 1 把木镐 + 1 把骑士剑（沿用 013 已存在的 Item_KnightSword 资产），
   通过 018 的存档系统初始化路径写入，不在运行时硬编码。
4. 远程武器（`weapon_bow*` / `weapon_arrow` / `weapon_*_magic_staff`）**本任务不接** —— 留给 021。

## 设计概要

### 现有代码摸底（不要重新设计）
- `ItemData` 已经有 5 个武器字段：`WeaponDamage / WeaponRange / SwingArcDegrees / SwingDuration / KnockbackForce`（见 `Assets/Scripts/Items/ItemData.cs`），本任务**只新增 1 个工具字段**，不动现有武器字段。
- `PlayerCombat` 已实现 `TryGetSelectedWeapon` 过滤 `Type == Weapon`，挥砍逻辑数据驱动，**新增武器 = 配资产即可，不改脚本**。
- `PlayerBlockInteraction` 已有 `_miningSpeed` 字段（默认 1.0），目前是 `[SerializeField]` 硬编码值，**本任务改成"读取选中工具的 MiningSpeedMultiplier，无工具则 1.0"**。
- 武器贴图命名约定 `weapon_<name>.png` 来源于 0x72 Dungeon Tileset II（16×16，PPU=16，Pivot=Bottom）—— 切片配置参考已有的 `weapon_knight_sword.png` 处理方式。

### Tool 字段加在哪
`ItemData` 已经"上帝化"地把武器字段直接铺在根节点（不分子结构）。沿用同样模式，新增 1 个工具字段：

```csharp
[SerializeField] private float _miningSpeedMultiplier = 1f;   // 仅 Tool 类型有效，Weapon/Block/Material 忽略
public float MiningSpeedMultiplier => _miningSpeedMultiplier;
```

> 不引入 `ToolType` 子枚举（斧 / 锤 / 镐）—— 020 只做镐子，斧子和锤子等"工具二号"出现时再决定要不要分子类型。

### 镐子怎么影响挖掘
`PlayerBlockInteraction.StartMining` 当前用字段 `_miningSpeed` 计算 `_miningDuration = hardness / _miningSpeed`。改成：

```csharp
float speed = GetCurrentMiningSpeed();   // 新增私有方法
_miningDuration = hardness / speed;
```

`GetCurrentMiningSpeed()` 逻辑：
1. 拿 `_inventory.GetSelectedItem()`
2. 如果选中槽是 `ItemType.Tool` 且 `MiningSpeedMultiplier > 0` → 返回 `_miningSpeed * MiningSpeedMultiplier`
3. 否则返回 `_miningSpeed`（兜底手挖）

> `_miningSpeed` 字段保留作为「玩家基础挖掘速度」（未来可加 buff/天赋），别删。

### 镐子在场景里怎么显示
**复用 `WeaponPivot/Weapon` 渲染槽**。当前 `PlayerCombat.RefreshWeaponRenderer` 只在 `Type == Weapon` 时显示武器。
本任务**不让镐子接进 PlayerCombat 挥砍** —— 镐子不挥砍，单纯在玩家手里"拿着"作为视觉反馈。

实现方式：新增独立的 `PlayerToolRenderer` 组件（挂在 Player 上），订阅 `Inventory.OnSelectedHotbarChanged`：
- 选中 Tool → 在 `WeaponPivot/Weapon` 上显示工具的 `Icon`，`_weaponPivot` 保持不旋转
- 选中 Weapon → `PlayerCombat` 已经处理，不干涉
- 选中 Block / 空 / Material → 隐藏 `_weaponRenderer`

> 这里有个**渲染权冲突**：`PlayerCombat.RefreshWeaponRenderer` 也会写 `_weaponRenderer.sprite/enabled`。
> 解决方式：让 `PlayerCombat` 在 `TryGetSelectedWeapon` 失败时**主动清空**（已经这样做了），
> 而 `PlayerToolRenderer` 只在选中 Tool 时主动写入。两者都订阅 `OnSelectedHotbarChanged`，但写入条件互斥，不会打架。
> Codex 实现时如果发现冲突，优先方案是**把工具显示也合并到 `PlayerCombat`**（改名 `PlayerHandheldRenderer` 或类似），
> 不要拆两个组件互相覆盖。最终方案在交付记录里说明。

### 初始物品怎么塞进新存档
018 的 `GameStateSnapshot.Apply` 是用 `SaveData` 还原。**新建存档**走的是另一条路 —— 当前应该是直接进入 `SampleScene`，
玩家空着背包开始。本任务在 016 主菜单的"New Game"按钮逻辑里加初始化：

```csharp
// 伪代码：MainMenu 的 "New Game" 按钮 OnClick
SaveSystem.Delete();   // 清掉旧存档（如果有）
// 新增：写一份 minimal 初始 SaveData（只填 Inventory 部分）
SaveData initial = SaveData.CreateNewGameDefault();
SaveSystem.Save(initial);
SceneManager.LoadScene("SampleScene");   // SampleScene 启动时会 Load 这份初始档
```

`SaveData.CreateNewGameDefault()` 是新增的静态工厂：
- `Inventory.Slots[0]` = `Item_Pickaxe_Wood` × 1
- `Inventory.Slots[1]` = `Item_KnightSword` × 1
- `Inventory.SelectedHotbarIndex` = 0
- `Player.CurrentHealth = MaxHealth = 100`，`Position` 留 null（让 WorldGenerator 决定出生点）
- `World.Seed` = `Random.Range(int.MinValue, int.MaxValue)`
- `World.PlayerEdits` = 空 list

> 这种做法把"新建存档"和"读档"统一成一条链路。代价是 NewGame 后第一次进场景比"裸跑"慢 ~50ms（一次磁盘写 + 一次 JSON 解析），可接受。

### 武器资产清单（Codex 通过 MCP 创建）

近战武器一组，全部 `Type=Weapon, MaxStackSize=1`。数值参考 013 的 Item_KnightSword（Damage=10, Range=1.4, Arc=100, Duration=0.35, Knockback=6）作为"中庸基准"。

| 资产名 | 贴图 | Damage | Range | Arc° | Duration | Knockback | 定位 |
|---|---|---|---|---|---|---|---|
| Item_KnightSword | weapon_knight_sword | 10 | 1.4 | 100 | 0.35 | 6 | 已存在（基准） |
| Item_RustySword | weapon_rusty_sword | 6 | 1.3 | 95 | 0.40 | 4 | 入门廉价剑 |
| Item_RegularSword | weapon_regular_sword | 9 | 1.4 | 100 | 0.36 | 5 | 标准 |
| Item_GoldenSword | weapon_golden_sword | 13 | 1.5 | 105 | 0.34 | 7 | 中阶 |
| Item_LavishSword | weapon_lavish_sword | 16 | 1.5 | 105 | 0.33 | 8 | 高阶华丽 |
| Item_RedGemSword | weapon_red_gem_sword | 18 | 1.5 | 110 | 0.32 | 9 | 高阶宝石 |
| Item_DuelSword | weapon_duel_sword | 12 | 1.6 | 90 | 0.34 | 6 | 长但弧度小 |
| Item_AnimeSword | weapon_anime_sword | 11 | 1.7 | 90 | 0.36 | 6 | 长剑 |
| Item_Katana | weapon_katana | 14 | 1.5 | 80 | 0.30 | 5 | 快速窄弧 |
| Item_Machete | weapon_machete | 11 | 1.3 | 110 | 0.36 | 6 | 短宽弧 |
| Item_Cleaver | weapon_cleaver | 14 | 1.2 | 120 | 0.42 | 8 | 重慢宽弧 |
| Item_SawSword | weapon_saw_sword | 13 | 1.3 | 100 | 0.40 | 6 | 锯齿剑 |
| Item_Knife | weapon_knife | 5 | 0.9 | 70 | 0.20 | 2 | 快速短匕首 |
| Item_Axe | weapon_axe | 12 | 1.3 | 110 | 0.40 | 7 | 单手斧 |
| Item_DoubleAxe | weapon_double_axe | 15 | 1.3 | 115 | 0.45 | 8 | 双刃斧 |
| Item_Waraxe | weapon_waraxe | 17 | 1.4 | 110 | 0.48 | 10 | 战斧重 |
| Item_Hammer | weapon_hammer | 11 | 1.2 | 110 | 0.45 | 9 | 单手锤 |
| Item_BigHammer | weapon_big_hammer | 18 | 1.3 | 115 | 0.55 | 12 | 重锤 |
| Item_Mace | weapon_mace | 13 | 1.2 | 100 | 0.42 | 8 | 钉头锤 |
| Item_BatonWithSpikes | weapon_baton_with_spikes | 12 | 1.2 | 100 | 0.42 | 8 | 狼牙棒 |
| Item_Spear | weapon_spear | 11 | 2.0 | 50 | 0.35 | 5 | 长矛（窄弧远距） |

> **Item_Bow / Item_Bow2 / Item_Arrow / Item_GreenMagicStaff / Item_RedMagicStaff / Item_ThrowingAxe** 不在本表 —— 021 任务处理（包括 ThrowingAxe，本任务不创建资产）。

### 工具资产

| 资产名 | 贴图 | Type | MaxStack | MiningSpeedMultiplier |
|---|---|---|---|---|
| Item_Pickaxe_Wood | tool_pickaxe_wood | Tool | 1 | 1.5 |

> ItemId 自动递增，避免和已注册物品冲突；Codex 在 MCP 创建时查一遍 ItemDatabase 现有最大 ID。

### 切片注意事项
所有 `weapon_*.png` 和 `tool_*.png` 切片设置：
- PPU = 16
- Filter Mode = Point (no filter)
- Compression = None
- Sprite Mode = Single
- **Pivot = Bottom**（保证武器握把对齐 WeaponPivot）
- **裁剪到实际像素内容**（用 Sprite Editor "Trim"），周围留白会让视觉变小（这条记忆里有，必须严格执行）

## 接口签名

### ItemData 新增 1 个字段
```csharp
public sealed class ItemData : ScriptableObject
{
    // ...existing fields...
    [SerializeField] private float _miningSpeedMultiplier = 1f;
    public float MiningSpeedMultiplier => _miningSpeedMultiplier;
}
```

### PlayerBlockInteraction 改动
```csharp
// 新增私有方法
private float GetCurrentMiningSpeed()
{
    if (_inventory == null) return _miningSpeed;
    ItemStack selected = _inventory.GetSelectedItem();
    if (selected.IsEmpty || selected.Item.Type != ItemType.Tool) return _miningSpeed;
    float multiplier = selected.Item.MiningSpeedMultiplier;
    return multiplier > 0f ? _miningSpeed * multiplier : _miningSpeed;
}

// StartMining 内部把 hardness / _miningSpeed 改成 hardness / GetCurrentMiningSpeed()
```

### PlayerToolRenderer（如果分离方案）
```csharp
// === Player/PlayerToolRenderer.cs ===
[DisallowMultipleComponent]
public sealed class PlayerToolRenderer : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private SpriteRenderer _weaponRenderer;   // 复用 WeaponPivot/Weapon
    [SerializeField] private PlayerCombat _playerCombat;       // 用来检查是否在挥砍中

    private void OnEnable() { _inventory.OnSelectedHotbarChanged += Refresh; ...; Refresh(0); }
    private void OnDisable() { ... }
    private void Refresh(int _)
    {
        if (_playerCombat != null && _playerCombat.IsSwinging) return;   // 挥砍中不抢渲染
        ItemStack selected = _inventory.GetSelectedItem();
        if (!selected.IsEmpty && selected.Item.Type == ItemType.Tool)
        {
            _weaponRenderer.sprite = selected.Item.Icon;
            _weaponRenderer.enabled = true;
        }
        // Weapon / 空 / Block 由 PlayerCombat 自己处理，本组件不干涉
    }
}
```

> **方案 B（推荐 Codex 优先尝试）**：把工具显示直接加进 `PlayerCombat` —— 在 `RefreshWeaponRenderer` 里
> `if Type==Weapon → 显示武器；else if Type==Tool → 显示工具图标，旋转保持 0；else → 隐藏`。改动最小，避免渲染抢权。
> 类名是否要改成 `PlayerHandheldRenderer` 由 Codex 决定，但**不强求改名**（影响面大，留到下次重构）。

### SaveData 新增静态工厂
```csharp
public sealed class SaveData
{
    // ...existing...
    public static SaveData CreateNewGameDefault()
    {
        return new SaveData
        {
            Version = 1,
            SavedAtIso = System.DateTime.UtcNow.ToString("o"),
            Player = new PlayerSaveData
            {
                Position = new Vector3(0f, 0f, 0f),   // WorldGenerator 会覆盖到出生点
                CurrentHealth = 100,
                MaxHealth = 100,
                FacingRight = true,
            },
            Inventory = new InventorySaveData
            {
                SelectedHotbarIndex = 0,
                Slots = BuildInitialSlots(),   // 长度 = 40，前两个填镐子和剑，其余空
            },
            World = new WorldSaveData
            {
                Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                PlayerEdits = new List<WorldTileChange>(),
            },
            Spawner = new SpawnerSaveData { SpawnTimer = 3f },
        };
    }

    private static List<InventorySlotSaveData> BuildInitialSlots()
    {
        // 引用 ItemDatabase 单例查 Item_Pickaxe_Wood / Item_KnightSword 的 ItemId
        // 返回长度 = Inventory.TotalSlots 的列表
    }
}
```

> ⚠️ **静态工厂访问 ItemDatabase 的方式**：`SaveData` 是纯数据类，不该直接持有 Unity 引用。
> 让 `BuildInitialSlots()` 在 `Resources.Load<ItemDatabase>("ItemDatabase")` 或通过新增的 `ItemDatabase.Instance` 单例拿到 ID。
> 当前 `ItemDatabase` 是 ScriptableObject，**Codex 实现时按以下顺序选**：
> 1. 如果项目已有运行时单例机制（GameManager 或类似），通过它注入
> 2. 否则把 ItemDatabase 资产移到 `Assets/Resources/ItemDatabase.asset`，用 `Resources.Load` 加载（一次性，开销可忽略）
> 3. 别在 SaveData 里写死 ItemId 整数（"魔法数字 1, 2, 3"会随物品增删错位）

### MainMenu "New Game" 按钮逻辑
当前 016 的"New Game"按钮直接 `SceneManager.LoadScene("SampleScene")`。改成：
```csharp
public void OnNewGameClicked()
{
    SaveSystem.Delete();
    SaveData initial = SaveData.CreateNewGameDefault();
    SaveSystem.Save(initial);
    SceneManager.LoadScene("SampleScene");
}
```
"Continue" 按钮逻辑不变。

## 依赖
- 任务 005 MiningDuration（修改 PlayerBlockInteraction.StartMining）
- 任务 007 ItemData / ItemDatabase（新增 1 字段，新增 22+ 个资产实例）
- 任务 008 Inventory（已有 GetSelectedItem，本任务零改动）
- 任务 013 PlayerCombat（仅可能调整 RefreshWeaponRenderer，根据 Codex 选定方案）
- 任务 016 MainMenu（"New Game" 按钮接初始化逻辑）
- 任务 018 SaveSystem / SaveData（新增 CreateNewGameDefault）

## 文件清单

### 修改
- `Assets/Scripts/Items/ItemData.cs` — 新增 `_miningSpeedMultiplier` 字段 + getter
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — `StartMining` 用 `GetCurrentMiningSpeed()` 替换 `_miningSpeed`
- `Assets/Scripts/Save/SaveData.cs` — 新增 `CreateNewGameDefault` 静态工厂 + `BuildInitialSlots`
- `Assets/Scripts/UI/MainMenu.cs`（实际文件名以 016 落地的为准） — "New Game" 按钮改走 SaveData 初始化路径
- `Assets/Scripts/Player/PlayerCombat.cs`（如选方案 B） — `RefreshWeaponRenderer` 增加 Tool 分支

### 新增（脚本）
- `Assets/Scripts/Player/PlayerToolRenderer.cs`（仅当 Codex 选方案 A 时） — 工具手持渲染

### 新增（资产，全部通过 MCP 创建）
- `Assets/Data/Items/Item_Pickaxe_Wood.asset`
- `Assets/Data/Items/Item_RustySword.asset`、`Item_RegularSword.asset`、`Item_GoldenSword.asset`、`Item_LavishSword.asset`、`Item_RedGemSword.asset`、`Item_DuelSword.asset`、`Item_AnimeSword.asset`、`Item_Katana.asset`、`Item_Machete.asset`、`Item_Cleaver.asset`、`Item_SawSword.asset`、`Item_Knife.asset`、`Item_Axe.asset`、`Item_DoubleAxe.asset`、`Item_Waraxe.asset`、`Item_Hammer.asset`、`Item_BigHammer.asset`、`Item_Mace.asset`、`Item_BatonWithSpikes.asset`、`Item_Spear.asset`
- `Assets/Data/ItemDatabase.asset` — 追加上述所有条目

### 资产导入设置（通过 MCP 配置）
- `Assets/Art/Sprites/Tools/tool_pickaxe_wood.png` 切片：PPU=16，Point，No Compression，Pivot=Bottom，Single，**裁剪到实际像素**
- 上述 20 张新增 `weapon_*.png` 同样切片设置（已存在的 weapon_knight_sword 不动；weapon_throwing_axe 留给 021）

## 验收标准

### 数据层
- [ ] `ItemData.MiningSpeedMultiplier` 字段在 Inspector 暴露，默认值 1.0
- [ ] `Item_Pickaxe_Wood.asset` 创建成功，Type=Tool，MiningSpeedMultiplier=1.5
- [ ] 22 把武器资产全部创建并注册到 `ItemDatabase.asset`，ItemId 无重复
- [ ] 所有武器贴图切片配置一致（PPU=16, Point, Bottom, Trim）

### 镐子加速挖掘
- [ ] 选中 Item_Pickaxe_Wood 时挖 Stone 耗时约 1.33 秒（2.0 / 1.5）
- [ ] 选中 Item_Pickaxe_Wood 时挖 Dirt 耗时约 0.33 秒（0.5 / 1.5）
- [ ] 选中武器或空槽时挖 Stone 耗时仍 2.0 秒，挖 Dirt 仍 0.5 秒（手挖兜底）
- [ ] 镐子图标在 WeaponPivot/Weapon 上正确显示
- [ ] 在挖掘途中切换槽位（数字键 1-9 或滚轮），mining 进度立即重置（沿用现有 ResetMining 逻辑，无需新增）

### 武器切换
- [ ] 切换不同武器，挥砍范围 / 速度 / 击退强度有明显差异（用 Slime 实测）
- [ ] 武器贴图正确跟随 WeaponPivot 旋转，无穿模
- [ ] 切到 Type=Block 时武器/工具都隐藏，正常放置方块

### 新建存档
- [ ] 主菜单 "New Game" 后进入 SampleScene，背包槽 0 = 木镐 ×1，槽 1 = 骑士剑 ×1
- [ ] 选中槽 0 默认是木镐
- [ ] 退出后 "Continue" 能正确读到刚才那份初始档
- [ ] 重复 "New Game" 会覆盖旧档（不会留下脏数据）
- [ ] SaveData JSON 文件大小 < 5KB（基础初始档不应该很大）

### 综合
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误（包括 ItemDatabase 重复 ID warning）
- [ ] 符合 `coding-conventions.md` 规范
- [ ] MCP 截图验证：手持木镐 / 骑士剑 / 战斧 三种状态视觉无 bug

## 注意事项

### 关于工具手持渲染的方案选择
Codex 自行决定 PlayerToolRenderer 独立 vs 合并进 PlayerCombat，**理由必须写进交付记录**。  
原则：选改动小且不引入渲染抢权的方案。如果发现两个组件互相覆盖 sprite，立即合并。

### 关于初始物品的 ItemDatabase 访问
不要在 SaveData 里写死整数 ItemId（重构脆弱）。优先方案：把 ItemDatabase 移到 Resources 用 Resources.Load。
如果 Codex 觉得这破坏了"Data 目录约定"，第二选项是新增 `ItemDatabase.Instance` 静态字段，由 Bootstrap MonoBehaviour 在场景启动时注入。两种方案都可以，**选定后在交付记录说明**。

### 关于切片"裁剪到实际像素"
0x72 Dungeon Tileset II 的 PNG 普遍带 1-2px 透明 padding（项目记忆里有相关条目）。Codex 切片时用 Sprite Editor 的 "Trim" 功能或手动调整 Sprite 矩形到实际像素区域，**否则武器在场景里视觉会偏小**。验证方式：MCP 截图，武器手持物视觉宽度应该接近 Slime 0.3 倍 scale 的体积参考。

### 不做的事
- **不做工具耐久** —— 已确认 020 决策 1A，无耐久
- **不做攻击动画 / 音效 / 粒子特效** —— 留给独立美术任务
- **不做工具按品阶分组** —— 木镐一把就够，铁镐 / 金镐 / 钻石镐留给后续美术补图后追加资产
- **不做斧子砍树 / 锤子拆方块** —— 工具语义当前只表达"挖掘加速"，砍树系统留给地形扩展任务
- **不做远程武器** —— 弓 / 箭 / 法杖等留给 021
- **不做合成系统对接** —— 玩家拿镐子的方式只有"新建存档默认"，025 合成系统出来后再改

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/020-melee-weapons-and-pickaxe.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。

**交付记录额外要求**：
1. **方案选择说明**：PlayerToolRenderer 独立 vs 合并进 PlayerCombat（带理由）；ItemDatabase 访问方式（Resources / 单例 / 其他）（带理由）
2. **MCP 截图证据**：木镐手持 / 骑士剑挥砍 / 武器切换瞬间 三张截图
3. **挖掘耗时实测**：木镐挖 Stone 实测耗时 + 手挖 Stone 实测耗时（应分别 ≈ 1.33s 和 2.0s）
4. **新建存档自测日志**：完整 JSON 内容（删档 → 新建 → 加载 → 验证 inventory 槽 0/1 内容）
