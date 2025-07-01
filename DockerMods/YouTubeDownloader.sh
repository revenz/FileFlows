# ----------------------------------------------------------------------------------------------------
# Name: YouTube Downloader
# Description: Installs yt-dlp, a powerful command-line tool for downloading videos and audio from YouTube and hundreds of other sites.
# Author: reven
# Revision: 1
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPCEtLSBHZW5lcmF0b3I6IEFkb2JlIElsbHVzdHJhdG9yIDIxLjAuMCwgU1ZHIEV4cG9ydCBQbHVnLUluIC4gU1ZHIFZlcnNpb246IDYuMDAgQnVpbGQgMCkgIC0tPgo8c3ZnIHZlcnNpb249IjEuMSIgaWQ9ItCh0LvQvtC5XzEiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgeG1sbnM6eGxpbms9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkveGxpbmsiIHg9IjBweCIgeT0iMHB4IgoJIHZpZXdCb3g9IjAgMCA0MDAgNDAwIiBlbmFibGUtYmFja2dyb3VuZD0ibmV3IDAgMCA0MDAgNDAwIiB4bWw6c3BhY2U9InByZXNlcnZlIj4KPGcgaWQ9IkJhY2tncm91bmQiPgoJCgkJPGxpbmVhckdyYWRpZW50IGlkPSJTVkdJRF8xXyIgZ3JhZGllbnRVbml0cz0idXNlclNwYWNlT25Vc2UiIHgxPSI5LjA5NDk0N2UtMTMiIHkxPSIxOTkiIHgyPSI0MDAiIHkyPSIxOTkiIGdyYWRpZW50VHJhbnNmb3JtPSJtYXRyaXgoNi4xMjMyMzRlLTE3IDEgMSAtNi4xMjMyMzRlLTE3IDEgMCkiPgoJCTxzdG9wICBvZmZzZXQ9IjAiIHN0eWxlPSJzdG9wLWNvbG9yOiNFNTJEMjciLz4KCQk8c3RvcCAgb2Zmc2V0PSIxIiBzdHlsZT0ic3RvcC1jb2xvcjojQkYxNzFEIi8+Cgk8L2xpbmVhckdyYWRpZW50PgoJPHJlY3QgZmlsbD0idXJsKCNTVkdJRF8xXykiIHdpZHRoPSI0MDAiIGhlaWdodD0iNDAwIi8+CjwvZz4KPGcgaWQ9IkxvZ28iPgoJPHBhdGggaWQ9IlRoZV9TaGFycG5lc3MiIG9wYWNpdHk9IjAuMTIiIGQ9Ik0xNzAuNiwxNTkuOWw2My45LDQyLjdsOS00LjZMMTcwLjYsMTU5Ljl6Ii8+Cgk8ZyBpZD0iTG96ZW5nZSI+CgkJPGc+CgkJCTxwYXRoIGZpbGw9IiNGRkZGRkYiIGQ9Ik0zMzIuMiwxNDYuMWMwLDAtMi42LTE4LjYtMTAuNy0yNi44Yy0xMC4yLTEwLjgtMjEuOC0xMC44LTI3LTExLjRjLTM3LjgtMi43LTk0LjQtMi43LTk0LjQtMi43SDIwMAoJCQkJYzAsMC01Ni42LDAtOTQuNCwyLjdjLTUuMywwLjYtMTYuOCwwLjctMjcsMTEuNGMtOC4xLDguMi0xMC43LDI2LjgtMTAuNywyNi44cy0yLjcsMjEuOC0yLjcsNDMuN3YyMC41YzAsMjEuOCwyLjcsNDMuNywyLjcsNDMuNwoJCQkJczIuNiwxOC42LDEwLjcsMjYuOGMxMC4zLDEwLjgsMjMuNywxMC40LDI5LjcsMTEuNWMyMS42LDIuMSw5MS43LDIuNyw5MS43LDIuN3M1Ni43LTAuMSw5NC41LTIuOGM1LjMtMC42LDE2LjgtMC43LDI3LTExLjQKCQkJCWM4LjEtOC4yLDEwLjctMjYuOCwxMC43LTI2LjhzMi43LTIxLjgsMi43LTQzLjd2LTIwLjVDMzM0LjksMTY3LjksMzMyLjIsMTQ2LjEsMzMyLjIsMTQ2LjF6IE0xNzIuMSwyMzV2LTc1LjhsNzIuOSwzOEwxNzIuMSwyMzUKCQkJCXoiLz4KCQk8L2c+Cgk8L2c+CjwvZz4KPC9zdmc+Cg==
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

set -e

YT_DLP_ROOT="$common/yt-dlp"
YT_DLP_BIN="$YT_DLP_ROOT/yt-dlp"
YT_DLP_UPDATE="$YT_DLP_ROOT/yt-dlp-update"
LOCKFILE="$YT_DLP_ROOT/yt-dlp.lock"

# Handle uninstall
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling yt-dlp..."
    rm -rf "$YT_DLP_ROOT"
    echo "Uninstallation complete."
    exit 0
fi

# Already installed?
if [ -f "$YT_DLP_BIN" ] && [ -f "$YT_DLP_UPDATE" ]; then
    echo "yt-dlp and update script are already installed."
    exit 0
fi

echo "Installing yt-dlp..."

mkdir -p "$YT_DLP_ROOT"

# Download yt-dlp binary
curl -L "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux" -o "$YT_DLP_BIN"
chmod a+rx "$YT_DLP_BIN"

echo "Creating yt-dlp-update script..."

cat << 'EOF' > "$YT_DLP_UPDATE"
#!/bin/bash -e

YT_DLP_BIN="$(dirname "$0")/yt-dlp"
LOCKFILE="$(dirname "$0")/yt-dlp.lock"

# Check if any other yt-dlp process (excluding this script) is running
if pgrep -f yt-dlp | grep -v $$ >/dev/null; then
  echo "yt-dlp is currently running. Skipping update."
  exit 0
fi

exec 200>"$LOCKFILE"
flock -x 200
"$YT_DLP_BIN" -U >/dev/null 2>&1 || true
EOF

chmod a+rx "$YT_DLP_UPDATE"

echo "yt-dlp installation and setup complete."
exit 0