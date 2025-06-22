# ----------------------------------------------------------------------------------------------------
# Name: AutoCRF
# Author: lawrence
# Description: This DockerMod installs ab-av1 and a FFmpeg wrapper script, it requires both FFmpeg7 and FFmpeg-BtbN installed (you may have to uninstall FFmpeg6 first)
# Revision: 4
# Icon: fas fa-compress-alt
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

DESTINATION_FOLDER="/opt/autocrf"

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

# Check if zstd is installed
if command -v zstd &>/dev/null; then
    echo "zstd already installed."
else
    # Update package lists and install dependencies
    if ! apt update || ! apt install -y zstd; then
        handle_error
    fi
fi

# Install auto crf
mkdir -p ${DESTINATION_FOLDER}
wget -O ${DESTINATION_FOLDER}/ab-av1.tar.zst $(curl -s https://api.github.com/repos/alexheretic/ab-av1/releases/latest | grep -m 1 'browser_' | cut -d\" -f4)
tar xvf ${DESTINATION_FOLDER}/ab-av1.tar.zst -C ${DESTINATION_FOLDER}
rm ${DESTINATION_FOLDER}/ab-av1.tar.zst

cat > ${DESTINATION_FOLDER}/ffmpeg <<EOF
#!/bin/bash

if [[ "\$@" =~ libvmaf|libsvtav1|libaom-av1 ]]; then
    if [ -e /opt/ffmpeg-uranite-static/bin/ffmpeg ]; then
        /opt/ffmpeg-uranite-static/bin/ffmpeg "\$@"
    else
        /opt/ffmpeg-static/bin/ffmpeg "\$@"
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
