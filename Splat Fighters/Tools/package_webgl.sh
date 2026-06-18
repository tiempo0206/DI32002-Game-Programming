#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="${1:-$PROJECT_DIR/Builds/WebGL}"
ARCHIVE_PATH="${2:-$PROJECT_DIR/Builds/Splat-Fighters-WebGL.zip}"
MAX_FILES=1000
MAX_TOTAL_BYTES=$((500 * 1024 * 1024))
MAX_FILE_BYTES=$((200 * 1024 * 1024))

if [[ ! -f "$BUILD_DIR/index.html" ]]; then
    echo "Missing $BUILD_DIR/index.html. Build the WebGL release first." >&2
    exit 1
fi

file_count=0
total_bytes=0
largest_file_bytes=0
largest_file=""

while IFS= read -r -d '' file; do
    if stat -f '%z' "$file" >/dev/null 2>&1; then
        file_bytes="$(stat -f '%z' "$file")"
    else
        file_bytes="$(stat -c '%s' "$file")"
    fi

    file_count=$((file_count + 1))
    total_bytes=$((total_bytes + file_bytes))
    if (( file_bytes > largest_file_bytes )); then
        largest_file_bytes="$file_bytes"
        largest_file="$file"
    fi
done < <(find "$BUILD_DIR" -type f -print0)

if (( file_count > MAX_FILES )); then
    echo "Build has $file_count files; itch.io HTML5 uploads allow at most $MAX_FILES." >&2
    exit 1
fi

if (( total_bytes > MAX_TOTAL_BYTES )); then
    echo "Build is $total_bytes bytes; itch.io HTML5 uploads allow at most $MAX_TOTAL_BYTES extracted bytes." >&2
    exit 1
fi

if (( largest_file_bytes > MAX_FILE_BYTES )); then
    echo "File exceeds itch.io's 200 MB limit: $largest_file ($largest_file_bytes bytes)." >&2
    exit 1
fi

mkdir -p "$(dirname "$ARCHIVE_PATH")"
rm -f "$ARCHIVE_PATH"
(
    cd "$BUILD_DIR"
    zip -qry "$ARCHIVE_PATH" .
)

archive_entries="$(unzip -Z1 "$ARCHIVE_PATH")"
if ! grep -qx 'index.html' <<< "$archive_entries"; then
    echo "Archive validation failed: index.html is not at the ZIP root." >&2
    exit 1
fi

echo "itch.io checks passed: $file_count files, $total_bytes extracted bytes."
echo "Largest file: $largest_file_bytes bytes ($largest_file)"
echo "Upload archive ready: $ARCHIVE_PATH"
