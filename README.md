# Monster Miner (MVP)

First-person physics mining game built in Unity 6 with the **Universal Render Pipeline (URP)**. Press Play in any scene — the runtime bootstrap builds the cavern, player, shop, and core loop automatically.

## Requirements

- Unity **6000.5.1f1** (or compatible Unity 6 LTS)
- Universal Render Pipeline **17.5.0** (installed via Package Manager / `manifest.json`)
- Windows PC target

## Open and Play

1. Open this folder in Unity Hub as a project.
2. Open `Assets/Scenes/Cavern.unity` (optional — bootstrap runs in any scene).
3. Press **Play**.

## Controls

| Input | Action |
|-------|--------|
| WASD | Move |
| Mouse | Look |
| Space | Jump |
| Left click | Swing pickaxe / weapon |
| E | Interact (pickups, shop listings, sell counter, slot machine) |
| 1–9 | Select inventory slot |
| [ / ] | Reorder adjacent inventory slots |

## Core Loop

1. Mine **rock nodes** for $ and exposed eggs.
2. Break **Monster Eggs** to hatch random monsters (physics launch).
3. Fight monsters in melee; blood marks appear on the floor.
4. Pick up **drops** and sell at the **shop counter**, or gamble one drop in the **slot machine**.
5. Buy upgrades on the **shop board** (pickaxe, weapon, HP, inventory, cavern expansion).
6. On death: keep $, drop inventory where you died, monsters despawn, respawn at start.

## Project Layout

- `Assets/Scripts/Core/` — bootstrap, context, database
- `Assets/Scripts/Player/` — FPS controller, combat, interaction
- `Assets/Scripts/World/` — cavern, rocks, eggs, spawning
- `Assets/Scripts/Combat/` — monsters, blood
- `Assets/Scripts/Economy/` — shop, slot machine, selling
- `Assets/Scripts/Inventory/` — currency, items, pickups
- `Assets/Scripts/UI/` — HUD

Data is created at runtime via `GameDatabase.CreateRuntimeDefaults()` so no ScriptableObject assets are required for MVP play.
