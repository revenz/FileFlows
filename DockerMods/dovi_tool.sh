# ----------------------------------------------------------------------------------------------------
# Name: dovi_tool
# Description: dovi_tool is a CLI tool combining multiple utilities for working with Dolby Vision.
# Revision: 6
# Icon: data:image/svg+xml;base64,PCFET0NUWVBFIHN2ZyBQVUJMSUMgIi0vL1czQy8vRFREIFNWRyAxLjEvL0VOIiAiaHR0cDovL3d3dy53My5vcmcvR3JhcGhpY3MvU1ZHLzEuMS9EVEQvc3ZnMTEuZHRkIj4KDTwhLS0gVXBsb2FkZWQgdG86IFNWRyBSZXBvLCB3d3cuc3ZncmVwby5jb20sIFRyYW5zZm9ybWVkIGJ5OiBTVkcgUmVwbyBNaXhlciBUb29scyAtLT4KPHN2ZyBmaWxsPSIjZmZmZmZmIiB3aWR0aD0iODAwcHgiIGhlaWdodD0iODAwcHgiIHZpZXdCb3g9IjAgMCAyNCAyNCIgcm9sZT0iaW1nIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHN0cm9rZT0iI2ZmZmZmZiI+Cg08ZyBpZD0iU1ZHUmVwb19iZ0NhcnJpZXIiIHN0cm9rZS13aWR0aD0iMCIvPgoNPGcgaWQ9IlNWR1JlcG9fdHJhY2VyQ2FycmllciIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIi8+Cg08ZyBpZD0iU1ZHUmVwb19pY29uQ2FycmllciI+Cg08cGF0aCBkPSJNMjQgMjAuMzUyVjMuNjQ4SDB2MTYuNzA0aDI0ek0xOC40MzMgNS44MDZoMi43MzZ2MTIuMzg3aC0yLjczNmMtMi44MzkgMC01LjIxNC0yLjc2Ny01LjIxNC02LjE5NHMyLjM3NS02LjE5MyA1LjIxNC02LjE5M3ptLTE1LjYwMiAwaDIuNzM2YzIuODM5IDAgNS4yMTQgMi43NjcgNS4yMTQgNi4xOTRzLTIuMzc0IDYuMTk0LTUuMjE0IDYuMTk0SDIuODMxVjUuODA2eiIvPgoNPC9nPgoNPC9zdmc+
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling..."
    rm -f /bin/dovi_tool
    exit 0
fi

# Check if rar is installed
if command -v dovi_tool &>/dev/null; then
    echo "already installed."
    exit 0
fi

wget -O /tmp/dovi_tool.tar.gz https://github.com/quietvoid/dovi_tool/releases/download/2.1.3/dovi_tool-2.1.3-x86_64-unknown-linux-musl.tar.gz
tar xvf /tmp/dovi_tool.tar.gz
mv dovi_tool /bin
rm /tmp/dovi_tool.tar.gz

# Verify installation
if command -v dovi_tool &>/dev/null; then
    echo "successfully installed."
    exit 0
fi

echo "Failed to install."
exit 1
