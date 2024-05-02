# ----------------------------------------------------------------------------------------------------
# Name: FFmpeg6
# Description: FFmpeg is a free and open-source software project consisting of a suite of libraries and programs for handling video, audio, and other multimedia files and streams. At its core is the command-line ffmpeg tool itself, designed for processing of video and audio files.
# Revision: 1
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4NCjwhLS0gR2VuZXJhdG9yOiBBZG9iZSBJbGx1c3RyYXRvciAyNi4wLjEsIFNWRyBFeHBvcnQgUGx1Zy1JbiAuIFNWRyBWZXJzaW9uOiA2LjAwIEJ1aWxkIDApICAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgeD0iMHB4IiB5PSIwcHgiDQoJIHZpZXdCb3g9IjAgMCAyMDAwIDIwMDAiIHN0eWxlPSJlbmFibGUtYmFja2dyb3VuZDpuZXcgMCAwIDIwMDAgMjAwMDsiIHhtbDpzcGFjZT0icHJlc2VydmUiPg0KPHN0eWxlIHR5cGU9InRleHQvY3NzIj4NCgkuc3Qwe2ZpbGw6bm9uZTtzdHJva2U6IzM3OEU0MztzdHJva2Utd2lkdGg6MzAwO3N0cm9rZS1saW5lY2FwOnJvdW5kO3N0cm9rZS1saW5lam9pbjpyb3VuZDtzdHJva2UtbWl0ZXJsaW1pdDo4O30NCjwvc3R5bGU+DQo8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSg1LDUpIj4NCgk8cGF0aCBjbGFzcz0ic3QwIiBkPSJNMTY2LjcsMTY2LjdoNTUyLjJMMTY2LjcsNzE4Ljl2NTUyLjJMMTI3MS4xLDE2Ni43aDU1Mi4yTDE2Ni43LDE4MjMuM2g1NTIuMkwxODIzLjMsNzE4Ljl2NTUyLjJsLTU1Mi4yLDU1Mi4yDQoJCWg1NTIuMiIvPg0KPC9nPg0KPC9zdmc+DQo=
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

if command -v ffmpeg &>/dev/null; then
    echo "FFmpeg is already installed."
    exit
fi

architecture=$(uname -m)

echo "Architecture: $architecture"

if [ "$architecture" == "x86_64" ]; then

  echo "The architecture is AMD (x86_64)."

  apt-get update
  wget -O - https://repo.jellyfin.org/jellyfin_team.gpg.key | apt-key add -
  echo "deb [arch=$( dpkg --print-architecture )] https://repo.jellyfin.org/$( awk -F'=' '/^ID=/{ print $NF }' /etc/os-release ) $( awk -F'=' '/^VERSION_CODENAME=/{ print $NF }' /etc/os-release ) main" | tee /etc/apt/sources.list.d/jellyfin.list
  apt-get update
  apt-get install --no-install-recommends --no-install-suggests -y jellyfin-ffmpeg6 
  ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg
  ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/local/bin/ffprobe

elif [ "$architecture" == "armv7l" ] || [ "$architecture" == "aarch64" ] || [[ "$architecture" =~ [Aa][Rr][Mm] ]]; then

  echo "The architecture is ARM."

  apt-get update
  wget -O - https://repo.jellyfin.org/jellyfin_team.gpg.key | apt-key add -
  echo "deb [arch=$( dpkg --print-architecture )] https://repo.jellyfin.org/$( awk -F'=' '/^ID=/{ print $NF }' /etc/os-release ) $( awk -F'=' '/^VERSION_CODENAME=/{ print $NF }' /etc/os-release ) main" | tee /etc/apt/sources.list.d/jellyfin.list
  apt-get update
  apt-get install --no-install-recommends --no-install-suggests -y jellyfin-ffmpeg6
  ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg
  ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/local/bin/ffprobe

else

  echo "The architecture is not recognized as AMD or ARM: $architecture."

fi