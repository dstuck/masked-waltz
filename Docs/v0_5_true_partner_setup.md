# v0.5 Setup: True Partner + Win Hearts

## Add the controller (so you can assign particles)

In `Assets/Scenes/Gameplay.unity`:

- Create an empty GameObject named `GameController` (at the scene root).
- Add component `TruePartnerGameController` (`Assets/Scripts/Game/TruePartnerGameController.cs`).

This is where you will assign the win heart particle prefab.

## Wire references (required)

Select `GameController` → `TruePartnerGameController` and assign:

- **_player**: drag the `Player` GameObject (has `PlayerController`)
- **_beatPulse**: drag `Canvas/BeatPulseUI` (has `BeatPulseUI`)

Optional:

- **_winText**: create a `TextMeshProUGUI` under the `Canvas` (start disabled) and drag it here
- **_winParticlesPrefab**: drag your heart particle prefab here
- **_showTargetMarker** + **_targetMarkerSprite**: debug-only target marker (off by default)

## Heart sprite on Particle System (Shuriken)

If you want each particle to *look like* a heart:

1. Select your particle **Material**.\n   - Find its main texture slot (usually **Base Map** or **MainTex**) and assign your heart texture.\n2. Select your `ParticleSystem`.\n   - In **Renderer**, set **Material** to that heart material.\n   - Use **Render Mode = Billboard**.

## Troubleshooting: pulse color not changing

`BeatPulseUI` tints **all Images/RawImages under the pulse object** by default (so multi-layer pulse frames tint consistently).

- If you want explicit control, assign the set of images to `BeatPulseUI._tintTargets` (all 4 images in your frame).

