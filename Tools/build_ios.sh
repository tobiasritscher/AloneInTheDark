#!/bin/bash
# Export and archive on a Mac with Unity iOS Build Support and Xcode installed.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the Unity.app/Contents/MacOS/Unity executable}"
: "${IOS_BUILD_NUMBER:?Set IOS_BUILD_NUMBER to a positive, unused TestFlight build number}"
: "${APPLE_TEAM_ID:?Set APPLE_TEAM_ID to your Apple Developer team ID}"
export IOS_BUILD_NUMBER APPLE_TEAM_ID
export IOS_BUILD_PATH="${IOS_BUILD_PATH:-$ROOT/Builds/iOS}"
ARCHIVE_PATH="${ARCHIVE_PATH:-$ROOT/Builds/AloneInTheCaves.xcarchive}"
mkdir -p "$ROOT/Builds"

"$UNITY_EDITOR" -batchmode -nographics -quit -accept-apiupdate \
    -projectPath "$ROOT/Alone in the Dark" -buildTarget iOS \
    -executeMethod IosBuild.Export -logFile "$ROOT/Builds/unity-ios.log"

xcodebuild -project "$IOS_BUILD_PATH/Unity-iPhone.xcodeproj" \
    -scheme Unity-iPhone -configuration Release -destination 'generic/platform=iOS' \
    -archivePath "$ARCHIVE_PATH" -allowProvisioningUpdates \
    DEVELOPMENT_TEAM="$APPLE_TEAM_ID" CODE_SIGN_STYLE=Automatic archive \
    > "$ROOT/Builds/xcode-archive.log" 2>&1

echo "Archive ready: $ARCHIVE_PATH"
echo "Open it in Xcode Organizer to validate and upload to App Store Connect."
