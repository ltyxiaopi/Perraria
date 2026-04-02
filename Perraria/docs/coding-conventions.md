# Perraria - 编码规范

本文档是 AGENTS.md 中编码规范部分的详细版。Claude Code 和 Codex 共同遵守。

## 命名规范

| 类别 | 风格 | 示例 |
|------|------|------|
| 类/结构体 | PascalCase | `PlayerController`, `TileData` |
| 公共方法/属性 | PascalCase | `TakeDamage()`, `CurrentHealth` |
| 私有字段 | _camelCase | `_moveSpeed`, `_rigidBody` |
| 局部变量/参数 | camelCase | `tilePos`, `damage` |
| 常量 | PascalCase | `MaxHealth`, `TileSize` |
| 枚举类型和值 | PascalCase | `ItemType.Weapon` |
| 接口 | I + PascalCase | `IDamageable`, `IInteractable` |
| 事件 | On + PascalCase | `OnHealthChanged`, `OnItemPickup` |

## 文件与目录

- 每个 `.cs` 文件只包含一个类
- 文件名与类名完全一致
- 脚本放入 `Assets/Scripts/` 下对应模块目录
- Editor 脚本放入 `Assets/Scripts/Editor/`
- ScriptableObject 定义放入对应模块目录，资产实例放入 `Assets/Data/`

## Unity 规范

### 字段暴露
```csharp
// 推荐：SerializeField + private
[SerializeField] private float _moveSpeed = 5f;

// 避免：直接 public
public float moveSpeed = 5f; // 不要这样
```

### 组件缓存
```csharp
// 推荐：Awake 中缓存
private Rigidbody2D _rb;

private void Awake()
{
    _rb = GetComponent<Rigidbody2D>();
}

// 避免：Update 中获取
private void Update()
{
    GetComponent<Rigidbody2D>().linearVelocity = ...; // 不要这样
}
```

### 标签比较
```csharp
// 推荐
if (other.CompareTag("Player")) { }

// 避免
if (other.tag == "Player") { } // 不要这样
```

### MonoBehaviour 生命周期顺序
```csharp
public class ExampleBehaviour : MonoBehaviour
{
    #region Unity Lifecycle

    private void Awake() { }      // 组件引用初始化
    private void OnEnable() { }   // 事件订阅
    private void Start() { }      // 依赖其他对象的初始化
    private void Update() { }     // 每帧逻辑
    private void FixedUpdate() { } // 物理相关
    private void OnDisable() { }  // 事件取消订阅
    private void OnDestroy() { }  // 清理资源

    #endregion

    #region Public Methods
    // ...
    #endregion

    #region Private Methods
    // ...
    #endregion
}
```

## 代码风格

- 方法不超过 30 行
- 大括号换行风格（Allman style）
- 只在逻辑不明显时写注释
- 不写 `// TODO` 进代码，待办事项走任务看板
- 不用 `#region` 嵌套

## Git 规范

- 分支: `feature/任务编号-简述` — `feature/001-player-movement`
- 提交信息: 英文，动词开头，首字母大写 — `Add player horizontal movement`
- 一个提交一个逻辑变更
