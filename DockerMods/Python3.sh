# ----------------------------------------------------------------------------------------------------
# Name: Python3
# Description: Installs Python3 into the Docker container if not already installed
# Author: John Andrews
# Revision: 1
# Icon: fab fa-python:#F1F000
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if Python 3 is installed
if command -v python3 &>/dev/null; then
    echo "Python 3 is already installed."
else
    echo "Python 3 is not installed. Installing..."
    sudo apt update
    sudo apt install -y python3
    echo "Python 3 has been installed."
fi