# ----------------------------------------------------------------------------------------------------
# Name: AutoCRF
# Author: lawrence
# Description: This DockerMod installs ab-av1 and a FFmpeg wrapper script, it requires both FFmpeg FileFlows Edition installed (you may have to uninstall other FFmpegs)
# Revision: 6
# Icon: fas fa-compress-alt
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

DESTINATION_FOLDER="/app/common/autocrf"

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling auto zstd..."
    if apt remove -y zstd; then
        echo "zstd crf successfully uninstalled."
    else
        handle_error
    fi
    if rm -rf ${DESTINATION_FOLDER}; then
        echo "auto crf successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Install openCL if we find a QSV device
! lspci | grep -Ei 'VGA|Display' | grep Intel || ! apt-get -qq update || ! apt-get install -yqq libmfx-gen1.2 libmfx-dev i965-va-driver-shaders intel-media-va-driver-non-free intel-opencl-icd

if [ -f ${DESTINATION_FOLDER}/ab-av1 ]; then
    echo "AutoCRF already installed."
    exit 0
fi

# Check if zstd is installed
if command -v zstd &>/dev/null; then
    echo "zstd already installed."
else
    # Update package lists and install dependencies
    if ! apt-get -qq update || ! apt-get install -yqq zstd; then
        handle_error
    fi
fi

# Install auto crf
mkdir -p ${DESTINATION_FOLDER}
wget --no-verbose -O ${DESTINATION_FOLDER}/ab-av1.tar.zst $(curl -s https://api.github.com/repos/alexheretic/ab-av1/releases/latest | grep -m 1 'browser_' | cut -d\" -f4)
tar xvf ${DESTINATION_FOLDER}/ab-av1.tar.zst -C ${DESTINATION_FOLDER}
rm ${DESTINATION_FOLDER}/ab-av1.tar.zst
mkdir -p ${DESTINATION_FOLDER}/cache
chmod 777 ${DESTINATION_FOLDER}/cache

cat > ${DESTINATION_FOLDER}/ffmpeg <<EOF
#!/bin/bash

if [[ "\$@" =~ libvmaf|libsvtav1|libaom-av1 ]]; then
    if [ -e /opt/ffmpeg-uranite-static/bin/ffmpeg ]; then
        /opt/ffmpeg-uranite-static/bin/ffmpeg "\$@"
    else
        /app/common/ffmpeg-static/ffmpeg "\$@"
    fi
else
    if [ -e /usr/local/bin/ffmpeg ]; then
        /usr/local/bin/ffmpeg "\$@"
    fi
fi

exit \$?
EOF

chmod +x ${DESTINATION_FOLDER}/ffmpeg

echo "Installation complete."

# Verify installation
if command -v ${DESTINATION_FOLDER}/ab-av1 &>/dev/null; then
    echo "Successfully installed."
    exit 0
fi

echo "Failed to install."
exit 1
