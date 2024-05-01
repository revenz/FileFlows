# ----------------------------------------------------------------------------------------------------
# Name: comskip
# Description: Comskip is a console application used for commercial detection in video files. It analyzes MPEG or H.264 files based on configurable parameters and generates output files containing the location of commercials. It supports various formats for video editing, cutting, and playback, but cannot read copy-protected recordings.
# Author: John Andrews
# Revision: 1
# Icon: fas fa-tv:#DA70D6
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if Git is installed
if ! command -v git &>/dev/null; then
    echo "Git is not installed. Installing..."
    # Update package lists
    apt update

    # Install Git
    apt install -y git
    
    echo "Installation complete."
else
    echo "Git is already installed."
fi