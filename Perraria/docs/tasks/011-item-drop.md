# 任务 011 - 掉落物实体

## 目标
实现场景中的掉落物实体（ItemDrop）：方块被挖掘后不再直接进入背包，而是在原位生成一个可拾取的物品实体，
玩家靠近后自动拾取进入背包。本任务为后续敌人死亡掉落物品做好基础。

## 设计概要

### 掉落物行为
- 方块挖掘完成时，在方块中心位置生成掉落物实体
- 掉落物显示物品图标（SpriteRenderer），上下浮动（bobbing）动画
- 生成后有短暂拾取延迟（默认 0.5 秒），防止挖掘瞬间拾取
- 玩家靠近掉落物时自动拾取，物品进入背包
- 背包已满时拾取失败，掉落物保留在地面
- 部分拾取：如果背包只能容纳部分数量，拾取能放入的部分，剩余留在地面

### 拾取检测
- 掉落物使用 `CircleCollider2D`（trigger），半径约 1.5 单位作为拾取范围
- 使用 `Rigidbody2D`（Kinematic）确保触发器事件正常触发
- 通过 `OnTriggerStay2D` 检测进入范围的 `Inventory` 组件，自动拾取
- 不依赖 Tag 或 Layer，直接用 `GetComponent<Inventory>()` 判断

### 浮动动画
- 掉落物在生成位置上下浮动（sin 波）
- 振幅 `_bobAmplitude`（默认 0.1 单位），频率 `_bobFrequency`（默认 2 Hz）
- 浮动基准点为生成时的位置

### PlayerBlockInteraction 改动
- 挖掘完成后不再直接调用 `Inventory.AddItem()`
- 改为在方块中心位置实例化 `ItemDrop` 预制体
- 通过 `ItemDrop.Initialize()` 设置物品数据

## 接口签名

### ItemDrop

```csharp
// === Items/ItemDrop.cs ===
// 场景中的可拾取物品实体
[DisallowMultipleComponent]
public sealed class ItemDrop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _pickupDelay = 0.5f;
    [SerializeField] private float _bobAmplitude = 0.1f;
    [SerializeField] private float _bobFrequency = 2f;

    private ItemData _itemData;
    private int _count;
    private float _spawnTime;
    private bool _isPickedUp;
    private Vector3 _spawnPosition;

    /// <summary>持有的物品数据</summary>
    public ItemData ItemData => _itemData;

    /// <summary>持有的物品数量</summary>
    public int Count => _count;

    /// <summary>
    /// 初始化掉落物。由生成方调用。
    /// </summary>
    public void Initialize(ItemData itemData, int count)
    {
        _itemData = itemData;
        _count = count;
        _spawnTime = Time.time;
        _spawnPosition = transform.position;

        if (_spriteRenderer != null && itemData != null)
        {
            _spriteRenderer.sprite = itemData.Icon;
        }
    }

    // Update: 浮动动画
    // OnTriggerStay2D: 拾取检测
}
```

### Update 逻辑

```csharp
private void Update()
{
    if (_isPickedUp) return;

    float yOffset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
    transform.position = _spawnPosition + new Vector3(0f, yOffset, 0f);
}
```

### 拾取逻辑

```csharp
private void OnTriggerStay2D(Collider2D other)
{
    if (_isPickedUp) return;
    if (Time.time - _spawnTime < _pickupDelay) return;

    Inventory inventory = other.GetComponent<Inventory>();
    if (inventory == null) return;

    int remaining = inventory.AddItem(_itemData, _count);
    if (remaining < _count)
    {
        // 至少拾取了一部分
        _count = remaining;
        if (_count <= 0)
        {
            _isPickedUp = true;
            Destroy(gameObject);
        }
    }
}
```

### PlayerBlockInteraction 改动

```csharp
// === Player/PlayerBlockInteraction.cs ===
// 以下为需要修改的部分

public sealed class PlayerBlockInteraction : MonoBehaviour
{
    // ── 新增字段 ──
    [SerializeField] private ItemDrop _itemDropPrefab;

    // ── 修改方法：AddDropToInventory → SpawnItemDrop ──
    // 原方法直接调用 _inventory.AddItem()
    // 新方法改为实例化 ItemDrop 预制体

    private void SpawnItemDrop(Vector3Int minedCell)
    {
        if (_itemDropPrefab == null) return;

        BlockType minedType = _tileManager.GetBlock(minedCell);
        ItemData dropItem = _blockDataRegistry.GetDropItem(minedType);
        if (dropItem == null) return;

        Vector3 spawnPosition = GetCellCenter(minedCell);
        ItemDrop drop = Instantiate(_itemDropPrefab, spawnPosition, Quaternion.identity);
        drop.Initialize(dropItem, 1);
    }

    // ── ContinueMining 中的调用点 ──
    // 将 AddDropToInventory(minedCell) 替换为 SpawnItemDrop(minedCell)
}
```

## 预制体配置

完成代码后需创建 ItemDrop 预制体：

### ItemDrop Prefab (`Assets/Prefabs/ItemDrop.prefab`)
```
ItemDrop (GameObject)
├── ItemDrop (Component)
│   └── _spriteRenderer → 自身 SpriteRenderer
│   └── _pickupDelay = 0.5
│   └── _bobAmplitude = 0.1
│   └── _bobFrequency = 2
├── SpriteRenderer
│   └── Sprite = null (运行时由 Initialize 设置)
│   └── Sorting Layer = Default
│   └── Order in Layer = 10 (渲染在方块之上)
├── Rigidbody2D
│   └── Body Type = Kinematic
│   └── Simulated = true
├── CircleCollider2D
│   └── Is Trigger = true
│   └── Radius = 1.5
```

- 需先创建 `Assets/Prefabs/` 目录（如不存在）
- Sprite 缩放建议 0.5x0.5 或适配 16px 像素风格

## 依赖
- `Inventory` — 背包 AddItem ✅ 已实现 (008)
- `ItemData` — 物品数据（Icon）✅ 已实现 (007)
- `BlockDataRegistry` — 方块掉落映射 ✅ 已实现 (007)
- `PlayerBlockInteraction` — 挖掘完成时的生成入口 ✅ 已实现 (004/008)
- `TileManager` — GetCellCenter 计算生成位置 ✅ 已实现

## 文件清单
- `Assets/Scripts/Items/ItemDrop.cs` — 新增，掉落物实体组件
- `Assets/Prefabs/ItemDrop.prefab` — 新增，掉落物预制体
- `Assets/Scripts/Player/PlayerBlockInteraction.cs` — 修改，挖掘后生成掉落物替代直接入包

## 场景配置
完成代码和预制体后需在 Unity 中：
1. 在 `PlayerBlockInteraction` 的 Inspector 中将 `ItemDrop.prefab` 拖入 `_itemDropPrefab` 字段

## 验收标准

### ItemDrop 基础
- [ ] 掉落物正确显示物品图标（ItemData.Icon）
- [ ] Icon 为 null 时不报错，SpriteRenderer 显示为空
- [ ] 掉落物有上下浮动动画
- [ ] 浮动不偏离生成位置

### 拾取
- [ ] 生成后 0.5 秒内玩家靠近不触发拾取
- [ ] 0.5 秒后玩家靠近自动拾取，物品进入背包
- [ ] 拾取后掉落物被销毁
- [ ] 背包已满时掉落物不被销毁，保留在地面
- [ ] 部分拾取：背包只能容纳部分数量时，拾取能放入的部分，剩余数量更新到掉落物上

### PlayerBlockInteraction
- [ ] 挖掘完成后在方块中心位置生成掉落物
- [ ] 掉落物包含正确的 ItemData 和数量 1
- [ ] 不再直接调用 Inventory.AddItem（移除旧逻辑）
- [ ] 方块无 DropItem 配置时不生成掉落物

### 预制体
- [ ] ItemDrop 预制体存在于 Assets/Prefabs/
- [ ] Rigidbody2D 设置为 Kinematic
- [ ] CircleCollider2D 设置为 Trigger，半径 1.5
- [ ] SpriteRenderer Order in Layer 高于地形

### 通用
- [ ] 编译无错误、无警告
- [ ] 不引入新的控制台错误
- [ ] 符合 `coding-conventions.md` 规范

## 注意事项
- **不做物理掉落**：v1.0 掉落物不受重力影响，直接在方块中心位置浮动。物理弹跳效果留给后续优化
- **不做掉落物合并**：同一位置的多个同类掉落物不自动合并为一个
- **不做拾取音效/动画**：v1.0 不做音效，拾取时直接销毁，不做飞向玩家的动画
- **不做自动消失**：掉落物不会超时消失，永久保留直到被拾取
- **GetCellCenter 复用**：PlayerBlockInteraction 中已有 `GetCellCenter()` 私有方法，直接复用计算掉落物生成位置
- **Inventory 引用保留**：PlayerBlockInteraction 上的 `_inventory` 字段保留不删除，因为放置方块仍需要读取背包
- **OnTriggerStay2D 而非 OnTriggerEnter2D**：使用 Stay 确保玩家站在掉落物上持续检测（如果第一次因背包满拾取失败，腾出空间后下一帧可以拾取）
- **预制体目录**：如果 `Assets/Prefabs/` 目录不存在，需先创建
