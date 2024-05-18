# ----------------------------------------------------------------------------------------------------
# Name: Git
# Description: Git simplifies the process of downloading and managing various resources, like software packages or configurations, from remote repositories, making it an essential tool for users seeking efficient access to files and projects.
# Author: John Andrews
# Revision: 2
# Icon: fab fa-git-alt:#FF4500
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if Git is installed
if command -v git &>/dev/null; then
    echo "Git is already installed."
    exit 0
fi

echo "Git is not installed. Installing..."

# Update package lists and install Git
if ! apt update || ! apt install -y git; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v git &>/dev/null; then
    echo "Git successfully installed."
    exit 0
fi

echo "Failed to install Git."
exit 1
