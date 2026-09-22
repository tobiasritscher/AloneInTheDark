# Phone playtest

The first internal iOS build, **0.5.0 (1)**, reached TestFlight **Testing** on
2026-09-22. See [IOS.md](IOS.md) for the release record and repeatable commands.
There is no recorded on-device gameplay result yet.

## Run through these paths

| Area | Check |
| --- | --- |
| First launch | Intro appears once, a tap skips it, the first chapter starts without a reading gate. |
| Controls | Drag anywhere; reverse direction; release/reacquire the finger; try a second finger. Check that UI taps do not accidentally steer/start play. |
| Death/retry | Wall contact kills; retry is immediate after the short lockout; pickups reappear; wind from the previous run is cleared. |
| Story | Play all six chapters, including the scene-to-generated transition from 3 to 4. Exits and bonus cups are reachable. |
| Continue | Close/relaunch after progressing; Continue opens the saved chapter. Check that a new story starts cleanly. |
| Endless | New seed on retry; increasing drift/gravity feels fair; no missing cave ahead or unbounded geometry behind. |
| Daily | Same local date gives the same seed on retry/relaunch; best score remains separate from endless. Date changes start a new daily record. |
| Pause | Pause/resume/menu/retry restore the correct time scale and clear held input. |
| Interruptions | Background/foreground, phone lock/unlock and audio interruptions leave the game usable. Do not assume automatic pause exists. |
| UI | On a notched phone, score, pause and sound controls remain visible and tappable; text fits; home indicator does not cover controls. |
| Audio | Seven cues and ambient loop play; loop seam is unobtrusive; mute survives relaunch. |
| Longer session | Check frame pacing, heat and battery impact through several retries and a longer endless run. |

## Known gaps to investigate

- `BuildUi` does not apply `Screen.safeArea`. Notch/home-indicator overlap needs device evidence.
- Steering sensitivity, braking and narrow passages have not been tuned on an iPhone.
- Unity 6 reported old physics settings below its supported serialization version.
  Re-save/inspect in the editor and verify collisions/forces before declaring migration complete.
- The first upload lacked `UnityRuntime.framework` dSYM. Distribution succeeded, but
  native Unity crashes may have limited symbolication.
- Daily identity uses the device's local date. It is not one globally synchronized UTC cave.

## Feedback to record

Ask three things: Were the controls obvious? Where did you die, and did it feel fair?
Did you want to open it again?

Record build/version, iPhone model, iOS version, mode/chapter and reproduction steps.
Attach a short recording or screenshot when useful; keep large captures out of Git.
Mark each result as observed or untested. The Python map checks establish connectivity
and authored-route clearance, not real-device playability.

## Later distribution

The current upload is internal TestFlight only. External testers, App Store release or
Android distribution require a separate release task and their own signing/metadata checks.
Do not treat historic Android-first plans as a prerequisite for testing this iPhone build.
