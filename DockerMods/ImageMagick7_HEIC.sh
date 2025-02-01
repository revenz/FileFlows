#!/bin/bash
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
