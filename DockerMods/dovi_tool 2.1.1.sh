# ----------------------------------------------------------------------------------------------------
# Name: dovi_tool
# Description: dovi_tool is a CLI tool combining multiple utilities for working with Dolby Vision.
# Revision: 3
# Icon: fas fa-file-video
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

wget -O /tmp/dovi_tool.tar.gz https://github.com/quietvoid/dovi_tool/releases/download/2.1.1/dovi_tool-2.1.1-x86_64-unknown-linux-musl.tar.gz
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
