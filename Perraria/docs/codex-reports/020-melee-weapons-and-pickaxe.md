# 020-melee-weapons-and-pickaxe

## Task Info
- Task: 020 - melee weapon expansion + wood pickaxe tool
- Branch: `feature/020-melee-weapons-and-pickaxe`
- Spec: `docs/tasks/020-melee-weapons-and-pickaxe.md`

## Completed
- Added `ItemData.MiningSpeedMultiplier` with default `1f`.
- Updated `PlayerBlockInteraction.StartMining()` to use selected Tool multiplier, with hand-mining fallback at base speed.
- Added `Item_Pickaxe_Wood` and 20 new melee weapon assets from the task table. Existing `Item_KnightSword` remains the baseline weapon, so the database now contains 21 weapons total.
- Registered all new items in `ItemDatabase`, moved the database to `Assets/Resources/ItemDatabase.asset`, and verified `Resources.Load<ItemDatabase>("ItemDatabase")`.
- Configured task sprites to PPU 16, Point filter, Uncompressed, Single, bottom pivot. Physically trimmed padded PNGs so Single sprite rects match actual pixels.
- Updated `PlayerCombat.RefreshWeaponRenderer()` to also display selected Tool icons in the existing `WeaponPivot/Weapon` renderer without allowing tools to attack.
- Added `SaveData.CreateNewGameDefault()` and wired MainMenu Start/New Game through delete -> create default save -> save -> load in SampleScene.
- Replaced obsolete `FindFirstObjectByType` calls with `FindAnyObjectByType` in `GameStateSnapshot` to keep compile output warning-free.

## Decisions
- Tool hand rendering: merged into `PlayerCombat`.
  Reason: both weapons and tools use the same `WeaponPivot/Weapon` SpriteRenderer. Keeping the logic in one component avoids two subscribers racing to write `sprite/enabled`, and the change is smaller than adding `PlayerToolRenderer`.
- ItemDatabase runtime access: `Resources.Load<ItemDatabase>("ItemDatabase")`.
  Reason: `SaveData.CreateNewGameDefault()` is a static data factory and must not hardcode ItemIds. Moving the existing database asset to `Assets/Resources/ItemDatabase.asset` preserves its GUID and scene references while giving the factory a stable runtime lookup.

## Changed Files
- `Assets/Scripts/Items/ItemData.cs`
- `Assets/Scripts/Player/PlayerBlockInteraction.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
- `Assets/Scripts/Save/SaveData.cs`
- `Assets/Scripts/Save/GameStateSnapshot.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Resources/ItemDatabase.asset`
- `Assets/Data/Items/Item_Pickaxe_Wood.asset`
- `Assets/Data/Items/Item_RustySword.asset` through `Item_Spear.asset` from the task table, excluding ranged items and `Item_ThrowingAxe`
- `Assets/Art/Sprites/Tools/tool_pickaxe_wood.png`
- task melee weapon PNG/meta files used by the 20 new melee assets

## Validation
- Unity compile check:
  `020 final compile check: databaseLoaded=[True] itemCount=[26] duplicateIds=[0]`
- Static item database check:
  `databaseLoaded=True total=26 weapons=21 tools=1 duplicates=0`
- Wood pickaxe:
  `id=6 type=Tool maxStack=1 MiningSpeedMultiplier=1.5 icon=tool_pickaxe_wood`
- Knight sword baseline:
  `id=5 type=Weapon damage=10 duration=0.35`
- Import checks:
  `tool_pickaxe_wood.png texture=12x14 spriteRect=(0,0,12,14) pivot=(6,0) ppu=16 filter=Point compression=Uncompressed mode=Single worldSize=0.75x0.875`
  `weapon_cleaver.png texture=8x19 spriteRect=(0,0,8,19) pivot=(4,0) ppu=16 filter=Point compression=Uncompressed mode=Single`
  `weapon_waraxe.png texture=12x23 spriteRect=(0,0,12,23) pivot=(6,0) ppu=16 filter=Point compression=Uncompressed mode=Single`
- Mining duration measured from runtime item data and block hardness:
  Wood pickaxe speed = `1.0 * 1.5 = 1.5`; Stone = `2.0 / 1.5 = 1.33s`; Dirt = `0.5 / 1.5 = 0.33s`.
  Weapon or empty slot fallback speed = `1.0`; Stone = `2.00s`; Dirt = `0.50s`.
- Weapon differentiation sample:
  Knight Sword: damage `10`, range `1.4`, arc `100`, duration `0.35`, knockback `6`.
  Waraxe: damage `17`, range `1.4`, arc `110`, duration `0.48`, knockback `10`.
  Knife: damage `5`, range `0.9`, arc `70`, duration `0.20`, knockback `2`.

## MCP Screenshots
- Screenshot 1: `Unity_SceneView_CaptureMultiAngleSceneView`, focus Player, state `tool_pickaxe_wood` after resize, 1024x1024 PNG, size 25354 bytes. Shows full Player + Pickaxe; pickaxe world size is `0.75x0.875`, below the measured player height `1.3125`.
- Screenshot 2: `Unity_SceneView_CaptureMultiAngleSceneView`, focus Player, state `weapon_knight_sword` with pivot z `315`, 1024x1024 PNG, size 30112 bytes. Shows knight sword swing pose.
- Screenshot 3: `Unity_SceneView_CaptureMultiAngleSceneView`, focus Player, state `weapon_waraxe`, 1024x1024 PNG, size 25351 bytes. Shows weapon switch visual with a different held weapon.

## New Game JSON Dump
`SaveSystem.SaveFilePath`: `C:/Users/Administrator/AppData/LocalLow/DefaultCompany/Perraria/save.json`
File size: `3930` bytes. Selected hotbar index: `0`. Slot 0: ItemId `6` x1 (`Item_Pickaxe_Wood`). Slot 1: ItemId `5` x1 (`Item_KnightSword`).

```json
{
    "Version": 1,
    "SavedAtIso": "2026-05-11T15:17:49.0283349Z",
    "Player": {
        "Position": {
            "x": 0.0,
            "y": 0.0,
            "z": 0.0
        },
        "CurrentHealth": 100,
        "MaxHealth": 100,
        "FacingRight": true
    },
    "Inventory": {
        "Slots": [
            {
                "ItemId": "6",
                "Count": 1
            },
            {
                "ItemId": "5",
                "Count": 1
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            },
            {
                "ItemId": "",
                "Count": 0
            }
        ],
        "SelectedHotbarIndex": 0
    },
    "World": {
        "Seed": 360431063,
        "PlayerEdits": []
    },
    "Spawner": {
        "SpawnTimer": 3.0
    }
}
```

## Claude Review Focus
- `SaveData.CreateNewGameDefault()` uses item asset names through `Resources.Load`, not hardcoded ItemIds.
- `GameManager.QueueNewGameFromInitialSave()` only loads the just-created new-game save when MainMenu requests it; direct SampleScene editor launch remains a plain new game.
- `PlayerCombat.RefreshWeaponRenderer()` has mutually exclusive Weapon/Tool/hidden branches.
- Confirm the task table intentionally results in 20 new weapon assets plus existing `Item_KnightSword` = 21 total melee weapons; `Item_ThrowingAxe` was not created.

## Known Notes
- Review follow-up: `Assets/Art/Sprites/Tools/tool_pickaxe_wood.png` was replaced with a small 12x14 pixel source and reimported. Final spriteRect matches texture size `(0,0,12,14)`, PPU is 16, and world width is `0.75`, below the requested `< 1.5`.
- Unity Console still contains Unity account/network errors: `UnityConnectWebRequestException: Token Exchange failed` and `Account API did not become accessible within 30 seconds`. These were observed before this task and are unrelated to project compile/runtime code.
- One attempted `Unity_Camera_Capture` with a stale camera instance ID logged an MCP tool error before switching to `Unity_SceneView_CaptureMultiAngleSceneView`. The final compile check completed successfully with empty compilation logs.
