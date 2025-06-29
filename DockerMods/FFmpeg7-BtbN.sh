#!/bin/bash
# ----------------------------------------------------------------------------------------------------
# Name: FFmpeg7-BtbN
# Description: This script installs the BtbN Linux GPL static build of FFmpeg 7.x into /opt/ffmpeg-static.
#              It does not remove or modify any existing FFmpeg installation. Users should manually
#              update their environment variables to use the new installation by adding 
#              /opt/ffmpeg-static/bin/ffmpeg and /opt/ffmpeg-static/bin/ffprobe to their PATH.
# Revision: 10
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjwhLS0gR2VuZXJhdG9yOiBBZG9iZSBJbGx1c3RyYXRvciAyNi4wLjEsIFNWRyBFeHBvcnQgUGx1Zy1JbiAuIFNWRyBWZXJzaW9uOiA2LjAwIEJ1aWxkIDApICAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgeD0iMHB4IiB5PSIwcHgiDQoJIHZpZXdCb3g9IjAgMCAyMDAwIDIwMDAiIHN0eWxlPSJlbmFibGUtYmFja2dyb3VuZDpuZXcgMCAwIDIwMDAgMjAwMDsiIHhtbDpzcGFjZT0icHJlc2VydmUiPg0KPHN0eWxlIHR5cGU9InRleHQvY3NzIj4NCgkuc3Qwe2ZpbGw6bm9uZTtzdHJva2U6IzM3OEU0MztzdHJva2Utd2lkdGg6MzAwO3N0cm9rZS1saW5lY2FwOnJvdW5kO3N0cm9rZS1saW5lam9pbjpyb3VuZDtzdHJva2UtbWl0ZXJsaW1pdDo4O30NCjwvc3R5bGU+DQo8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSg1LDUpIj4NCgk8cGF0aCBjbGFzcz0ic3QwIiBkPSJNMTY2LjcsMTY2LjdoNTUyLjJMMTY2LjcsNzE4Ljl2NTUyLjJMMTI3MS4xLDE2Ni43aDU1Mi4yTDE2Ni43LDE4MjMuM2g1NTIuMkwxODIzLjMsNzE4Ljl2NTUyLjJsLTU1Mi4yLDU1Mi4yDQoJCWg1NTIuMiIvPg0KPC9nPg0KPC9zdmc+DQo=
# ----------------------------------------------------------------------------------------------------

# Variables
FFMPEG_VERSION="latest"  # Since we're using the 'latest' build
# URL for the latest BtbN Linux GPL build
FFMPEG_URL="https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz"
FFMPEG_DIR="/opt/ffmpeg-static"
TEMP_DIR="/tmp/ffmpeg-static"

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling ffmpeg and ffprobe from $FFMPEG_DIR..."
    if [ -d "$FFMPEG_DIR" ]; then
        rm -rf "$FFMPEG_DIR"
        echo "$FFMPEG_DIR removed."
    else
        echo "FFmpeg is not installed in $FFMPEG_DIR."
    fi
    echo "Uninstallation complete."
    exit 0
fi

# Step 1: Check if ffmpeg is already installed in /opt/ffmpeg-static
if [ -d "$FFMPEG_DIR" ]; then
    echo "FFmpeg is already installed in $FFMPEG_DIR."
    exit 0
fi

# Step 2: Create the directory for ffmpeg installation in /opt if it doesn't exist
echo "Creating $FFMPEG_DIR for installation..."
mkdir -p "$FFMPEG_DIR"

# Step 3: Create a temporary directory for downloading the static build
echo "Creating temporary directory for download..."
mkdir -p "$TEMP_DIR"

# Step 4: Download the static build of ffmpeg from BtbN GPL builds
echo "Downloading ffmpeg $FFMPEG_VERSION GPL static build..."
wget --no-verbose -O "$TEMP_DIR/ffmpeg-static.tar.xz" "$FFMPEG_URL"

# Step 5: Extract the downloaded archive into /opt/ffmpeg-static
echo "Extracting ffmpeg static build to $FFMPEG_DIR..."
tar -xf "$TEMP_DIR/ffmpeg-static.tar.xz" -C "$FFMPEG_DIR" --strip-components=1

# Step 6: Cleanup temporary files
echo "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

# Step 7: Provide instructions to the user
echo "FFmpeg $FFMPEG_VERSION successfully installed in $FFMPEG_DIR."
echo "Please update your environment variables or PATH to use the new binaries:"
echo "  $FFMPEG_DIR/bin/ffmpeg"
echo "  $FFMPEG_DIR/bin/ffprobe"
exit 0
