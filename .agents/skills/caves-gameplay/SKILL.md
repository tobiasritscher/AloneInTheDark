---
name: caves-gameplay
description: Change or debug controls, physics, UI, saves and game flow in Alone in the Caves. Use for gameplay fixes and phone playtest feedback, not signing or store uploads.
---

# Change the game without breaking scene wiring

Read [AGENTS.md](../../../AGENTS.md) and the script map in [README.md](../../../README.md).
The Unity root is `Alone in the Dark/`; game code is under `Assets/Scripts/`.

## Find the behaviour

- `PlayerController`: state machine, physics, score, level lifecycle and UI layout.
  States: Title, Intro, Ready, Playing, Paused, GameOver, Won.
- `GameInput`: desktop axes and floating touch stick. `TapDown` excludes UI hits;
  `Move` currently does not, so reproduce UI-touch interference before changing it.
- `GameSave`: PlayerPrefs keys, local-date daily ID, progress, scores and mute setting.
- `CameraFit`: constant minimum view width across aspect ratios.
- `GameAudio`: resource loading, two AudioSources, persistent audio object.

Read the scene's serialized references when a field, button callback or object is
involved. `Restart()` and `exitGame()` are still referenced by the scene. UI is
repositioned/cloned in `BuildUi`, so changing only scene positions may do nothing.

## Preserve the play loop

Keep input sampling in `Update` and forces in `FixedUpdate`. The Rigidbody moves
in the XY plane; Unity 6 uses `linearVelocity`. Velocity limiting must still allow
steering against wind/gravity. Overlapping wind zones are tracked independently.

Retry rebuilds a level without loading the scene. Pickups in scene chapters are
hidden and restored; generated levels are destroyed/rebuilt. Reset drag and wind
state when leaving play. Check `Time.timeScale` on pause, retry and return to title.

Daily and endless share `EndlessCave`: endless retry changes the seed; daily retry
keeps it. The date is currently local time, not a globally synchronized UTC day.
Treat changes to daily identity or existing save keys as data/behaviour migrations.

## Verify the affected path

Use [docs/PLAYTEST.md](../../../docs/PLAYTEST.md) to select the relevant manual checks.
Reproduce the triggering case, then check retry and return-to-menu with the same input.
For scene/UI changes verify live references, phone aspect ratio and touch targets.
For physics changes verify both a scene chapter and a generated chapter, plus wind.

Compile with the pinned Unity version after C# changes. A successful iOS export
does not prove runtime behaviour. Report separately what was compiled, exercised in
Play Mode, or tested on an iPhone. Keep notch layout and background/resume behaviour
as unverified until actually tested; no safe-area handling exists in the current UI.
