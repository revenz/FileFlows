#!/bin/bash
# ----------------------------------------------------------------------------------------------------
# Name: btbn_ffmpeg7-latest_installer
# Description: This script installs the BtbN Linux GPL static build of FFmpeg into /opt/ffmpeg-static and symlinks it to /usr/local/bin.
# Revision: 1
# Icon: fas fa-file-video
# ----------------------------------------------------------------------------------------------------
# Variables
FFMPEG_VERSION="latest"  # Since we're using the 'latest' build
# URL for the latest BtbN Linux GPL build
FFMPEG_URL="https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz"
INSTALL_DIR="/usr/local/bin"
FFMPEG_DIR="/opt/ffmpeg-static"
TEMP_DIR="/tmp/ffmpeg-static"

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling ffmpeg and ffprobe..."
    sudo rm -f "$INSTALL_DIR/ffmpeg"
    sudo rm -f "$INSTALL_DIR/ffprobe"
    echo "FFmpeg and FFprobe symlinks removed."
    
    if [ -d "$FFMPEG_DIR" ]; then
        echo "Removing $FFMPEG_DIR..."
        sudo rm -rf "$FFMPEG_DIR"
    fi
    echo "Uninstallation complete."
    exit 0
fi

# Check if ffmpeg is already installed
if command -v ffmpeg &>/dev/null; then
    echo "FFmpeg is already installed."
    exit 0
fi

# Step 1: Create the directory for ffmpeg installation in /opt if it doesn't exist
echo "Creating $FFMPEG_DIR for installation..."
sudo mkdir -p "$FFMPEG_DIR"

# Step 2: Create a temporary directory for downloading the static build
echo "Creating temporary directory for download..."
mkdir -p "$TEMP_DIR"

# Step 3: Download the static build of ffmpeg from BtbN GPL builds
echo "Downloading ffmpeg $FFMPEG_VERSION GPL static build..."
wget -O "$TEMP_DIR/ffmpeg-static.tar.xz" "$FFMPEG_URL"

# Step 4: Extract the downloaded archive into /opt/ffmpeg-static
echo "Extracting ffmpeg static build to $FFMPEG_DIR..."
sudo tar -xf "$TEMP_DIR/ffmpeg-static.tar.xz" -C "$FFMPEG_DIR" --strip-components=1

# Step 5: Remove any existing ffmpeg and ffprobe symlinks from /usr/local/bin
echo "Removing existing ffmpeg and ffprobe symlinks from $INSTALL_DIR..."
sudo rm -f "$INSTALL_DIR/ffmpeg"
sudo rm -f "$INSTALL_DIR/ffprobe"

# Step 6: Symlink the new ffmpeg and ffprobe from /opt/ffmpeg-static to /usr/local/bin
echo "Creating symlinks to $INSTALL_DIR..."
sudo ln -s "$FFMPEG_DIR/ffmpeg" "$INSTALL_DIR/ffmpeg"
sudo ln -s "$FFMPEG_DIR/ffprobe" "$INSTALL_DIR/ffprobe"

# Step 7: Cleanup temporary files
echo "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

# Step 8: Verify installation
echo "Verifying ffmpeg installation..."
if command -v ffmpeg &>/dev/null; then
    echo "FFmpeg $FFMPEG_VERSION successfully installed."
    exit 0
else
    echo "Failed to install FFmpeg."
    exit 1
fi
