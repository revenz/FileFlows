# ----------------------------------------------------------------------------------------------------
# Name: comskip
# Description: Comskip is a application used for commercial detection in video files. It analyzes MPEG or H.264 files and generates output files containing the location of commercials.
# Author: John Andrews
# Revision: 1
# Default: true
# Icon: fas fa-tv:#DA70D6
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if comskip is installed
if ! command -v comskip &>/dev/null; then
    echo "comskip is not installed. Installing..."
    # Update package lists
    apt update

    # Install comskip
    apt install -y comskip
    
    echo "Installation complete."
else
    echo "comskip is already installed."
fi