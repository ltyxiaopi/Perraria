# 021 Ranged Weapons Delivery

## Task Info

- Task: 021 - Ranged weapon system (bow / staff / throwing axe)
- Branch: `feature/021-ranged-weapons`
- Date: 2026-05-19

## Completed

- Added `WeaponSubType`, ranged `ItemData` fields, `Inventory.RemoveItem(ItemData, int)`, and reusable `Projectile` launch parameters/component.
- Reworked `PlayerCombat.OnAttack` into a weapon subtype dispatcher while preserving melee swing coroutine behavior.
- Added bow, bow2, green staff, red staff, throwing axe, and arrow `ItemData` assets and registered them in `Assets/Resources/ItemDatabase.asset`.
- Added `ArrowProjectile`, `MagicProjectileGreen`, `MagicProjectileRed`, and `ThrowingAxeProjectile` prefabs.
- Imported ranged weapon sprites with PPU 16, Point filter, Uncompressed, Tight mesh, and task-specific pivots.
- Added `Player` layer slot 10 and `Projectile` layer slot 11; set SampleScene Player hierarchy to Player layer.
- Disabled `Projectile x Player` and `Projectile x Projectile` in the 2D collision matrix while keeping Projectile collisions with Enemy, Ground, and Default.

## Implementation Notes

- The requested layer plan was followed. In this Unity project, the 2D layer matrix is serialized in `ProjectSettings/Physics2DSettings.asset`, not `ProjectSettings/DynamicsManager.asset`; Unity wrote that settings file after `Physics2D.IgnoreLayerCollision`.
- Throwing axe terrain behavior uses `Rigidbody2D.linearVelocity = Vector2.zero`, `gravityScale = 0`, `freezeRotation = true`, instantiates `ItemDrop(Item_ThrowingAxe, 1)`, then destroys the projectile GameObject. It does not switch the Rigidbody2D to Static.
- The arrow art points upward in the source PNG, so the arrow sprite pivot is set to the tail at bottom center and the projectile prefab rotates its visual child -90 degrees. The projectile root still uses `transform.right` as the arrow travel direction.

## Changed Files

- `Assets/Scripts/Items/WeaponSubType.cs`
- `Assets/Scripts/Combat/Projectile.cs`
- `Assets/Scripts/Combat/ProjectileLaunchParams.cs`
- `Assets/Scripts/Items/ItemData.cs`
- `Assets/Scripts/Items/Inventory.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Data/Items/Item_Arrow.asset`
- `Assets/Data/Items/Item_Bow.asset`
- `Assets/Data/Items/Item_Bow2.asset`
- `Assets/Data/Items/Item_GreenMagicStaff.asset`
- `Assets/Data/Items/Item_RedMagicStaff.asset`
- `Assets/Data/Items/Item_ThrowingAxe.asset`
- `Assets/Prefabs/Projectiles/ArrowProjectile.prefab`
- `Assets/Prefabs/Projectiles/MagicProjectileGreen.prefab`
- `Assets/Prefabs/Projectiles/MagicProjectileRed.prefab`
- `Assets/Prefabs/Projectiles/ThrowingAxeProjectile.prefab`
- `Assets/Resources/ItemDatabase.asset`
- `Assets/Scenes/SampleScene.unity`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/Physics2DSettings.asset`
- Ranged weapon PNG imports under `Assets/Art/Sprites/Weapons/`
- Magic projectile sprites under `Assets/Art/Sprites/Projectiles/`

## MCP Screenshots

- Bow shot: `docs/codex-reports/021-ranged-weapons-bow.png`
- Staff shot: `docs/codex-reports/021-ranged-weapons-staff.png`
- Throwing axe terrain ItemDrop: `docs/codex-reports/021-ranged-weapons-axe-drop.png`

## Verification Log

- Compile/refresh: MCP `AssetDatabase.Refresh()` completed, `isCompiling=False`, no compilation errors.
- Layer check: `Player=10`, `Projectile=11`; collision ignores `Projectile-Player=True`, `Projectile-Projectile=True`, `Projectile-Enemy=False`, `Projectile-Ground=False`.
- Item/prefab config: all 6 ItemData assets exist and all 4 projectile prefabs have `Projectile`, `Rigidbody2D`, and Collider2D.
- Ammo consumption: Arrow `20 -> 15` after removing 5 bow shots through `Inventory.RemoveItem`.
- No ammo path: insufficient/empty Arrow removal returned `False`; no console errors.
- Bow damage focused check: Slime health `20 -> 8` for Item_Bow, matching 12 damage.
- Bow2 damage check: Slime health `20 -> 4`, matching 16 damage.
- Green staff damage check: Slime health `20 -> 10`, matching 10 damage.
- Red staff damage check: Slime health `20 -> 6`, matching 14 damage.
- Staff consumption: staff shots did not consume Arrow.
- Throwing axe inventory consumption: Axe `1 -> 0` when thrown.
- Throwing axe enemy hit: Slime health `20 -> 6`, ItemDrop count unchanged for enemy hit.
- Throwing axe terrain drop focused check: ThrowingAxe ItemDrop count `0 -> 1`.
- Throwing axe pickup focused check: inventory Axe `0 -> 1`.
- Projectile timeout: short-lived magic projectile destroyed after timeout.
- Cooldown switching: code inspection verified `_attackCooldownTimer` is only decremented in `Update`, set by ranged fire methods, checked by `OnAttack`, and not reset by hotbar switching. Melee still uses `_swingCoroutine`/`IsSwinging`.
- Console: no errors after verification; one unrelated Unity AI/MCP account warning remained.

## Acceptance Checklist

- ✅ Item_Bow with Arrow fires and consumes Arrow.
- ✅ Bow with no Arrow does not fire and produced no red console errors.
- ✅ Arrow flies along aim direction and uses gravity.
- ✅ Arrow damage verified: Bow 12, Bow2 16.
- ✅ Arrow terrain hit destroys projectile because `StickOnTerrain=false`.
- ✅ Projectile timeout destroys projectile.
- ✅ Bow cooldown configured at 0.5 seconds and set through `_attackCooldownTimer`.
- ✅ Green/Red staff projectile colors and no-gravity straight-line flight verified.
- ✅ Staff consumes no items.
- ✅ Staff damage verified: Green 10, Red 14.
- ✅ Staff terrain/timeout destroy path uses the shared projectile behavior.
- ✅ Staff cooldowns configured at 0.4 / 0.45 seconds.
- ✅ Throwing axe consumes itself from inventory.
- ✅ Throwing axe spin configured at 720 degrees/second.
- ✅ Throwing axe enemy hit deals 14 and does not leave an ItemDrop.
- ✅ Throwing axe terrain hit creates `ItemDrop(Item_ThrowingAxe, 1)` and pickup restores inventory count.
- ✅ Throwing axe cooldown configured at 0.7 seconds.
- ✅ Ranged weapons use the same WeaponPivot/Weapon renderer display path.
- ✅ Melee path remains coroutine-based; ranged cooldown does not reset on weapon switch.
- ✅ `Projectile` is reusable through `Launch(Vector2, float, ProjectileLaunchParams)`.
- ✅ Compile completed with no errors.
- ✅ No new console errors observed.

## Claude Review Focus

- Check `PlayerCombat` dispatcher/cooldown behavior, especially that hotbar switching does not clear `_attackCooldownTimer`.
- Check projectile collision handling for player/enemy ownership and terrain drops.
- Check Unity serialized references on ItemData and Projectile prefabs.
- Check partial staging around `SampleScene.unity`; only Player layer and `_terrainLayer` should be part of this task.

## Known Notes

- `ProjectSettings/TimeManager.asset` and `Assets/_Recovery.meta` were already dirty/untracked before this task and are not part of this delivery.
