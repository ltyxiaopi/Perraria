# 任务 XXX - 任务标题

## 目标
简要描述这个任务要实现什么。

## 美术资源前置检查（Claude 派任务前必填）
- **是否需要新素材**: 是 / 否（如否，删除本节剩余内容）
- **素材清单**:
  - [ ] `<资源文件名>` — 已落位 `Assets/Art/<目录>/`，来源链接：
- **切片/动画规格**（Claude 用 MCP 看图后填写）:
  - 单帧尺寸：__×__
  - 动画段：Idle 帧 [0-N] / Attack 帧 [N+1-M] / ...
- **授权与署名**: 商用 OK / 是否需要署名 / 是否计入致谢

> 没完成本节就把任务派给 Codex，会导致 Codex 自行做占位 → 后续返工。Claude 自查。

## 接口签名
```csharp
// 类名、方法签名、参数和返回值
public class ClassName : MonoBehaviour
{
    public void MethodName(Type param);
}
```

## 依赖
- 依赖的模块或接口列表

## 文件清单
- `Assets/Scripts/Module/FileName.cs` — 简要说明

## 验收标准
- [ ] 标准 1
- [ ] 标准 2

## 注意事项
- 特别需要注意的约束或边界情况

## 交付记录（Codex 必填）
完成任务并自测通过后，**push 分支前**必须在 `docs/codex-reports/<任务编号>-<任务名>.md`
写一份交付记录，参考 `docs/codex-reports/README.md` 的结构。Claude 审查时会先读这份记录，
没写视为未完成，审查不通过。
