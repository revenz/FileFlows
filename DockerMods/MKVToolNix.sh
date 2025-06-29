# ----------------------------------------------------------------------------------------------------
# Name: MKVToolNix
# Description: MKVToolNix is a set of tools to create, alter, split, join, and inspect Matroska (MKV) files.
# Author: reven
# Revision: 4
# Icon: data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI2NCIgaGVpZ2h0PSI2NCIgdmVyc2lvbj0iMSI+CiA8cGF0aCBzdHlsZT0ib3BhY2l0eTowLjIiIGQ9Im0gNCw1NiAwLDIgYyAwLDEuNjYyIDEuMzM4LDMgMywzIGwgNTAsMCBjIDEuNjYyLDAgMywtMS4zMzggMywtMyBsIDAsLTIgYyAwLDEuNjYyIC0xLjMzOCwzIC0zLDMgTCA3LDU5IEMgNS4zMzgsNTkgNCw1Ny42NjIgNCw1NiBaIi8+CiA8cmVjdCBzdHlsZT0iZmlsbDojZTRlNGU0IiB3aWR0aD0iNTYiIGhlaWdodD0iNTYiIHg9Ii02MCIgeT0iLTYwIiByeD0iMyIgcnk9IjMiIHRyYW5zZm9ybT0ibWF0cml4KDAsLTEsLTEsMCwwLDApIi8+CiA8cGF0aCBzdHlsZT0ib3BhY2l0eTowLjE7ZmlsbDojZmZmZmZmIiBkPSJNIDcgNCBDIDUuMzM4IDQgNCA1LjMzOCA0IDcgTCA0IDggQyA0IDYuMzM4IDUuMzM4IDUgNyA1IEwgNTcgNSBDIDU4LjY2MiA1IDYwIDYuMzM4IDYwIDggTCA2MCA3IEMgNjAgNS4zMzggNTguNjYyIDQgNTcgNCBMIDcgNCB6Ii8+CiA8cGF0aCBzdHlsZT0iZmlsbDpub25lO3N0cm9rZTojNDc0NzQ3O3N0cm9rZS13aWR0aDoxLjQyOTk5OTk1IiBkPSJNIDQ1LjYyNCwyNS40MzQgQSAxNy4xODAyLDEyLjc0NzIgNTkgMCAxIDIzLjc3MiwzOC41NjYgMTcuMTgwMiwxMi43NDcyIDU5IDAgMSA0NS42MjQsMjUuNDM0IFoiLz4KIDxwYXRoIHN0eWxlPSJmaWxsOiMyYTU4YWUiIGQ9Im0gMzQsMjQgYyAtNS41MjQyLDAgLTEwLDQuNDc0NiAtMTAsMTAgMCw1LjUyNTQgNC40NzQ2LDEwIDEwLDEwIDUuNTI1NCwwIDEwLC00LjQ3NDYgMTAsLTEwIDAsLTUuNTI1NCAtNC40NzQ2LC0xMCAtMTAsLTEwIHogbSAtMS43ODg1LDUuMjg0OCBjIDMuNzU2NiwwIDYuODI4OCwzLjAzMiA2LjgyODgsNi43ODg2IDAsMy43NTY2IC0zLjA3MjQsNi44Mjg4IC02LjgyODgsNi44Mjg4IC0zLjc1NjYsMCAtNi43ODg2LC0zLjA3MjQgLTYuNzg4NiwtNi44Mjg4IDAsLTMuNzU2NiAzLjAzMiwtNi43ODg2IDYuNzg4NiwtNi43ODg2IHoiLz4KIDxwYXRoIHN0eWxlPSJmaWxsOiM0NzQ3NDciIGQ9Im0gMTMuNzIwOCwzMi45OTIgYyAwLDAgLTQuMDE3LDMuODM1OCAtMy43MDM0LDYuMjMxNiAwLjQzMjkyLDMuMzA0NCA0LjU4MjQsMy44MzE0IDcuODU3MiwzLjc5MzIgNy45Mzk0LC0wLjA5IDM2LjIxOCwtOS4zNjggMzYuMTI2LC0yMC41IC0wLjAzLC0zLjU5ODIgLTYuNTAyLC0zLjkxNjIgLTYuNTAyLC0zLjcwMjggMCwwIDQuODAwNCwxLjIwNDUyIDQuNjk1OCwzLjUyMjIgLTAuMzkwMjIsOC42MzkyIC0yNC4wMSwxOC4zMzg2IC0zNC41LDE4LjYwMzYgLTIuMjAxOCwwLjA1NiAtNS4yNjYsLTAuNDIgLTYuMTQyMiwtMi40NCAtMC43ODQ4LC0xLjgxIDIuMTY3NiwtNS41MDggMi4xNjc2LC01LjUwOCB6Ii8+CiA8cGF0aCBzdHlsZT0iZmlsbDojZDI0NjQ2IiBkPSJtIDQ2LDIzIGEgNSw1IDAgMCAxIC0xMCwwIDUsNSAwIDEgMSAxMCwwIHoiLz4KIDxwYXRoIHN0eWxlPSJmaWxsOiM2MDYwNjAiIGQ9Im0gMzIsMzkuMDAyIGEgMywzIDAgMCAxIC02LDAgMywzIDAgMSAxIDYsMCB6Ii8+Cjwvc3ZnPgo=
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling MKVToolNix..."
    if apt-get remove -y mkvtoolnix; then
        echo "MKVToolNix successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if MKVToolNix is installed
if command -v mkvmerge &>/dev/null; then
    echo "MKVToolNix is already installed."
    exit 0
fi

echo "MKVToolNix is not installed. Installing..."

# Update package lists and install MKVToolNix
if ! apt-get -qq update || ! apt-get install -yqq mkvtoolnix; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v mkvmerge &>/dev/null; then
    echo "MKVToolNix successfully installed."
    exit 0
fi

echo "Failed to install MKVToolNix."
exit 1
