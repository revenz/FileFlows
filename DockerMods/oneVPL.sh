# ----------------------------------------------------------------------------------------------------
# Name: oneVPL
# Description: This is only required if you have the onevpl Intel driver installed, most systems do not.
# Author: reven / lawrence
# Revision: 7
# Icon: data:image/svg+xml;base64,PHN2ZyB2ZXJzaW9uPSIxLjIiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgdmlld0JveD0iMCAwIDQ1MCA0NTAiIHdpZHRoPSI0NTAiIGhlaWdodD0iNDUwIj48c3R5bGU+LmF7ZmlsbDojMDA3MWM1fS5ie2ZpbGw6I2ZmZn08L3N0eWxlPjxwYXRoIGZpbGwtcnVsZT0iZXZlbm9kZCIgY2xhc3M9ImEiIGQ9Im0wIDY0LjNjMC0zNS41IDI4LjgtNjQuMyA2NC4zLTY0LjNoMzIxLjRjMzUuNSAwIDY0LjMgMjguOCA2NC4zIDY0LjN2MzIxLjRjMCAzNS41LTI4LjggNjQuMy02NC4zIDY0LjNoLTMyMS40Yy0zNS41IDAtNjQuMy0yOC44LTY0LjMtNjQuM3oiLz48cGF0aCBjbGFzcz0iYiIgZD0ibTMxLjcgMTUyLjJoMjguMXYyOC4xaC0yOC4xeiIvPjxwYXRoIGZpbGwtcnVsZT0iZXZlbm9kZCIgY2xhc3M9ImIiIGQ9Im01OS4xIDI5OC41aC0yNi43di0xMDEuMmgyNi43em0xNzMuMyAxLjFjLTguMSAwLTE0LjktMC43LTIwLjMtMi4yLTUuMy0xLjUtOS43LTMuOS0xMy03LjMtMy4zLTMuNC01LjctNy45LTcuMS0xMy40LTEuNC01LjYtMi4xLTEyLjYtMi4xLTIwLjl2LTk3LjloMjYuN3YzOS40aDE5LjN2MjIuOWgtMTkuM3YzNS4zYzAgNCAwLjIgNy4zIDAuNiA5LjggMC41IDIuNyAxLjQgNC43IDIuOCA2LjEgMS40IDEuNCAzLjUgMi40IDYuMyAyLjggMi40IDAuNCA1LjcgMC42IDkuNiAwLjZ2MjQuOHptMTI5LjYtMXYtMTQ4LjVoMjYuN3YxNDguNXptLTE4Ni43LTU3LjV2NTcuNGgtMjYuOXYtNTMuOGMwLTguMy0yLTE0LjgtNS44LTE5LjQtMy45LTQuNi05LjgtNy0xNy41LTctNi41IDAtMTIuMiAyLjQtMTYuOSA3LjItNC40IDQuNS02LjcgMTEuNS03IDIxdjUyaC0yNi41di0xMDEuMmgyNi4zdjE0LjZsMS40LTEuOWMzLjYtNC42IDgtOC4yIDEzLjMtMTAuOCA1LjItMi42IDExLjEtMy45IDE3LjUtMy45IDEzLjIgMCAyMy42IDQgMzEgMTIgNy40IDggMTEuMiAxOS40IDExLjEgMzMuOHptMTcyLjkgMTYuMmgtNzkuNWwwLjMgMC45YzEuNyA1LjkgNC45IDEwLjcgOS42IDE0LjEgNC43IDMuNSAxMC45IDUuMiAxOC40IDUuMiAxMiAwIDIxLjYtNi41IDI1LjYtMTEuM2wxOS4yIDE0LjZjLTguNCA5LjYtMjIuNCAxOS45LTQ1IDE5LjktNy44IDAtMTUuMS0xLjQtMjEuNy00LjItNi42LTIuNy0xMi4zLTYuNS0xNi45LTExLjItNC42LTQuNy04LjMtMTAuMy0xMC45LTE2LjctMi42LTYuNC0zLjktMTMuMy0zLjktMjAuNiAwLTcuMyAxLjMtMTQuMiA0LjEtMjAuNiAyLjctNi40IDYuNS0xMiAxMS4yLTE2LjcgNC43LTQuNyAxMC4zLTguNCAxNi43LTExLjIgNi40LTIuNyAxMy4zLTQuMSAyMC42LTQuMSA3LjcgMCAxNC44IDEuMyAyMS4yIDQgNi40IDIuNyAxMS45IDYuNCAxNi41IDExLjIgNC41IDQuOCA4LjEgMTAuNCAxMC43IDE2LjcgMi41IDYuNCAzLjggMTMuMiAzLjggMjAuNXptLTI1LjYtMTguOWMwLTcuNC04LjYtMjAuMy0yNi45LTIwLjMtMTguMyAwLjEtMjYuOCAxMy0yNi44IDIwLjR6bTk3LjMgNTEuMmMwIDEuMy0wLjMgMi42LTAuOCAzLjgtMC41IDEuMi0xLjIgMi4yLTIuMSAzLjEtMC45IDAuOS0xLjkgMS42LTMuMSAyLjEtMS4yIDAuNS0yLjQgMC44LTMuOCAwLjgtMS4zIDAtMi42LTAuMy0zLjgtMC44LTEuMi0wLjUtMi4yLTEuMi0zLjEtMi4xLTAuOS0wLjktMS42LTEuOS0yLjEtMy4xLTAuNS0xLjItMC44LTIuNC0wLjgtMy44IDAtMS4zIDAuMy0yLjYgMC44LTMuOCAwLjUtMS4yIDEuMi0yLjIgMi4xLTMuMSAwLjktMC45IDEuOS0xLjYgMy4xLTIuMSAxLjItMC41IDIuNC0wLjggMy44LTAuOCAxLjMgMCAyLjYgMC4zIDMuOCAwLjggMS4yIDAuNSAyLjIgMS4yIDMuMSAyLjEgMC45IDAuOSAxLjYgMS45IDIuMSAzLjEgMC41IDEuMiAwLjggMi41IDAuOCAzLjh6bS0xLjggMGMwLTEuMi0wLjItMi4yLTAuNi0zLjItMC40LTEtMS0xLjktMS43LTIuNi0wLjctMC43LTEuNi0xLjMtMi42LTEuNy0xLTAuNC0yLjEtMC42LTMuMi0wLjYtMS4yIDAtMi4yIDAuMi0zLjIgMC42LTEgMC40LTEuOSAxLTIuNiAxLjctMC43IDAuNy0xLjMgMS42LTEuNyAyLjYtMC40IDEtMC42IDIuMS0wLjYgMy4yIDAgMS4yIDAuMiAyLjIgMC42IDMuMiAwLjQgMSAxIDEuOSAxLjcgMi42IDAuNyAwLjcgMS42IDEuMyAyLjYgMS43IDEgMC40IDIuMSAwLjYgMy4yIDAuNiAxLjIgMCAyLjItMC4yIDMuMi0wLjYgMS0wLjQgMS45LTEgMi42LTEuNyAwLjctMC43IDEuMy0xLjYgMS43LTIuNiAwLjQtMSAwLjYtMiAwLjYtMy4yem0tMy4zIDUuN2gtMi4ybC0yLjgtNC42aC0xLjV2NC42aC0yLjF2LTExLjNoNC40YzEuMyAwIDIuNCAwLjMgMyAwLjkgMC43IDAuNiAxIDEuNCAxIDIuNSAwIDEtMC4zIDEuNy0wLjggMi4yLTAuNSAwLjUtMS4xIDAuOC0xLjkgMC45em0tMy4xLTYuN2MwLjItMC4xIDAuNC0wLjMgMC42LTAuNSAwLjEtMC4yIDAuMi0wLjUgMC4yLTAuOSAwLTAuNCAwLTAuNy0wLjItMC45LTAuMS0wLjItMC4zLTAuNC0wLjYtMC41LTAuMy0wLjEtMC42LTAuMi0wLjktMC4yLTAuNCAwLTIuMiAwLTIuNSAwdjMuMmMwLjQgMCAyLjIgMCAyLjUgMCAwLjMgMCAwLjYtMC4xIDAuOS0wLjJ6Ii8+PC9zdmc+
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling Intel VPL..."
    if apt-get remove -y libmfx-gen1.2 libmfx-dev i965-va-driver-shaders intel-media-va-driver-non-free intel-opencl-icd; then
        echo "Intel VPL successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Check if Intel VPL is installed
if ls /usr/share/doc/libmfx-gen1.2/copyright &>/dev/null; then
    echo "Intel VPL is already installed."
    exit 0
fi

echo "Intel VPL is not installed. Installing..."

# Update package lists and install Intel VPL
if ! apt-get -qq update || ! apt-get install -yqq libmfx-gen1.2 libmfx-dev i965-va-driver-shaders intel-media-va-driver-non-free intel-opencl-icd; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if ls /usr/share/doc/libmfx-gen1.2/copyright &>/dev/null; then
    echo "Intel VPL successfully installed."
    exit 0
fi

echo "Failed to install Intel VPL."
exit 1
