# iPhone / TestFlight build

The project now uses Unity **6000.6.2f1** with the built-in render pipeline.
Install its **iOS Build Support** module and Xcode 26 or newer on the build Mac.

On `jarvis`, the working copy is `/Users/jarvis/Builds/AloneInTheCaves`.

Sync source from the repository root before building. Check for remote edits first;
the remote folder is a working copy, not a second source of truth.

```bash
rsync -az --exclude Library --exclude Temp --exclude Logs --exclude UserSettings \
  --exclude Builds --exclude obj \
  'Alone in the Dark' Tools docs jarvis:Builds/AloneInTheCaves/
ssh -t jarvis
```

Then, in that SSH shell:

```bash
cd /Users/jarvis/Builds/AloneInTheCaves
export UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity
export APPLE_TEAM_ID=C33HMQ72W7
export IOS_BUILD_NUMBER=2 # Example: check App Store Connect for the next unused number.
security unlock-keychain ~/Library/Keychains/login.keychain-db
./Tools/build_ios.sh
```

The script exports a release IL2CPP Xcode project, then archives it with automatic signing.
It selects Xcode through `DEVELOPER_DIR`, without changing the Mac's global selection.
Logs are in `Builds/unity-ios.log` and `Builds/xcode-archive.log`.
Generated projects, archives and logs are ignored by Git.

App identity:

- Name: **Alone in the Caves**
- Bundle ID: **com.wipsalone.aloneinthecaves**
- Version: **0.5.0**
- Minimum iOS: **15.0**
- Suggested App Store Connect SKU: **alone-in-the-caves**

Build **1** is already uploaded. Verify the current highest build before the next
release; the scripts deliberately do not let Xcode auto-change the build number.

## Signing and upload

Xcode must be signed into the Apple Developer team. The signing certificate's private key
must be accessible in an unlocked keychain. An SSH build can compile successfully and then
fail with `errSecInternalComponent` if the keychain is locked or denies access to `codesign`.
For SSH builds, run `security unlock-keychain ~/Library/Keychains/login.keychain-db`
interactively in the **same SSH session** as the build/export command. Unlocking in a separate
SSH session did not resolve signing on `jarvis`. Never put keychain passwords in this repository.

The explicit bundle identifier and [App Store Connect record](https://appstoreconnect.apple.com/apps/6814689953/testflight)
are registered under Tobias Ritscher's team. Apple app ID: **6814689953**.
For command-line uploads, run `APPLE_TEAM_ID=C33HMQ72W7 ./Tools/upload_testflight.sh`
after unlocking the keychain in the same session. This uploads for internal testing only.
Alternatively, open `Builds/AloneInTheCaves.xcarchive` in Xcode Organizer and
choose **Distribute App → App Store Connect**. Let Apple process the upload, then add the build
to an internal TestFlight group containing your App Store Connect user. Install TestFlight
on the iPhone and open the available build there.

For the Apple Developer portal and App Store Connect, use the user's **full Google Chrome
with the existing Apple Passwords profile**. Keep passkey/Touch ID support available;
do not use Safari or a temporary Chromium/Chrome for Testing profile. Select the task's
tab by its URL/identity so switching tabs during automation cannot redirect actions.

The upload log is `Builds/testflight-upload.log`. `EXPORT SUCCEEDED` means Apple accepted
the upload; wait for processing to complete and verify **Testing** plus the intended group
in App Store Connect. A timeout is not proof of failure: check the existing upload before
retrying. Internal builds cannot be used for external testing or App Store release.

The export declares no non-exempt encryption; the game implements no encryption.
Unity supplies privacy manifests, including the `PlayerPrefs` / UserDefaults declaration.

## Verification on 2026-09-22

- Unity 6000.6.2f1 imported the project, compiled the scripts and exported iOS build 1.
- Xcode 26.6 compiled and linked the arm64 app using the iOS 26.5 SDK.
- Signed archive succeeded after unlocking the keychain in the same SSH session.
  `codesign --verify --deep --strict` passed for the archived app.
- Internal TestFlight upload of **0.5.0 (1)** succeeded (`EXPORT SUCCEEDED`).
  Apple completed processing and the build status is **Testing**. The **Internal** group
  has automatic distribution enabled and includes **tobias@ritscher.ch**; TestFlight shows
  one invitation. Open the invitation on the iPhone and install through TestFlight.
- Upload warning: UnityRuntime.framework's dSYM is absent from the archive. This did not
  block upload but limits symbolication of crashes inside Unity's runtime.
- Generated chapters 4–6 passed their reachability and clearance checks.
- No on-device playtest yet. Check notch/home-indicator overlap, touch steering, pause/resume,
  sound, all six chapters, endless/daily modes and save persistence after relaunch.
- Unity reports that the old physics settings need re-saving in the editor; verify physics
  behaviour during the first playtest. A successful compile does not establish gameplay feel.

Apple's current SDK requirements: <https://developer.apple.com/news/upcoming-requirements/>.
