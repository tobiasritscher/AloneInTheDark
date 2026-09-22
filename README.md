# Alone in the Caves

A small Unity game about a spark of light trying to get out of a cave system. Steer by
dragging anywhere on the screen. Touch a wall and you go out.

Built for short sessions: no tutorial, no menus to navigate, no loading between attempts.
One tap starts a run and one tap retries it.

![screenshot](https://user-images.githubusercontent.com/97285266/158054642-870e9984-3b92-4ba2-9b5b-95d9d26e92c4.png)

## Modes

| Mode | What it is |
| --- | --- |
| Story | Six chapters, one line of story each. Progress is saved, so Continue picks up where you stopped. |
| Endless | A procedural cave that narrows and pulls harder the further you get. New seed every run. |
| Daily | One seeded cave per calendar day, the same for everybody, with its own best score. |

## Running it

Open the `Alone in the Dark` folder as a Unity project and press play. `GameScene` is the only
scene. In the editor and in development builds a few shortcuts exist that are compiled out of
release builds: `G` toggles gravity, `N` skips a chapter, `1`-`6` jump to a chapter, `P` saves a
screenshot.

Keyboard and mouse work everywhere, so the game is playable in the editor without a device.

## Layout

```
Alone in the Dark/
  Assets/Scripts/            game code, see below
  Assets/Resources/Audio/    sound effects and the ambient loop
  Assets/Resources/Levels/   handcrafted chapters as ASCII maps
  Assets/Resources/Prefabs/  the cube every cave wall is made of
  Assets/Scenes/GameScene    the player, the UI, and chapters 1-3
Tools/
  generate_audio.py          regenerates every sound file
  generate_levels.py         regenerates the ASCII chapters and checks they are solvable
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
actually reachable, which is worth running before shipping a new cave.

## Regenerating the audio

`python3 Tools/generate_audio.py` writes every `.wav` in `Assets/Resources/Audio` from
synthesised sine waves and noise. Nothing in the audio folder comes from a third party, so
there is no attribution to track. `ambient.wav` is built to loop seamlessly.

## Status

Version 0.5.0, aimed at a first round of external playtesters. See `docs/PLAYTEST.md` for
what still has to happen before a build goes out, and what feedback is worth collecting.
