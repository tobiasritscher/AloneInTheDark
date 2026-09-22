# Working on Alone in the Caves

## Start here

- The repository is `AloneInTheDark`; the game is **Alone in the Caves**.
- Unity project: `Alone in the Dark/` (quote paths). Engine: **6000.6.2f1**,
  built-in render pipeline, legacy Input Manager, uGUI. Read `ProjectVersion.txt`
  and `Packages/manifest.json` before changing engine or package assumptions.
- Read [README.md](README.md) for the code map, [docs/IOS.md](docs/IOS.md) for
  build/signing/TestFlight, and [docs/PLAYTEST.md](docs/PLAYTEST.md) for device checks.
- Start with `git status --short` and the current branch. Preserve unrelated work.
  Commit/push on the current branch when requested; do not infer a merge into `main`.

## Project skills

Use the relevant repository skill; read only what the current task needs:

| Task | Skill |
| --- | --- |
| Controls, physics, UI, saves, game flow | [.agents/skills/caves-gameplay/SKILL.md](.agents/skills/caves-gameplay/SKILL.md) |
| Chapters, procedural caves, story, sound assets | [.agents/skills/caves-content/SKILL.md](.agents/skills/caves-content/SKILL.md) |
| Unity export, signing, TestFlight on jarvis | [.agents/skills/caves-ios-release/SKILL.md](.agents/skills/caves-ios-release/SKILL.md) |

## Browser preference

Use the user's **full Google Chrome installation and existing profile with Apple
Passwords integration** for browser work, including Apple Developer / App Store Connect.
Do not substitute Safari, Chromium, Chrome for Testing, a temporary automation profile,
or a headless browser for authenticated account work. Preserve passkeys and Touch ID.
If available automation cannot attach to that Chrome profile, use supported native UI
control or explain the specific missing setup. Do not silently switch browsers.
Target the task's tab by URL/identity, not whichever tab happens to be frontmost.

## Invariants worth preserving

- `Assets/Scenes/GameScene.unity` is the only build scene. Chapters 1–3 are scene
  objects; chapters 4–6 are resource text assets sorted by filename.
- `PlayerController` has serialized scene references. Preserve field names/GUIDs and
  the scene's `Restart()` / `exitGame()` callbacks unless migrating their references too.
- Commit `.meta` files with their assets. Moving assets must preserve their GUIDs.
  Keep Unity's manifest and lockfile tracked; do not commit caches or build outputs.
- Portrait, drag-anywhere steering, one-tap retry, short skippable story lines.
  Input belongs in `GameInput`, persistence in `GameSave`, audio in `GameAudio`.
- Save keys already ship to testers. Migrate existing data deliberately when changing them.
- Use frame/time-based motion and fades. Keep debug/cheat keys restricted to editor or
  development builds. Keep the particle assets on the built-in render pipeline.

## Build and verification

- Build Mac: `ssh jarvis`; checkout: `/Users/jarvis/Builds/AloneInTheCaves`.
  This is a synced copy, not the authoritative Git checkout. Sync source before building;
  never copy its `Library`, `Temp`, logs or builds into the repository.
- `Tools/build_ios.sh` exports Unity and archives Xcode;
  `Tools/upload_testflight.sh` uploads an existing archive for internal testing only.
  Follow [docs/IOS.md](docs/IOS.md), including an unused build number and unlocking
  the keychain **in the same SSH session**. Do not persist passwords in scripts or docs.
- An export, signed archive, successful upload, TestFlight `Testing` status, and
  on-device playtest are separate results. Report only the stages actually verified.
  Upload only when the user requested distribution; an ordinary gameplay edit is not
  authorization to release a new build.
- There is no automated Unity gameplay test suite yet. Check changed behaviours in
  Play Mode/on a device, compile game changes, and run relevant content checks.
  Documentation-only changes need link/skill/syntax checks, not another store upload.
- Unity-generated YAML uses trailing spaces. For diff hygiene use
  `git -c core.whitespace=-blank-at-eol diff --check`; avoid reformatting all assets.

## Communication

Be concise and direct. In German use umlauts and `ss`, never `ß`.
Record durable discoveries in the relevant document/skill. Keep historical build
results dated; do not mistake them for the current state of Apple services.
