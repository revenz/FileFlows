# ----------------------------------------------------------------------------------------------------
# Name: comskip
# Description: Comskip is a application used for commercial detection in video files. It analyzes MPEG or H.264 files and generates output files containing the location of commercials.
# Author: reven
# Revision: 4
# Default: true
# Icon: fas fa-tv:#DA70D6
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling comskip..."
    if apt-get remove -y comskip; then
        echo "comskip successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if comskip is installed
if command -v comskip &>/dev/null; then
    echo "comskip is already installed."
    exit 0
fi

echo "comskip is not installed. Installing..."

# Update package lists
if ! apt-get -qq update; then
    handle_error
fi

# Install comskip
if ! apt-get install -yqq comskip; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v comskip &>/dev/null; then
    echo "comskip successfully installed."
    exit 0
else
    echo "Failed to install comskip."
    exit 1
fi
