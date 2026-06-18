#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY_VERSION="$(awk '/m_EditorVersion:/ { print $2; exit }' "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt")"
UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"
UNITY_ROOT="$(cd "$(dirname "$UNITY_PATH")/../../.." && pwd)"
WEBGL_SUPPORT_DIR="$UNITY_ROOT/PlaybackEngines/WebGLSupport"
BUILD_DIR="${1:-$PROJECT_DIR/Builds/WebGL}"
LOG_PATH="$PROJECT_DIR/Logs/webgl-build.log"

if [[ ! -x "$UNITY_PATH" ]]; then
    echo "Unity Editor was not found at: $UNITY_PATH" >&2
    echo "Set UNITY_PATH to the Unity executable and try again." >&2
    exit 1
fi

if [[ ! -d "$WEBGL_SUPPORT_DIR" && ! -d "$UNITY_ROOT/Unity.app/Contents/PlaybackEngines/WebGLSupport" ]]; then
    echo "Unity WebGL Build Support is not installed for $UNITY_VERSION." >&2
    echo "Open Unity Hub > Installs > $UNITY_VERSION > Add modules, then install WebGL Build Support." >&2
    exit 1
fi

mkdir -p "$(dirname "$LOG_PATH")"

"$UNITY_PATH" \
    -batchmode \
    -quit \
    -projectPath "$PROJECT_DIR" \
    -executeMethod SplatFightersWebGLBuild.BuildForItch \
    -buildPath "$BUILD_DIR" \
    -logFile "$LOG_PATH"

if [[ ! -f "$BUILD_DIR/index.html" ]]; then
    echo "Build finished without $BUILD_DIR/index.html" >&2
    exit 1
fi

echo "WebGL build ready: $BUILD_DIR"
echo "Build log: $LOG_PATH"
