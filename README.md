# Alone in the Caves

A small Unity game about a spark of light trying to get out of a cave system. Steer by
dragging anywhere on the screen. Touch a wall and you go out.

Built for short sessions: no tutorial, no menus to navigate, no loading between attempts.
One tap starts a run and one tap retries it.

**Verified release, 2026-09-22:** iOS **0.5.0 (1)** compiled, signed, uploaded and
reached **Testing** in internal TestFlight. Device gameplay still needs a playtest.
[TestFlight / App Store Connect](https://appstoreconnect.apple.com/apps/6814689953/testflight)
· [Build instructions](docs/IOS.md) · [Playtest checklist](docs/PLAYTEST.md)

The Git repository and Unity folder retain the original name, `AloneInTheDark` /
`Alone in the Dark`. The product name is **Alone in the Caves**.

## Modes

| Mode | What it is |
| --- | --- |
| Story | Six chapters, one line of story each. Progress is saved, so Continue picks up where you stopped. |
| Endless | A procedural cave that narrows and pulls harder the further you get. New seed every run. |
| Daily | One seeded cave per local calendar date, with its own best score. Devices on different dates can have different caves. |

## Running it

Open the `Alone in the Dark` folder in Unity **6000.6.2f1** and press play. `GameScene` is the only
build scene (`Assets/Scenes/GameScene.unity`). The project uses the built-in render pipeline,
uGUI and the legacy Input Manager. Do not convert to URP/HDRP just to open it.

In the editor and in development builds a few shortcuts exist that are compiled out of
release builds: `G` toggles gravity, `N` skips a chapter, `1`-`6` jump to a chapter, `P` saves a
screenshot.

Keyboard and mouse work everywhere, so the game is playable in the editor without a device.

For iPhone builds and TestFlight, see [docs/IOS.md](docs/IOS.md).
The verified toolchain on `ssh jarvis` is Unity 6000.6.2f1 with iOS Build Support,
Xcode 26.6 and iOS SDK 26.5. The app targets iOS 15 or newer.

## Layout

```
Alone in the Dark/
  Assets/Editor/IosBuild.cs  release iOS export entry point
  Assets/Scripts/            game code, see below
  Assets/Resources/Audio/    sound effects and the ambient loop
  Assets/Resources/Levels/   handcrafted chapters as ASCII maps
  Assets/Resources/Prefabs/  the cube every cave wall is made of
  Assets/Scenes/GameScene.unity  the player, the UI, and chapters 1-3
  Packages/                 tracked manifest and package lockfile
  ProjectSettings/          engine, player, scene and physics settings
Tools/
  build_ios.sh              Unity export followed by signed Xcode archive
  upload_testflight.sh      upload an archive for internal TestFlight testing
  generate_audio.py          regenerates every sound file
  generate_levels.py         regenerates the ASCII chapters and checks they are solvable
.agents/skills/             task-specific instructions for future agents
AGENTS.md                   repository conventions and user preferences
docs/                       iOS runbook and device playtest checklist
```

| Script | Responsibility |
| --- | --- |
| `PlayerController` | The ball, and the game flow: title, intro, chapters, pause, death, win. |
| `GameInput` | All input in one place. Drag-anywhere stick on touch, axes on desktop. |
| `GameSave` | Best scores, chapter progress, whether the intro was seen, sound on or off. |
| `GameAudio` | Loads clips by name from `Resources/Audio` and plays them. |
| `LevelBuilder` | Turns an ASCII map into cave geometry, gates, pickups and wind zones. |
| `EndlessCave` | Streams the procedural cave in ahead of the player and drops it behind. |
| `CameraFit` | Pulls the camera back so the view is equally wide on any aspect ratio. |
| `Story` | Every line of text in the game. |

`PlayerController` owns the flow from title to intro, ready, playing, pause, death and
win. Its scene-linked fields and `Restart()` / `exitGame()` callbacks must remain
resolvable. UI positions are set in `BuildUi`, not just in the scene. Existing save
keys live in `GameSave`; preserve or migrate them when changing progression.

## Adding or editing a chapter

Chapters 4 to 6 are plain text files in `Assets/Resources/Levels`. Any `.txt` added there
becomes a chapter, ordered by file name, and `Story.Chapters` supplies its opening line.

```
@ name The walls came closer
@ gravity 1.2
###########
###..B....#
###.S....F#
###########
```

`#` is rock, `.` is open, `S` is the start, `F` is the exit, `B` is a bonus cup, and
`< > ^ v` are wind blowing that way. Exactly one `S` is required.

Editing the files by hand is fine. `python3 Tools/generate_levels.py` regenerates them from
the waypoint definitions in that script and asserts that the exit and every pickup are
reachable in the map. **It overwrites levels 4–6**, so preserve deliberate hand edits first.
Clearance checks cover the authored route; they do not replace testing physics and wind.

## Regenerating the audio

`python3 Tools/generate_audio.py` writes every `.wav` in `Assets/Resources/Audio` from
synthesised sine waves and noise. Nothing in the audio folder comes from a third party, so
there is no attribution to track. `ambient.wav` is built to loop seamlessly.

## Checks before a change goes out

```bash
bash -n Tools/build_ios.sh Tools/upload_testflight.sh
git -c core.whitespace=-blank-at-eol diff --check
```

For generator changes, run the corresponding Python tool and inspect its asset diff.
Audio regeneration can change the noise-based crash cue even without a source change.
For C# or scene changes, compile with the pinned Unity version and exercise the affected
path in Play Mode. There is no automated Unity gameplay test suite yet.

Phone checks and current gaps are in [docs/PLAYTEST.md](docs/PLAYTEST.md), including
notch/home-indicator layout, touch feel, interruption handling and physics after the
engine upgrade. Successful compilation and TestFlight availability are not a device test.

## Working with an agent

[AGENTS.md](AGENTS.md) is the entry point. Repository skills cover
[gameplay](.agents/skills/caves-gameplay/SKILL.md),
[content](.agents/skills/caves-content/SKILL.md) and
[iOS releases](.agents/skills/caves-ios-release/SKILL.md).
They are committed under `.agents/skills` so future checkouts retain the workflow.
For Apple account browser work, use the user's full Chrome profile with Apple Passwords
integration. Builds, signing credentials and machine caches stay out of Git.
