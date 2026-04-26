# 任务 015 - 玩家血量 UI（左上角心形）

## 目标
在 HUD 左上角实时显示玩家生命值，采用 Terraria 风格心形图标：
**5 颗心 = 100 HP，每颗心代表 20 HP，支持半心**。
血量变化时立即更新，受伤时心形减少，回血时恢复。

## 接口签名

```csharp
[DisallowMultipleComponent]
public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;   // 拖入 Player 上的 PlayerHealth
    [SerializeField] private Image[] _heartImages;         // 5 个心形 Image，从左到右
    [SerializeField] private Sprite _heartFull;            // 满心 = heartDisplay_0
    [SerializeField] private Sprite _heartEmpty;           // 空心 = heartDisplay_1
    [SerializeField] private Sprite _heartHalf;            // 半心 = heartDisplay_2

    // HP per heart 是固定常量 = 20，在脚本中以 const 形式存在
}
```

订阅 `PlayerHealth.OnHealthChanged(currentHealth, maxHealth)`，按下式渲染每颗心：
```
heartIndex = 0..4
hpForThisHeart = clamp(current - heartIndex * 20, 0, 20)
sprite = hpForThisHeart >= 20 ? full : hpForThisHeart >= 10 ? half : empty
```

## 依赖
- 任务 010 PlayerHealth（已完成，已发布 `OnHealthChanged` 事件）

## 文件清单
- `Assets/Scripts/UI/PlayerHealthUI.cs` — 新增
- `Assets/Art/UI/heartDisplay.png` — **已由用户放置**，横向排列 3 帧雪碧图：
  - Frame 0（左）：**满血心形**（红色）
  - Frame 1（中）：**空血心形**（深色）
  - Frame 2（右）：**半血心形**（左半红、右半深色）
  - **注意**：顺序是 满 / 空 / 半，**不是** 满 / 半 / 空
- 资源导入配置（**通过 MCP 操作**）：
  - `TextureType = Sprite (2D and UI)`
  - `SpriteMode = Multiple`
  - `FilterMode = Point (no filter)`，避免像素艺术模糊
  - `Compression = None`（小图无所谓压缩）
  - `PixelsPerUnit` 与项目其它 Sprite 一致（参考现有 weapon / slime 配置）
  - **Sprite Editor 切割**：均分为 3 列 1 行，**必须裁掉每帧的透明 padding**（参考记忆 `project_sprite_slicing.md`：
    若不裁掉透明边，UI 中显示会偏小或偏移）
  - 切割完成后产出 3 个子 sprite，按导入器自动命名（如 `heartDisplay_0 / _1 / _2`）
- 场景配置（**通过 MCP 操作**）：
  - 在 Canvas 下新增子物体 `HUD/HealthBar`，`RectTransform` 锚点设为左上 `(0,1)`，pivot `(0,1)`，
    位置 `(20, -20)`（距离左边 20px、距离上边 20px）
  - 在 `HealthBar` 下新建 5 个子 `Image`，水平横向排列，间距 4px，单个尺寸 32×32（保持像素艺术风格）
  - 挂 `PlayerHealthUI` 组件，绑定：
    - `_playerHealth` → Player 上的 PlayerHealth
    - `_heartImages` → 5 个 Image
    - `_heartFull` → `heartDisplay_0`（满血帧）
    - `_heartEmpty` → `heartDisplay_1`（空血帧）
    - `_heartHalf` → `heartDisplay_2`（半血帧）

## 验收标准
- [ ] 启动游戏后左上角立即显示 5 颗满心
- [ ] 按 P 键扣 10 HP 后最右一颗心变成半心
- [ ] 再按 P 键扣 10 HP 后最右一颗心变成空心
- [ ] 完全损失 5 颗心后所有心都为空（HP=0 时刚好触发死亡，5 颗都空）
- [ ] 自然回血时心形按顺序恢复
- [ ] 重生后 5 颗满心立即显示
- [ ] 心形 UI 不被快捷栏 / 背包遮挡（render 顺序合理）

## 注意事项
- **HP/heart = 20 是硬编码常量**，未来 maxHealth 改成 200 时需要扩展（例如改为 10 颗心或半心代表更小数值）；
  当前 maxHealth=100 不必处理这种情况
- **不要每帧 `GetComponent<PlayerHealth>().CurrentHealth` 轮询**，必须订阅 `OnHealthChanged` 事件
- **`OnEnable` 订阅、`OnDisable` 解绑**，避免场景切换时悬空回调
- **首次显示**：Awake / OnEnable 时主动调一次 `RefreshUI(player.CurrentHealth, player.MaxHealth)`，
  防止事件错过初始值
- 心形美术风格：与现有像素 UI（hotbar 槽位）一致，红色 / 灰色描边，避免突兀

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/015-player-health-ui.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
