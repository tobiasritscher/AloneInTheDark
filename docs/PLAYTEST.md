# Getting to a first playtest build

Written for the 0.5.0 beta. Everything in "Done" is in the repo; everything in "Still needed"
is not, and most of it needs the Unity editor or a developer account, so it cannot be done
from a headless checkout.

## Done in this branch

**Playable on a phone at all.** Every keyboard-only interaction was replaced. Steering is a
drag-anywhere virtual stick with a floating origin, so reversing direction is immediate and
the thumb never has to find a fixed pad. Taps advance everything. The Android back button
pauses, and pause is also a button in the corner because a gesture-navigation phone may not
have a back button at all.

**No reading gates.** The story intro plays once, on the very first launch, and can be tapped
away. After that every chapter opens with a single line that fades in over a live world and
disappears by itself. The only instruction the game ever shows is "drag anywhere", once, on
the first run.

**Instant retry.** Death rebuilds the same level in place instead of reloading the scene, so
another attempt starts immediately. Consumed pickups are hidden rather than destroyed and come
back on retry.

**Reasons to come back.** Best scores for each mode, saved chapter progress, an endless mode
that ramps, and a daily cave seeded from the date so it is the same run for everyone that day.

**Sound.** Eight cues plus a seamless ambient drone, all synthesised by
`Tools/generate_audio.py`, so there is no third-party licence in the repo. A sound toggle is on
the title screen and in the pause menu.

**Three more chapters**, generated and verified by `Tools/generate_levels.py`, taking the story
from three chapters to six.

**Bugs fixed along the way.** Leaving a left-wind zone never cleared the wind, because the exit
check tested the right-wind tag twice. The bonus was set to a flat 100 and never reset, so the
secret was worth one pickup per session. The bonus cup prefab had no collider, so it could not
be picked up at all. The light pulse and the intro light ramp moved by a fixed amount per
frame, so they ran at double speed on a 120 Hz phone. Exceeding the speed cap zeroed the whole
input axis, which meant wind or a long fall left you unable to steer back.

**Release settings.** Portrait locked, UI scaled to a 1080x1920 reference with best-fit text,
a camera that keeps a constant view width on any aspect ratio, 64-bit plus IL2CPP for Android,
bundle identifiers for Android and iOS, minimum SDK 24, version 0.5.0, and the debug and cheat
keys compiled out of release builds.

**Repository.** Unity's package manifest is tracked again; the C# gitignore template was
hiding it, so a fresh clone resolved whatever package versions happened to be cached. The
80 MB screen recording is untracked. IDE files are untracked.

## Still needed before a build goes out

These need the editor, a device, or an account.

1. **Engine upgrade.** The project is on Unity 2020.3.27. Both stores need newer toolchains.
   2022.3 LTS is the least painful jump and keeps the built-in render pipeline the particle
   packs expect.
2. **Open the project once and commit what Unity writes.** It will generate
   `Packages/packages-lock.json` and `.meta` files for anything it disagrees with. The
   manifest committed here lists the built-in modules plus uGUI; Unity re-adds editor tooling
   packages on its own.
3. **Check the title screen on a real phone.** The UI positions are set in code against a
   1080x1920 reference. They are reasonable but nobody has seen them on hardware.
4. **Tune the feel.** Steering sensitivity, the brake force, the drift in endless mode and how
   tight the caves are. These are single numbers in `PlayerController` and `EndlessCave`, and
   only a thumb can tell you the right values. Expect this to be the bulk of the work.
5. **A signing keystore**, backed up somewhere that is not this repository. Losing it means
   never updating the Android listing again.
6. **The repository name** still says the old title. The product name, bundle identifiers and
   this documentation say Alone in the Caves, because "Alone in the Dark" is a live trademark
   of another game franchise and would get the listing pulled. Renaming the GitHub repository
   is a one-click operation but it is yours to make.
7. **Store paperwork.** A privacy policy URL (the game collects nothing, which still needs
   saying), screenshots, an age rating questionnaire, and the developer accounts themselves.
8. **History still carries the 80 MB video.** It is untracked going forward, but a fresh clone
   still pays for it. Removing it needs a history rewrite and a force push, so it is worth
   doing once, deliberately, when nobody has work in flight.

## Distributing it

**Android, and do this one first.** Google Play Console, internal testing track. Upload an AAB,
share the opt-in link, up to 100 testers, no review wait. It also validates the signing setup
before anything is public.

**iOS.** TestFlight, internal testers only, which skips App Review. Needs the Apple developer
account, so decide whether round one is Android only.

## What to ask testers

Keep it to three questions. More than that and nobody answers.

1. Did you work out the controls without being told?
2. Where did you die most, and did it feel like your mistake or the game's?
3. Did you open it a second time? If not, why not?

Question three is the one that matters. The game is trying to be worth reopening in a waiting
room, and everything else is negotiable.

Useful context to collect with the answers: phone model, whether they played in one sitting or
several, and which mode they spent the most time in. `GameSave.Runs` counts runs started and is
a decent proxy for engagement if you ever want to surface it.
