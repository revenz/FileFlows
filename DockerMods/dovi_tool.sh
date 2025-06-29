# ----------------------------------------------------------------------------------------------------
# Name: dovi_tool
# Description: dovi_tool is a CLI tool combining multiple utilities for working with Dolby Vision.
# Author: lawrence / iBuSH
# Revision: 10
# Icon: data:image/svg+xml;base64,PCFET0NUWVBFIHN2ZyBQVUJMSUMgIi0vL1czQy8vRFREIFNWRyAxLjEvL0VOIiAiaHR0cDovL3d3dy53My5vcmcvR3JhcGhpY3MvU1ZHLzEuMS9EVEQvc3ZnMTEuZHRkIj4KDTwhLS0gVXBsb2FkZWQgdG86IFNWRyBSZXBvLCB3d3cuc3ZncmVwby5jb20sIFRyYW5zZm9ybWVkIGJ5OiBTVkcgUmVwbyBNaXhlciBUb29scyAtLT4KPHN2ZyBmaWxsPSIjZmZmZmZmIiB3aWR0aD0iODAwcHgiIGhlaWdodD0iODAwcHgiIHZpZXdCb3g9IjAgMCAyNCAyNCIgcm9sZT0iaW1nIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHN0cm9rZT0iI2ZmZmZmZiI+Cg08ZyBpZD0iU1ZHUmVwb19iZ0NhcnJpZXIiIHN0cm9rZS13aWR0aD0iMCIvPgoNPGcgaWQ9IlNWR1JlcG9fdHJhY2VyQ2FycmllciIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIi8+Cg08ZyBpZD0iU1ZHUmVwb19pY29uQ2FycmllciI+Cg08cGF0aCBkPSJNMjQgMjAuMzUyVjMuNjQ4SDB2MTYuNzA0aDI0ek0xOC40MzMgNS44MDZoMi43MzZ2MTIuMzg3aC0yLjczNmMtMi44MzkgMC01LjIxNC0yLjc2Ny01LjIxNC02LjE5NHMyLjM3NS02LjE5MyA1LjIxNC02LjE5M3ptLTE1LjYwMiAwaDIuNzM2YzIuODM5IDAgNS4yMTQgMi43NjcgNS4yMTQgNi4xOTRzLTIuMzc0IDYuMTk0LTUuMjE0IDYuMTk0SDIuODMxVjUuODA2eiIvPgoNPC9nPgoNPC9zdmc+
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling dovi_tool..."
    if rm -f /bin/dovi_tool; then
        echo "dovi_tool successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if dovi_tool is installed
if command -v dovi_tool &>/dev/null; then
    echo "already installed."
    exit 0
fi

echo "dovi_tool is not installed. Installing..."

# Update package lists and install dependencies
if ! apt-get -qq update || ! apt-get install -yqq libfontconfig-dev; then
    handle_error
fi

# Install dovi_tool
wget --no-verbose -O /tmp/dovi_tool.tar.gz $(curl https://api.github.com/repos/quietvoid/dovi_tool/releases/latest | grep 'browser_' | grep -m 1 x86_64-unknown-linux | cut -d\" -f4)
tar xvf /tmp/dovi_tool.tar.gz
mv dovi_tool /bin
rm /tmp/dovi_tool.tar.gz

echo "Installation complete."

# Verify installation
if command -v dovi_tool &>/dev/null; then
    echo "Successfully installed."
    exit 0
fi

echo "Failed to install."
exit 1