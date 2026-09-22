#!/bin/bash
# Upload an existing signed archive for internal TestFlight testing.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
: "${APPLE_TEAM_ID:?Set APPLE_TEAM_ID to your Apple Developer team ID}"
export APPLE_TEAM_ID
ARCHIVE_PATH="${ARCHIVE_PATH:-$ROOT/Builds/AloneInTheCaves.xcarchive}"
test -d "$ARCHIVE_PATH" || { echo "Archive not found: $ARCHIVE_PATH" >&2; exit 1; }
mkdir -p "$ROOT/Builds"

python3 - "$ROOT/Builds/ExportOptions.plist" <<'PY'
import os
import plistlib
import sys

with open(sys.argv[1], "wb") as output:
    plistlib.dump({
        "method": "app-store-connect",
        "destination": "upload",
        "teamID": os.environ["APPLE_TEAM_ID"],
        "signingStyle": "automatic",
        "manageAppVersionAndBuildNumber": False,
        "uploadSymbols": True,
        "testFlightInternalTestingOnly": True,
    }, output)
PY

xcodebuild -exportArchive -archivePath "$ARCHIVE_PATH" \
    -exportOptionsPlist "$ROOT/Builds/ExportOptions.plist" \
    -exportPath "$ROOT/Builds/TestFlight" -allowProvisioningUpdates \
    > "$ROOT/Builds/testflight-upload.log" 2>&1
echo "Upload succeeded. Check TestFlight for Apple's processing status."
