---
name: caves-ios-release
description: Build, sign, diagnose or distribute Alone in the Caves for iPhone using Unity and Xcode on SSH host jarvis. Use for iOS archives and authorized internal TestFlight releases.
---

# Build and distribute on jarvis

Read [docs/IOS.md](../../../docs/IOS.md) for paths, identity, scripts and the dated
release record. Read [AGENTS.md](../../../AGENTS.md) for the user's Chrome preference.
Never copy passwords, signing keys or session cookies into the repository.

## Prepare only what changed

Check local Git status and the remote source before syncing. `jarvis`'s copy is at
`/Users/jarvis/Builds/AloneInTheCaves`; sync `Assets`, `Packages`, `ProjectSettings`
and the build tools with paths quoted. Exclude `Library`, `Temp`, `Logs`,
`UserSettings`, `.git`, archives and builds. Do not use `--delete` against the whole
remote checkout. If somebody edited the remote copy, reconcile those edits first.

Use the project's pinned editor and its matching iOS Build Support module. Unity
Hub alone is insufficient. Verify licence/module availability when startup fails.
Set `DEVELOPER_DIR` to Xcode.app; the machine's global `xcode-select` pointed at
CommandLineTools during the first release. Avoid changing it globally.

Check App Store Connect for the latest uploaded version/build before selecting an
unused `IOS_BUILD_NUMBER`. Build 1 is already uploaded; do not reuse it or re-upload
after an ambiguous timeout until checking Apple's actual state.

## Export, archive, upload

Use `Tools/build_ios.sh` with `UNITY_EDITOR`, `APPLE_TEAM_ID` and `IOS_BUILD_NUMBER`.
`IosBuild.Export` creates a non-development IL2CPP device export, iOS 15 minimum,
portrait, automatic signing and the current offline game's encryption declaration.
Reassess that declaration if networking/crypto is introduced.

On this Mac, keychain unlocking must happen in the **same SSH session** as signing
or export. Use an interactive `security unlock-keychain` prompt, then run the command
in that shell. A separate successful unlock did not fix `errSecInternalComponent`.
If failure persists, inspect the exact signing error before rebuilding Unity or
changing certificate access. Do not revoke certificates or weaken all key ACLs.

After `ARCHIVE SUCCEEDED`, verify the archive with `codesign --verify --deep --strict`.
For a requested TestFlight release, run `Tools/upload_testflight.sh` with the team ID
and unlocked keychain. It uploads for internal testing only. An existing valid archive
can be retried without another Unity export; code/content changes require rebuilding.

## Verify distribution

Use full **Google Chrome with the user's existing Apple Passwords profile** for
App Store Connect. Match the tab's URL/identity; do not use Safari or an isolated
Chromium/testing profile. Let the user complete Touch ID/2FA when necessary.

The app record and explicit bundle ID already exist. Use the record linked in
`docs/IOS.md` rather than creating duplicates. The `Internal` group distributes
automatically and contains the owner's account. Do not add unrelated testers.

Require both `EXPORT SUCCEEDED` and Apple processing completion. `Processing` is
not installable; `Testing` with the intended group is the verified first-release
state. Check for validation errors if processing fails. Stop retries for rejected
credentials/access or an unchanged validation error until the cause is resolved.

The first upload warned about missing `UnityRuntime.framework` dSYM; it still
reached Testing. Report the crash-diagnostics limitation separately from upload
success. Update the dated release record and give the user the exact version/build
and iPhone installation step. Never claim installation without device evidence.
