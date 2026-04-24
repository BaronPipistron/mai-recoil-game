# Recoil Breach

A playable first-person sci-fi wave-survival prototype built in Unity 6.3 LTS with recoil-driven movement as the core mechanic.

## Unity / Platform
- Unity version: `6000.3.14f1` (Unity 6.3 LTS)
- Target platform: Windows PC
- Input: Keyboard + Mouse
- Render pipeline: URP (Universal Render Pipeline)

## Free Assets Used
1. 50 CC0 Sci-Fi SFX (audio)
2. Handgun Reload Sound Effect (dedicated reload SFX)
3. Orbitron Font (UI)
4. Generic Sci-Fi Metal Texture (arena material)
5. Low Poly Animated Guns (weapon model pack)
6. Robot Pack 3D (legacy fallback enemy models)
7. Animated Mech Pack (primary enemy models)

## Controls
- Move: `WASD`
- Look: `Mouse`
- Fire shotgun: `LMB`
- Reload: `R`
- Jump: `Space`
- Dash (after upgrade): `Left Shift`
- Pause: `Esc`

## Gameplay Overview
- First-person arena shooter with endless waves.
- Shotgun recoil physically pushes the player backward (grounded recoil has no forced upward boost).
- Recoil is tuned for movement tech and evasive repositioning.
- Two enemy types:
  - `Rusher`: faster melee pressure unit.
  - `Bulwark`: slower, tankier frontline.
- Every enemy has a moving weak spot with emissive visual highlighting.
- Weak spot positions shift over time; weak spot hits are more rewarding.
- Arena includes lava and out-of-bounds kill hazard for recoil mismanagement punishment.
- Between waves, player chooses 1 of 3 upgrades.

## Upgrade Themes Included
- Stronger recoil (`Overcharged Shells`)
- Weaker recoil (`Stability Dampener`)
- Directional dash unlock/boost (`Directional Dash` / `Dash Thrusters`)
- Additional practical upgrades: HP boost, pellet damage boost, reload boost

## Architecture Overview
Core scripts are under `Assets/_Game`:
- `Core/`: run state, damage contract, session orchestration
- `Player/`: FPS movement, camera feedback, health
- `Weapons/`: shotgun stats + firing/reload/recoil logic
- `Enemies/`: enemy behavior, weak spot logic, enemy stats
- `Waves/`: spawning, escalation, score
- `Upgrades/`: upgrade generation and application
- `UI/`: HUD, menus, upgrade selection panel
- `Arena/`: hazard volumes
- `Data/`: ScriptableObject balance assets
- `Prefabs/`, `Scenes/`, `Materials/`: generated content

Editor automation:
- `Assets/Editor/ProjectBootstrap.cs` creates/refreshes prefabs, scene, materials, and build settings.
