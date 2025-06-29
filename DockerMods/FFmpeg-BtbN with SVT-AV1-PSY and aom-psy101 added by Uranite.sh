# ----------------------------------------------------------------------------------------------------
# Name: FFmpeg-BtbN with SVT-AV1-PSY and aom-psy101 added by Uranite
# Description: This script installs the BtbN Linux GPL static build of FFmpeg (latest) with SVT-AV1-PSY and aom-psy101 into /opt/ffmpeg-uranite-static. It does not remove or modify any existing FFmpeg installation. Users should manually update their environment variables to use the new installation by adding /opt/ffmpeg-uranite-static/bin/ffmpeg and /opt/ffmpeg-uranite-static/bin/ffprobe to their PATH.
# Revision: 13
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjwhLS0gR2VuZXJhdG9yOiBBZG9iZSBJbGx1c3RyYXRvciAyNi4wLjEsIFNWRyBFeHBvcnQgUGx1Zy1JbiAuIFNWRyBWZXJzaW9uOiA2LjAwIEJ1aWxkIDApICAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgeD0iMHB4IiB5PSIwcHgiDQoJIHZpZXdCb3g9IjAgMCAyMDAwIDIwMDAiIHN0eWxlPSJlbmFibGUtYmFja2dyb3VuZDpuZXcgMCAwIDIwMDAgMjAwMDsiIHhtbDpzcGFjZT0icHJlc2VydmUiPg0KPHN0eWxlIHR5cGU9InRleHQvY3NzIj4NCgkuc3Qwe2ZpbGw6bm9uZTtzdHJva2U6IzM3OEU0MztzdHJva2Utd2lkdGg6MzAwO3N0cm9rZS1saW5lY2FwOnJvdW5kO3N0cm9rZS1saW5lam9pbjpyb3VuZDtzdHJva2UtbWl0ZXJsaW1pdDo4O30NCjwvc3R5bGU+DQo8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSg1LDUpIj4NCgk8cGF0aCBjbGFzcz0ic3QwIiBkPSJNMTY2LjcsMTY2LjdoNTUyLjJMMTY2LjcsNzE4Ljl2NTUyLjJMMTI3MS4xLDE2Ni43aDU1Mi4yTDE2Ni43LDE4MjMuM2g1NTIuMkwxODIzLjMsNzE4Ljl2NTUyLjJsLTU1Mi4yLDU1Mi4yDQoJCWg1NTIuMiIvPg0KPC9nPg0KPC9zdmc+DQo=
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Handle errors
trap 'echo "Error occurred. Exiting..."; exit 1' ERR

INSTALL_DIR="/opt/ffmpeg-uranite-static"
VERSION_FILE="$INSTALL_DIR/version.txt"

# Uninstall handler
if [ "$1" == "--uninstall" ]; then
    echo "Removing uranite FFmpeg installation..."
    rm -rf "$INSTALL_DIR"
    echo "FFmpeg (uranite) successfully uninstalled"
    exit 0
fi

# Check current version
CURRENT_VERSION=""
if [ -f "$VERSION_FILE" ]; then
    CURRENT_VERSION=$(cat "$VERSION_FILE")
fi

# Get latest release info from GitHub API
echo "Checking for updates..."
API_RESPONSE=$(curl -s https://api.github.com/repos/uranite/FFmpeg-Builds/releases/latest)
LATEST_NAME=$(echo "$API_RESPONSE" | grep '"name":' | head -1 | cut -d'"' -f4)
LATEST_TAG=$(echo "$API_RESPONSE" | grep '"tag_name":' | cut -d'"' -f4)

# Version comparison using both name and tag
LATEST_VERSION="${LATEST_NAME} (${LATEST_TAG})"

if [ "$CURRENT_VERSION" == "$LATEST_VERSION" ] && [ -f "$BIN_DIR/ffmpeg" ]; then
    echo "Already up to date: $LATEST_VERSION"
    exit 0
fi

echo "New version available: $LATEST_VERSION"
echo "Current installation: ${CURRENT_VERSION:-Not installed}"

# Determine architecture
ARCH=$(uname -m)
case $ARCH in
    x86_64) PKG_ARCH="linux64" ;;
    aarch64) PKG_ARCH="linuxarm64" ;;
    *) echo "Unsupported architecture: $ARCH"; exit 1 ;;
esac

# Package configuration
PKG_NAME="ffmpeg-master-latest-${PKG_ARCH}-gpl.tar.xz"
DOWNLOAD_URL="https://github.com/uranite/FFmpeg-Builds/releases/download/latest/$PKG_NAME"
TMP_DIR=$(mktemp -d)

# Cleanup handler
trap 'rm -rf "$TMP_DIR"' EXIT

echo "Downloading $PKG_NAME..."
wget --no-verbose -O "$TMP_DIR/ffmpeg.tar.xz" "$DOWNLOAD_URL"

echo "Extracting ffmpeg static build to to $INSTALL_DIR..."
rm -rf "$INSTALL_DIR" 2>/dev/null
mkdir -p "$INSTALL_DIR"
tar -xf "$TMP_DIR/ffmpeg.tar.xz" -C "$INSTALL_DIR" --strip-components=1

# Store combined version info
echo "$LATEST_VERSION" > "$VERSION_FILE"

# Cleanup temporary files
echo "Cleaning up temporary files..."
rm -rf "$TMP_DIR"

# Verify installation
if [ -f "$INSTALL_DIR/bin/ffmpeg" ] && "$INSTALL_DIR/bin/ffmpeg" -version &>/dev/null; then
    echo "Successfully installed FFmpeg (uranite)"
    echo "Version: $LATEST_VERSION"
    echo "Please update your environment variables or PATH to use the new binaries:"
	echo "  $INSTALL_DIR/bin/ffmpeg"
	echo "  $INSTALL_DIR/bin/ffprobe"
    exit 0
else
    echo "Installation failed"
    exit 1
fi