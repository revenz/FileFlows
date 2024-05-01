# ----------------------------------------------------------------------------------------------------
# Name: Git
# Description: Git simplifies the process of downloading and managing various resources, like software packages or configurations, from remote repositories, making it an essential tool for users seeking efficient access to files and projects.
# Revision: 1
# Icon: fab fa-git-alt:#FF4500
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
