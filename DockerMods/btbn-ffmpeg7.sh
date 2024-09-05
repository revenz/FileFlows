#!/bin/bash
# ----------------------------------------------------------------------------------------------------
# Name: btbn-latest-ffmpeg7_installer
# Description: This script installs the BtbN Linux GPL static build of FFmpeg 7.x into /opt/ffmpeg-static.
#              It does not remove or modify any existing FFmpeg installation. Users should manually
#              update their environment variables to use the new installation by adding 
#              /opt/ffmpeg-static/bin/ffmpeg and /opt/ffmpeg-static/bin/ffprobe to their PATH.
# Revision: 5
# Icon: fas fa-file-video
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
        sudo rm -rf "$FFMPEG_DIR"
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
sudo mkdir -p "$FFMPEG_DIR"

# Step 3: Create a temporary directory for downloading the static build
echo "Creating temporary directory for download..."
mkdir -p "$TEMP_DIR"

# Step 4: Download the static build of ffmpeg from BtbN GPL builds
echo "Downloading ffmpeg $FFMPEG_VERSION GPL static build..."
wget -O "$TEMP_DIR/ffmpeg-static.tar.xz" "$FFMPEG_URL"

# Step 5: Extract the downloaded archive into /opt/ffmpeg-static
echo "Extracting ffmpeg static build to $FFMPEG_DIR..."
sudo tar -xf "$TEMP_DIR/ffmpeg-static.tar.xz" -C "$FFMPEG_DIR" --strip-components=1

# Step 6: Cleanup temporary files
echo "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

# Step 7: Provide instructions to the user
echo "FFmpeg $FFMPEG_VERSION successfully installed in $FFMPEG_DIR."
echo "Please update your environment variables or PATH to use the new binaries:"
echo "  /opt/ffmpeg-static/bin/ffmpeg"
echo "  /opt/ffmpeg-static/bin/ffprobe"
exit 0
