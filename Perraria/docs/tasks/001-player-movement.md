# 任务 001 - 玩家基础移动

## 目标
实现玩家角色的水平移动和跳跃，使用 Unity Input System (New) 读取输入，Rigidbody2D 驱动物理移动。

## 接口签名
```csharp
// Assets/Scripts/Player/PlayerController.cs
public class PlayerController : MonoBehaviour
{
    // Inspector 配置
    [SerializeField] private float _moveSpeed;    // 水平移动速度
    [SerializeField] private float _jumpForce;    // 跳跃力度

    // 地面检测
    [SerializeField] private Transform _groundCheck;    // 地面检测点
    [SerializeField] private float _groundCheckRadius;  // 检测半径
    [SerializeField] private LayerMask _groundLayer;    // 地面图层

    public bool IsGrounded { get; }  // 是否在地面上
}
```

## 依赖
- Unity Input System (已引入项目)
- Rigidbody2D、BoxCollider2D 组件
- 需要一个 "Ground" Layer

## 文件清单
- `Assets/Scripts/Player/PlayerController.cs` — 玩家移动控制

## 验收标准
- [ ] 按 A/D 或左右箭头水平移动，松开即停
- [ ] 按 Space 跳跃，仅在地面时可跳
- [ ] 不能在空中二段跳
- [ ] 移动方向时角色 SpriteRenderer 水平翻转
- [ ] 使用 FixedUpdate 处理物理移动
- [ ] 所有参数通过 SerializeField 在 Inspector 中可调

## 注意事项
- 使用 Input System 的 PlayerInput 组件或直接读取 InputAction
- 地面检测用 Physics2D.OverlapCircle
- 不要在此任务中处理攻击、挖掘等其他输入
