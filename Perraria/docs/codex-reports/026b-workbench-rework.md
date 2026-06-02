# Task 026b - Workbench Rework Delivery

Branch: `feature/026b-workbench-rework`

## Changed Files

- Added `BlockType.WorkbenchRight = 7` and registered it in `TileRegistry`.
- Updated `PlayerBlockInteraction` for two-cell workbench placement, paired-cell mining, and right-click Workbench UI opening.
- Added `WorkbenchUI` for the independent workbench recipe panel.
- Updated `WorkbenchProximity` so either workbench half counts as the station.
- Added `TileManager.IsInBounds` for safe two-cell placement validation.
- Updated `BlockDataRegistry` and CSV with Workbench/WorkbenchRight hardness and drop behavior.
- Updated `WorkbenchTile.asset` to render at `transform.m00=2`; added transparent `WorkbenchRightTile.asset`.
- Recut `workbench.png` to the selected Kenney roguelike sheet tile and updated its importer.
- Updated `SampleScene.unity`: inventory recipe list is now one Workbench recipe; WorkbenchUI has Wood Sword, Arrow, and Wood Pickaxe recipes; `PlayerBlockInteraction._workbenchUI` is bound.
- Updated `docs/task-board.md` and added `docs/tasks/026b-workbench-rework.md`.

## Runtime Verification Log

- Compile/import: `AssetDatabase.Refresh()` completed with `isCompiling=False`; Unity MCP command compilation succeeded.
- Final Console: `0` errors, `1` warning. The warning is Unity AI Toolkit account/API accessibility noise, not task code.
- Binding inspection:
  `WorkbenchTile sprite=workbench scaleX=2 collider=Grid`.
  `WorkbenchRightTile sprite=null collider=Grid`.
  `TileRegistry workbench=WorkbenchTile right=WorkbenchRightTile`.
  `BlockDataRegistry entries=8`; Workbench `hardness=0.4 drop=Item_Workbench chance=1`; WorkbenchRight `hardness=0.4 drop=null chance=0`.
  `PlayerBlockInteraction workbenchUI=WorkbenchRoot`.
  `WorkbenchUI recipes=3: Recipe_WoodSword, Recipe_WoodArrow, Recipe_WoodPickaxe; slots=3`.
  `InventoryUI recipes=1: Recipe_Workbench; slots=1`.
- Play Mode input harness:
  right-click empty cell placed `Workbench` anchor and `WorkbenchRight` half.
  placement consumed exactly one `Item_Workbench`.
  right-click on `WorkbenchRight` opened independent `WorkbenchUI`.
  right-click on an existing workbench did not place another block.
  occupied right cell blocked two-cell placement and did not consume the item.
  `WorkbenchProximity.IsNearWorkbench()` detected the placed workbench while near.
  `WorkbenchUI` closed after the player moved outside proximity.
  `CraftingService.TryCraft(Recipe_WoodArrow, stationAvailable=true)` consumed 1 Wood and produced 5 Arrows.
  full inventory craft failed atomically: Wood stayed `2`, Arrows stayed `0`.
  save change enumeration after placement returned `changeCount=2` and included both `Workbench` and `WorkbenchRight`.
- Play Mode mining harness:
  mining `WorkbenchRight` cleared both cells.
  drop delta was exactly `1` `Item_Workbench`.

## Review Focus

- Check `PlayerBlockInteraction.PlaceWorkbench` and `MineWorkbench` edge cases around broken pairs, out-of-bounds right cells, and rollback after a failed second `SetBlock`.
- Check scene UI hierarchy after moving three existing recipe slots from InventoryUI into WorkbenchUI.
- Check whether future multi-cell blocks should be generalized after 027 rather than extending this workbench-specific branch.

## Known Notes

- Unity MCP dynamic commands fail on `MethodInfo.Invoke` in this editor session, so runtime verification used Play Mode, `InputSystem.QueueStateEvent(MouseState)`, and `SendMessage("Update")` instead of reflection.
- Console has one Unity AI Toolkit account/API warning after final refresh; no task compile/runtime errors were present.
