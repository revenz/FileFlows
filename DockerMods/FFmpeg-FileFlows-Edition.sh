# ----------------------------------------------------------------------------------------------------
# Name: FFmpeg FileFlows Edition
# Description: Default FileFlows DockerMod for installing FFmpeg.
#              Installs both the Jellyfin FFmpeg build and the latest BtbN static build.
#              Both versions are included to support different processing requirements within FileFlows.
# Revision: 2
# Icon: data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAPAAAADwCAMAAAAJixmgAAAAAXNSR0IB2cksfwAAAAlwSFlzAAALEwAACxMBAJqcGAAAAtZQTFRFAAAAM5kzN4w6N448N407OI07N407OI48OI08OI47OI48OI47OY88M5kzKn8qNo46OI47N447N448SJFIOI48OI08OI48OI07Oo86N408N447OI04No07N448No06OI47N407OZA5OI48N447QIs8N448OYw5N407QYdAySZ5pEBoOI07P38/0iB9/wCQ4BaDoD5oN4w8N408RIVBuTFzLotF2huA8gmLmURrOI07sTdvOI48SoFEzCR74BaD5BOFOI47OI08UH1G9wWNOI48OI485xGG/QGPN4488gmKVnhJ4BaDN408OI477Q2IxCp3N406rDtsOI47VapVXXNM6BCGqlRzOI078gmKsjdvOI04OI08OI48AP8AZG5P1R5+gH9IN5A8OI47PJE89geM5xGGokVoOI47OI07N48/6RCGokNqOI09bmhTP5Q/N447N408NI48+gSO6g+HoUJpOI49N408OI07N407OI48O5A7d2FX/AKPgVpb/gGQjFJgmUllm0dmnkVnqzxstjRxO4w9wC11PYo+ySZ5OI08OY89N408oERquTFz0x996w6H+wOO7wuJ2Bx/vy51pUBrN448yyR61R5+pj9rsThu9gaMuzBzgD9IySZ5vC90wyp3rjlu7A6ItzRxoUNmrjlutjRxuzBzvC90N448P38/OY87o0NoAH8AN447OYs5N448OYs5N4w8PIc8N448N448uzBzOI48N407NpE/tDVwOI0+N448N448OI48No47OI04nkNp2RqA7A2InUVltDZw0x99oEJm/QKP7Q2I3heCxyh4rTltm0tipUJp9AiLqTxrOI04OI47n0Rm5RKF9QeMo0BpN408pUdsuDNy8AqKNo49rDtuxSl3+gOON448+AWN3hiCOI46tzNxuDJy8wiLpT9qujFzpEBo3xeCmkxi7gyJyCd5oUFprjpuxih4zyJ8tTRxokFnaoC2jgAAAPJ0Uk5TAApOnM3m/P/+69CaUAUGRpbJ5Qdx6G1sJ9jOJDjbQfDvNXb7N+4ogP/6StEI///3EHJ3/7YL//4WWpSo/+H/+ef1///5/f//7f7/9p2V/9NKf94D//sG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
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

set -e

# ---------------------- Configurable Paths ----------------------
BTBN_DIR="$common/ffmpeg-static"
TEMP_DIR="/tmp/ffmpeg-static"

# Constants
JELLYFIN_LINK="/usr/local/bin/ffmpeg"
JELLYFIN_DIR="/usr/lib/jellyfin-ffmpeg"
BTBN_URL="https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz"

# ---------------------- Utility ------------------------
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# ---------------------- Jellyfin FFmpeg ------------------------
function is_jellyfin_installed {
    [ -x "$JELLYFIN_LINK" ] && "$JELLYFIN_LINK" -version 2>/dev/null | grep -q jellyfin
}

function install_jellyfin_ffmpeg {
    if is_jellyfin_installed; then
        echo "Jellyfin FFmpeg is already installed."
        return
    fi

    echo "Installing Jellyfin FFmpeg..."

    architecture=$(uname -m)
    if [[ ! "$architecture" =~ ^(x86_64|aarch64|armv7l)$ ]]; then
        echo "Unsupported architecture: $architecture"
        exit 1
    fi

    curl -m 15 -fsSL https://repo.jellyfin.org/debian/jellyfin_team.gpg.key | gpg --dearmor --batch --yes -o /etc/apt/trusted.gpg.d/debian-jellyfin.gpg
    os_id=$(awk -F'=' '/^ID=/{ print $NF }' /etc/os-release)
    os_codename=$(awk -F'=' '/^VERSION_CODENAME=/{ print $NF }' /etc/os-release)
    echo "deb [arch=$(dpkg --print-architecture)] https://repo.jellyfin.org/$os_id $os_codename main" > /etc/apt/sources.list.d/jellyfin.list

    apt-get -qq update
    apt-get install --no-install-recommends --no-install-suggests -yqq jellyfin-ffmpeg7

    ln -sf "$JELLYFIN_DIR/ffmpeg" /usr/local/bin/ffmpeg
    ln -sf "$JELLYFIN_DIR/ffprobe" /usr/local/bin/ffprobe

    echo "Jellyfin FFmpeg installed."
}

function uninstall_jellyfin_ffmpeg {
    echo "Uninstalling Jellyfin FFmpeg..."
    rm -f /usr/local/bin/ffmpeg /usr/local/bin/ffprobe
    apt-get remove --purge -y jellyfin-ffmpeg7 || echo "Not installed."
    rm -f /etc/apt/sources.list.d/jellyfin.list
    rm -f /etc/apt/trusted.gpg.d/debian-jellyfin.gpg /etc/apt/trusted.gpg.d/debian-jellyfin.gpg~
    apt-get update
}

# ---------------------- BtbN FFmpeg ------------------------
function install_btbn_ffmpeg {
    if is_btbn_installed; then
        echo "BtbN FFmpeg is already installed at $BTBN_DIR."
        return
    fi

    echo "Installing BtbN FFmpeg into $BTBN_DIR..."
    mkdir -p "$BTBN_DIR" "$TEMP_DIR"
    wget --no-verbose -O "$TEMP_DIR/ffmpeg-static.tar.xz" "$BTBN_URL"

    # Extract to temp directory first
    mkdir -p "$TEMP_DIR/extract"
    tar -xf "$TEMP_DIR/ffmpeg-static.tar.xz" -C "$TEMP_DIR/extract" --strip-components=1

    # Only keep ffmpeg and ffprobe binaries from bin/
    mkdir -p "$BTBN_DIR"
    cp "$TEMP_DIR/extract/bin/ffmpeg" "$BTBN_DIR/"
    cp "$TEMP_DIR/extract/bin/ffprobe" "$BTBN_DIR/"

    # Ensure executables
    chmod +x "$BTBN_DIR/ffmpeg" "$BTBN_DIR/ffprobe"

    # Cleanup temp
    rm -rf "$TEMP_DIR"

    echo "BtbN FFmpeg installed in $BTBN_DIR"
}

function is_btbn_installed {
    [ -x "$BTBN_DIR/ffmpeg" ] && [ -x "$BTBN_DIR/ffprobe" ]
}

function uninstall_btbn_ffmpeg {
    echo "Uninstalling BtbN FFmpeg from $BTBN_DIR..."
    rm -rf "$BTBN_DIR"
}

# ---------------------- Main Actions ------------------------
function install_all {
    if [ -z "$common" ]; then
        echo "❌ ERROR: \$common is not set. Please export it before running the script."
        exit 1
    fi

    echo "Installing to persistent path: $common"
    install_jellyfin_ffmpeg
    install_btbn_ffmpeg
    echo "✅ All components installed."
}

function uninstall_all {
    uninstall_jellyfin_ffmpeg
    uninstall_btbn_ffmpeg
    echo "🗑️ All components uninstalled."
}

# ---------------------- Entrypoint ------------------------
if [ "$1" == "--uninstall" ]; then
    uninstall_all
else
    install_all
fi

exit 0