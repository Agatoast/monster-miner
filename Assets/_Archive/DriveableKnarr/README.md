# Driveable Knarr Archive (Sep 2026)

Saved snapshot of the **driveable Warrenson knarr** work before reverting to the original static Viking ship.

## What was archived

- **10× scaled knarr** with hull-fit walk deck, helm, sail trim, shore sand semicircle
- **Driveable boat** — deck cargo mode, mast helm sailing, water-only movement
- **Underwater hull fixes** — shader clip, mesh clip, renderer hide (`BoatHullWaterClip`, `BoatHullWaterVisibility`)
- **Deck landing fixes** — mast-clear spawn offset, rigidbody sync

## Files in this folder

| File | Original path |
|------|----------------|
| `Scripts/KnarrVisualFactory.driveable-archive.cs.txt` | `Assets/Scripts/Util/KnarrVisualFactory.cs` |
| `Scripts/DriveableBoat.driveable-archive.cs.txt` | `Assets/Scripts/Player/DriveableBoat.cs` |
| `Scripts/BoatDeckInteract.driveable-archive.cs.txt` | `Assets/Scripts/Player/BoatDeckInteract.cs` |
| `Scripts/BoatHelmInteract.driveable-archive.cs.txt` | `Assets/Scripts/Player/BoatHelmInteract.cs` |
| `Scripts/BoatHelmDisplay.driveable-archive.cs.txt` | `Assets/Scripts/UI/BoatHelmDisplay.cs` |
| `Scripts/BoatHullWaterVisibility.driveable-archive.cs.txt` | `Assets/Scripts/Util/BoatHullWaterVisibility.cs` |
| `Scripts/PlayerVehicleMount.driveable-archive.cs.txt` | `Assets/Scripts/Player/PlayerVehicleMount.cs` (boat sections) |
| `Shaders/BoatHullWaterClip.driveable-archive.shader.txt` | `Assets/Shaders/BoatHullWaterClip.shader` |

## Active runtime (after revert)

- `KnarrVisualFactory.cs` — original simple spawn: `d_knarr_wood` prefab, URP materials, hull box collider, parented under `WarrensonsLake`
- No `DriveableBoat` component on spawn (scripts remain in project but unused until restored)

## How to restore

1. Copy archive `.cs.txt` files back to their original paths (remove `.driveable-archive.cs.txt` suffix).
2. Copy shader archive back to `Assets/Shaders/BoatHullWaterClip.shader`.
3. Replace `KnarrVisualFactory.cs` with the archived driveable version.
4. Update `LakeBuilder.cs` to call `KnarrVisualFactory.CreateAtBeach(parent, LakeCatalog.GetBoatBeachContentLocal(...))` with driveable offsets.
5. Restore `GameBootstrap` references to `KnarrVisualFactory.BoatVerticalOffsetFeet` if needed.
6. Stop Play → Play to rebuild the world.

## Key constants (driveable version)

- `BoatScale = 10f`
- `BoatVerticalOffsetFeet = -30f`
- `BoatNorthOffsetFeet = 100f`
- `DeckAboveWaterSurfaceFeet = 1.35f`
- `DeckCargoEntryAftOffsetFeet = 14f`
