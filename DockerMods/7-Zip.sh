# ----------------------------------------------------------------------------------------------------
# Name: 7-Zip
# Description: 7-Zip is a free and open-source file archiver, a utility used to place groups of files within compressed containers known as "archives".
# Author: reven
# Revision: 4
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4KPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIGhlaWdodD0iMzQiIHdpZHRoPSI0NCI+Cgk8cmVjdCB3aWR0aD0iNDIiIGhlaWdodD0iMzIiIGZpbGw9IiNmZmYiLz4KCTxwYXRoIGQ9Im0yOCAxM3YzaDUuM2wtNS4zIDcuN3YzLjNoOXYtM2gtNS4zbDUuMy03Ljd2LTMuM3ptLTE3LTN2M2g3LjVsLTQuNSA0LjV2Ni41aDR2LTYuNWw0LTR2LTMuNW0tMTUtM2gxOHYyMGgtMTh6bS03LTd2MzRoNDR2LTM0em0zIDNoMzh2MjhoLTM4Ii8+Cjwvc3ZnPgo=
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling 7zip..."
    if apt-get remove -y p7zip-full; then
        echo "7zip successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if 7zip is installed
if command -v 7z &>/dev/null; then
    echo "7zip is already installed."
    exit 0
fi

echo "7zip is not installed. Installing..."

# Update package lists
if ! apt-get -qq update; then
    handle_error
fi

# Install p7zip-full
if ! apt install -yqq p7zip-full; then
    handle_error
fi

echo "Installation complete."

# Check if 7zip is now installed
if command -v 7z &>/dev/null; then
    echo "7zip successfully installed."
    exit 0
else
    echo "Failed to install 7zip."
    exit 1
fi
