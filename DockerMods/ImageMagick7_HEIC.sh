# ----------------------------------------------------------------------------------------------------
# Name: ImageMagick 7 with HEIC read-write enabled
# Description: ImageMagick, invoked from the command line as magick, is a free and open-source cross-platform software suite for displaying, creating, converting, modifying, and editing raster images. This docker mod is compiled on the spot to obtain version 7 of ImageMagick with HEIC read-write support. This is needed in order to be able to use the Image Convert flow element with output set as HEIC.
# Author: AlexandruNegura
# Revision: 1
# Icon: data:image/svg+xml;base64,PHN2ZyBoZWlnaHQ9IjQ4IiB2aWV3Qm94PSIwIDAgNDggNDgiIHdpZHRoPSI0OCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayI+PGxpbmVhckdyYWRpZW50IGlkPSJhIiBncmFkaWVudFVuaXRzPSJ1c2VyU3BhY2VPblVzZSIgeDE9Ii05MC4wODIyODEiIHgyPSItOTAuMDgyMjgxIiB5MT0iMTA0NS40ODY1OTYiIHkyPSIxMDAyLjU5NTQzOCI+PHN0b3Agb2Zmc2V0PSIwIiBzdG9wLWNvbG9yPSIjMDBhYWQ0Ii8+PHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSIjMmFkNGZmIi8+PC9saW5lYXJHcmFkaWVudD48bGluZWFyR3JhZGllbnQgaWQ9ImIiIGdyYWRpZW50VW5pdHM9InVzZXJTcGFjZU9uVXNlIiB4MT0iMjMuMjMzNTEiIHgyPSIyMy4xMDcyMzkiIHkxPSIxMDM5LjQ4Mjc1NSIgeTI9IjEwMTUuNjE3OTAxIj48c3RvcCBvZmZzZXQ9IjAiIHN0b3AtY29sb3I9IiMyZDJkMmQiLz48c3RvcCBvZmZzZXQ9IjEiIHN0b3AtY29sb3I9IiMzYzNjM2MiLz48L2xpbmVhckdyYWRpZW50PjxnIHRyYW5zZm9ybT0idHJhbnNsYXRlKDAgLTEwMDQuMzYyMikiPjxyZWN0IGZpbGw9IiMwMDY2ODAiIGhlaWdodD0iNDMuMzIxNjE3IiByeD0iMy45MzgzMjkiIHRyYW5zZm9ybT0ibWF0cml4KC45OTM4Nzk1OCAtLjExMDQ2ODkyIC4xMTA0Njg5MiAuOTkzODc5NTggMCAwKSIgd2lkdGg9IjQzLjMyMTYxNyIgeD0iLTExMS40ODU3MiIgeT0iMTAwMy41NDI1Ii8+PHJlY3QgZmlsbD0idXJsKCNhKSIgaGVpZ2h0PSI0My4zMjE2MTciIHJ4PSIzLjkzODMyOSIgdHJhbnNmb3JtPSJtYXRyaXgoLjk5Mzg3OTU4IC0uMTEwNDY4OTIgLjExMDQ2ODkyIC45OTM4Nzk1OCAwIDApIiB3aWR0aD0iNDMuMzIxNjE3IiB4PSItMTExLjQ4NTcyIiB5PSIxMDAyLjU1NzciLz48ZyBmaWxsPSJub25lIj48cGF0aCBkPSJtMjIuNjU1NzMgMTAyNC4wNjk1IDIuNjU2LS4xNDEgMS40MDYtMi4xMTMuOTY1IDIuNDEgMi41NTkuOTE0LTEuOTczIDEuNTc0LS4wNDMgMi41NTUtMi4wOTQtMS4zNC0yLjgwMS45MDIuNzg1LTIuNDc3bS0xLjQ2MS0yLjI4NSIvPjxwYXRoIGQ9Im0yMi4yNTc3MyAxMDMxLjc1MjUgMi40OTYtLjkxOC43MTUtMi40MzggMS42MzMgMi4wMSAyLjcxNS4xMTctMS40MTQgMi4wODYuNzE1IDIuNDUzLTIuMzk1LS42NTYtMi40MDYgMS42OTEuMDE2LTIuNTk4bS0yLjA3NC0xLjc0NiIvPjxwYXRoIGQ9Im0xNS43OTY3MyAxMDI4LjUyMjUgMi42NTIuMTk5IDEuNjYtMS45MjIuNjUyIDIuNTEyIDIuNDIyIDEuMjM0LTIuMTU2IDEuMzA5LS4zNjcgMi41MjctMS45MDItMS41OS0yLjg5NS41MzkgMS4wOS0yLjM1NW0tMS4xNTYtMi40NTMiLz48L2c+PHBhdGggZD0ibTI2LjcyOTczIDEwMTUuNzAyNWMtMi40OC4zMi01LjE0NSAxLjUyNy03LjMxMyA1LjA5OC00Ljk1NyA4LjE2LTQuNjA1IDExLTUuMzk1IDExLjkxOC0uNzkzLjkxNC0yLjkzIDEuNDEtMi42MjEgMi40NDkuMzA5IDEuMDM1LjYyMS45MDIuNjIxLjkwMnMzLjEyMSA0LjUyIDEyLjE3MiAzLjM0OGM5LjA1LTEuMTcyIDEwLjMwNS0zLjA4MiAxMC4yMjMtNC4wMy0uMDgyLS45NDktMS45OTItMS42NDEtMS45OTItMS42NDFzLTEuNDE0LTQuOTg0LTEuMDUxLTcuMDNjLjM2My0yLjA0NyAxLjc0Mi01LjAzIDMuMTA1LTUuNjcyIDEuMzU5LS42NDggMi4wMzkgMi4xNjggMi43NDIgMS40NzMuNjk5LS42OTUtMS4xOC02LjA2LTMuMjU4LTYuMTAyLTEuMTY0LS4wMjMtNC4wNTktMS4xMjEtNy4yMzQtLjcwN20uMDA4IDYuODQ4LjY5MSAyLjA0MyAyLjA5OC41OTQtMS43NzMgMS4yNjIuMDc4IDIuMTQ1LTEuNzczLTEuMjYyLTIuMDkuNzMuNjcyLTIuMDItMS4zNDgtMS42OTUgMi4yMDMtLjAybS01Ljg0IDQuMjI3LjA3NCAxLjQ4IDEuMzI0Ljc5My0xLjQzLjUxNi0uMzU5IDEuNDY1LS45NzMtMS4xNDEtMS41MjMuMDk4LjgxNi0xLjIzNC0uNTgyLTEuMzg3IDEuNDc3LjM4M203LjA4Mi0uNjY0IDEuNDE4IDEuODk4IDIuMzk1LS4wOTQtMS4zOTEgMS45MjIuODE2IDIuMTg4LTIuMjk3LS43MTEtMS44ODcgMS40NTctLjAxNi0yLjM1OS0xLjk4OC0xLjI5NyAyLjI3Ny0uNzQ2bS42NzItMi4yNTgiIGZpbGw9InVybCgjYikiLz48L2c+PC9zdmc+
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

function uninstall_image_magick() {
    export PKG_CONFIG_PATH=/usr/src:/usr/src/libheif:/usr/src/libde265:/usr/src/libpng-1.6.46

    cd /usr/src/ImageMagick-7*
    make uninstall

    cd /usr/src/libheif/
    make uninstall

    cd /usr/src/libde265/
    make uninstall

    cd /usr/src/libpng-1.6.46/
    make uninstall

    apt-get remove -y jq

    apt-get remove -y \
      build-essential \
      autotools-dev \
      autoconf \
      automake \
      libtool \
      cmake \
      git \
      libx265-dev \
      libnuma-dev \
      libdjvulibre-dev \
      libfftw3-dev \
      libghc-bzlib-dev \
      libgoogle-perftools-dev \
      libgraphviz-dev \
      libgs-dev \
      libjbig-dev \
      libjemalloc-dev \
      libjpeg-dev \
      liblcms2-dev \
      liblqr-1-0-dev \
      liblzma-dev \
      libopenexr-dev \
      libopenjp2-7-dev \
      libpango1.0-dev \
      libraqm-dev \
      libraw-dev \
      librsvg2-dev \
      libtiff-dev \
      libwebp-dev \
      libwmf-dev \
      libxml2-dev \
      libzip-dev \
      libzstd-dev

      rm -rf /usr/src/libpng-1.6.46/
      rm -rf /usr/src/libde265/
      rm -rf /usr/src/libheif/
      rm -rf /usr/src/ImageMagick-7*
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling ImageMagick..."
    if uninstall_image_magick; then
        echo "ImageMagick successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi


# Check if ImageMagick is installed
if command -v magick &>/dev/null; then
    echo "ImageMagick is already installed."
    exit 0
fi

cd /usr/src/

#jq
apt-get install -y jq

# Installing tools
apt-get install -y \
  build-essential \
  autotools-dev \
  autoconf \
  automake \
  libtool \
  cmake \
  git \
  libx265-dev \
  libnuma-dev \
  libdjvulibre-dev \
  libfftw3-dev \
  libghc-bzlib-dev \
  libgoogle-perftools-dev \
  libgraphviz-dev \
  libgs-dev \
  libjbig-dev \
  libjemalloc-dev \
  libjpeg-dev \
  liblcms2-dev \
  liblqr-1-0-dev \
  liblzma-dev \
  libopenexr-dev \
  libopenjp2-7-dev \
  libpango1.0-dev \
  libraqm-dev \
  libraw-dev \
  librsvg2-dev \
  libtiff-dev \
  libwebp-dev \
  libwmf-dev \
  libxml2-dev \
  libzip-dev \
  libzstd-dev

# Install libpng
wget https://sourceforge.net/projects/libpng/files/libpng16/1.6.46/libpng-1.6.46.tar.gz --no-check-certificate
tar -zxvf libpng-1.6.46.tar.gz
cd libpng-1.6.46/
./configure
make
make install

# Setting the PACKAGE CONFIG PATH so that imagemagick knows of stuff we compiled
export PKG_CONFIG_PATH=/usr/src:/usr/src/libheif:/usr/src/libde265:/usr/src/libpng-1.6.46

# Install libde265
cd /usr/src/
git clone https://github.com/strukturag/libde265.git
cd libde265/
./autogen.sh
./configure
make
make install

# Install libheif
cd /usr/src/
git clone https://github.com/lomorage/libheif
cd libheif/
./autogen.sh
./configure
make
make install

#Install ImageMagick
cd /usr/src/
wget https://www.imagemagick.org/download/ImageMagick.tar.gz --no-check-certificate
tar -zxvf ImageMagick.tar.gz
cd ImageMagick-7*
./configure \
  --with-bzlib=yes \
  --with-djvu=yes \
  --with-dps=yes \
  --with-fftw=yes \
  --with-flif=yes \
  --with-fontconfig=yes \
  --with-fpx=yes \
  --with-freetype=yes \
  --with-gslib=yes \
  --with-gvc=yes \
  --with-heic=yes \
  --with-jbig=yes \
  --with-jemalloc=yes \
  --with-jpeg=yes \
  --with-jxl=yes \
  --with-lcms=yes \
  --with-lqr=yes \
  --with-lzma=yes \
  --with-magick-plus-plus=yes \
  --with-openexr=yes \
  --with-openjp2=yes \
  --with-pango=yes \
  --with-perl=yes \
  --with-png=yes \
  --with-raqm=yes \
  --with-raw=yes \
  --with-rsvg=yes \
  --with-tcmalloc=yes \
  --with-tiff=yes \
  --with-webp=yes \
  --with-wmf=yes \
  --with-x=yes \
  --with-xml=yes \
  --with-zip=yes \
  --with-zlib=yes \
  --with-zstd=yes \
  --with-gcc-arch=native

make
make install
ldconfig /usr/local/lib
identify --version

#cleanup
cd /usr/src/
rm -rf ImageMagick.tar.gz
rm -rf libpng-1.6.46.tar.gz
