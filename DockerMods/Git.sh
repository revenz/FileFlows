# ----------------------------------------------------------------------------------------------------
# Name: Git
# Description: Git simplifies the process of downloading and managing various resources, like software packages or configurations, from remote repositories, making it an essential tool for users seeking efficient access to files and projects.
# Author: reven
# Revision: 4
# Icon: fab fa-git-alt:#FF4500
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling git..."
    if apt-get remove -y git; then
        echo "git successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if Git is installed
if command -v git &>/dev/null; then
    echo "Git is already installed."
    exit 0
fi

echo "Git is not installed. Installing..."

# Update package lists and install Git
if ! apt-get -qq update || ! apt-get install -yqq git; then
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
