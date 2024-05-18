# ----------------------------------------------------------------------------------------------------
# Name: Docker
# Description: Installs Docker inside Docker.  This allows you to launch sibling Docker containers.
# Author: John Andrews
# Revision: 2
# Icon: fab fa-docker:#0076BF
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if Docker is installed
if command -v docker &>/dev/null; then
    echo "Docker is already installed."
    exit 0
fi

echo "Docker is not installed. Installing..."

# Install Docker
if ! curl -fsSL https://get.docker.com | sh; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v docker &>/dev/null; then
    echo "Docker successfully installed."
    exit 0
else
    echo "Failed to install Docker."
    exit 1
fi
