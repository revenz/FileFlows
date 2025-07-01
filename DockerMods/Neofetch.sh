# ----------------------------------------------------------------------------------------------------
# Name: Neofetch
# Description: Neofetch is a system information tool written in the Bash shell scripting language. By default, on the left side is a logo of the distribution, rendered in ASCII art.
# Author: reven
# Revision: 4
# Icon: data:image/svg+xml;base64,PHN2ZyB2aWV3Qm94PSIwIDAgMTYgMTYiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgZmlsbC1ydWxlPSJldmVub2RkIiBjbGlwLXJ1bGU9ImV2ZW5vZGQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiIHN0cm9rZS1taXRlcmxpbWl0PSIxLjQxNCI+PHBhdGggZmlsbD0iI0U5NTQyMCIgZD0iTTggMGM0LjQxNSAwIDggMy41ODUgOCA4cy0zLjU4NSA4LTggOC04LTMuNTg1LTgtOCAzLjU4NS04IDgtOHptMi4xODYgMTEuNzg2Yy0uNTEuMjk1LS42ODYuOTQ4LS4zOSAxLjQ2LjI5NC41MS45NDcuNjg1IDEuNDU4LjM5LjUxLS4yOTUuNjg2LS45NDguMzktMS40Ni0uMjk0LS41MS0uOTQ3LS42ODUtMS40NTgtLjM5ek04IDExLjEyYy0uNDcgMC0uOTE1LS4xMDUtMS4zMTQtLjI5bC0uNzQyIDEuMzNjLjYyLjMwNiAxLjMxOC40OCAyLjA1Ni40OC40MyAwIC44NDUtLjA2IDEuMjQtLjE3LjA3LS40MjguMzI0LS44MjMuNzMtMS4wNTguNDA2LS4yMzQuODc1LS4yNTcgMS4yOC0uMTA0Ljc5LS43NzYgMS4zMDUtMS44MzMgMS4zOC0zLjAxbC0xLjUyMy0uMDIyQzEwLjk2NyA5Ljg3IDkuNjMgMTEuMTIgOCAxMS4xMnpNNC44OCA4YzAtMS4wNTUuNTI1LTEuOTg4IDEuMzI3LTIuNTUzbC0uNzgtMS4zMDhjLS45MzYuNjItMS42MyAxLjU4LTEuOTIgMi42OS4zMzcuMjcuNTUzLjY5LjU1MyAxLjE2cy0uMjE2Ljg4LS41NTMgMS4xNmMuMjkgMS4xMS45ODQgMi4wNyAxLjkyIDIuNjlsLjc4LTEuMzFDNS40MDUgOS45OCA0Ljg4IDkuMDUgNC44OCA4ek0yLjU2IDYuOTMyYy0uNTkgMC0xLjA2OC40NzgtMS4wNjggMS4wNjggMCAuNTkuNDc4IDEuMDY4IDEuMDY4IDEuMDY4LjU5IDAgMS4wNjgtLjQ3OCAxLjA2OC0xLjA2OCAwLS41OS0uNDc4LTEuMDY4LTEuMDY4LTEuMDY4ek04IDQuODhjMS42MyAwIDIuOTY3IDEuMjUgMy4xMDcgMi44NDNMMTIuNjMgNy43Yy0uMDc2LTEuMTc2LS41OS0yLjIzMi0xLjM4LTMuMDEtLjQwNi4xNTUtLjg3NS4xMy0xLjI4LS4xMDMtLjQwNi0uMjM0LS42Ni0uNjMtLjczLTEuMDZDOC44NDQgMy40MiA4LjQzIDMuMzYgOCAzLjM2Yy0uNzQgMC0xLjQzNy4xNzMtMi4wNTYuNDhsLjc0MiAxLjMzYy40LS4xODYuODQ1LS4yOSAxLjMxNC0uMjl6bTIuMTg2LS42NjdjLjUxLjI5NSAxLjE2NC4xMiAxLjQ2LS4zOS4yOTQtLjUxLjEyLTEuMTY0LS4zOTItMS40Ni0uNTEtLjI5NC0xLjE2NC0uMTItMS40Ni4zOTItLjI5NC41MS0uMTIgMS4xNjMuMzkyIDEuNDU4eiIvPjwvc3ZnPg==
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling Neofetch..."
    if apt-get remove -y neofetch; then
        echo "Neofetch successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if Neofetch is installed
if command -v neofetch &>/dev/null; then
    echo "Neofetch is already installed."
    exit 0
fi

echo "Neofetch is not installed. Installing..."

# Update package lists and install Neofetch
if ! apt-get -qq update || ! apt install -yqq neofetch; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v neofetch &>/dev/null; then
    echo "Neofetch successfully installed."
    exit 0
fi

echo "Failed to install Neofetch."
exit 1
