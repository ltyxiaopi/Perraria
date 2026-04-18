# 任务 012 补充记录 - 史莱姆胶囊体后续调参

## 目的

这份记录只覆盖任务 012 完成后的后续微调，供 Claude 审查时快速了解当前史莱姆碰撞体状态。

## 本次改动

- 将 `Assets/Prefabs/Enemies/Slime.prefab` 的 `CapsuleCollider2D` 调整为：
  - `offset = (-0.40, -0.03)`
  - `size = (1.25, 1.03)`
- 将 `SampleScene` 中 `Enemies` 根节点下的 12 只 `Slime` 场景实例同步为同一组 `CapsuleCollider2D` 参数，避免 prefab 与场景覆盖不一致。

## 未改动内容

- 没有改动主角当前碰撞体参数。
- 主角当前仍保持用户手调后的状态：
  - `CapsuleCollider2D.offset = (0.00, 0.04)`
  - `CapsuleCollider2D.size = (0.82, 1.30)`
  - `GroundCheck.localPosition = (0.00, -0.66, 0.00)`
- 没有改动史莱姆的缩放。
- 没有改动史莱姆的 `GroundCheck`，仍为：
  - `GroundCheck.localPosition = (0.00, -0.66, 0.00)`

## 当前史莱姆相关参数

- `Transform.localScale = (0.65, 0.55, 1.00)`
- `CapsuleCollider2D.offset = (-0.40, -0.03)`
- `CapsuleCollider2D.size = (1.25, 1.03)`
- `Sprite = slime_idle1_purple_0`

## 说明

- 这次调整是根据用户直接给定的胶囊体参数执行的。
- 本次目标是继续压低史莱姆贴地观感，并保持 prefab 与 12 只场景实例完全一致。
- 如果 Claude 后续仍观察到“视觉悬空”，优先怀疑贴图透明留白，而不是本次胶囊体参数没有同步。
