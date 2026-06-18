#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-$PROJECT_DIR/Builds/WebGL}"
CHANNEL="${ITCH_CHANNEL:-html5}"
VERSION="${ITCH_VERSION:-$(awk '/bundleVersion:/ { print $2; exit }' "$PROJECT_DIR/ProjectSettings/ProjectSettings.asset")}"

if [[ -z "${ITCH_USER:-}" || -z "${ITCH_GAME:-}" ]]; then
    echo "Set ITCH_USER and ITCH_GAME to your lowercase itch.io username and project slug." >&2
    echo "Example: ITCH_USER=my-name ITCH_GAME=splat-fighters $0" >&2
    exit 1
fi

if [[ ! "$ITCH_USER" =~ ^[a-z0-9-]+$ || ! "$ITCH_GAME" =~ ^[a-z0-9-]+$ ]]; then
    echo "ITCH_USER and ITCH_GAME must use lowercase letters, numbers, or hyphens." >&2
    exit 1
fi

if [[ ! -f "$BUILD_DIR/index.html" ]]; then
    echo "Missing $BUILD_DIR/index.html. Build the WebGL release first." >&2
    exit 1
fi

if ! command -v butler >/dev/null 2>&1; then
    echo "butler is not installed or is not on PATH." >&2
    echo "Install it from https://itch.io/docs/butler/installing.html" >&2
    exit 1
fi

butler push "$BUILD_DIR" "$ITCH_USER/$ITCH_GAME:$CHANNEL" --userversion "$VERSION"
