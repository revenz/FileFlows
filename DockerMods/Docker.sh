# ----------------------------------------------------------------------------------------------------
# Name: Docker
# Description: Installs Docker inside Docker.  This allows you to launch sibling Docker containers.
# Revision: 1
# Icon: fab fa-docker:#0076BF
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if Docker is installed
if ! command -v docker &>/dev/null; then
    echo "ImageMagick is not installed. Installing..."

    # Install Docker
    curl -fsSL https://get.docker.com | sh;
    
    echo "Installation complete."
else
    echo "Docker is already installed."
fi