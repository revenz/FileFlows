# ----------------------------------------------------------------------------------------------------
# Name: rar
# Description: RAR is an archive file format used for data compression, while UNRAR is a utility to extract content from RAR archives.
# Author: reven
# Revision: 4
# Default: true
# Icon: data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iaXNvLTg4NTktMSI/Pg0KPCEtLSBVcGxvYWRlZCB0bzogU1ZHIFJlcG8sIHd3dy5zdmdyZXBvLmNvbSwgR2VuZXJhdG9yOiBTVkcgUmVwbyBNaXhlciBUb29scyAtLT4NCjxzdmcgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgDQoJIHZpZXdCb3g9IjAgMCA1MTIgNTEyIiB4bWw6c3BhY2U9InByZXNlcnZlIj4NCjxwYXRoIHN0eWxlPSJmaWxsOiNCQzVFQjA7IiBkPSJNNTExLjM0NCwyNzQuMjY2QzUxMS43NywyNjguMjMxLDUxMiwyNjIuMTQzLDUxMiwyNTZDNTEyLDExNC42MTUsMzk3LjM4NSwwLDI1NiwwUzAsMTE0LjYxNSwwLDI1Ng0KCWMwLDExNy43NjksNzkuNTMsMjE2Ljk0OSwxODcuODA5LDI0Ni44MDFMNTExLjM0NCwyNzQuMjY2eiIvPg0KPHBhdGggc3R5bGU9ImZpbGw6I0FBMzM5OTsiIGQ9Ik01MTEuMzQ0LDI3NC4yNjZMMzE0Ljk5MSw3Ny45MTNMMTE5LjA5Niw0MzQuMDg3bDY4LjcxNCw2OC43MTRDMjA5LjUyMiw1MDguNzg3LDIzMi4zODUsNTEyLDI1Niw1MTINCglDMzkxLjI0Myw1MTIsNTAxLjk3Niw0MDcuMTI1LDUxMS4zNDQsMjc0LjI2NnoiLz4NCjxwb2x5Z29uIHN0eWxlPSJmaWxsOiNGRkZGRkY7IiBwb2ludHM9IjI3OC4zMjgsMzMzLjkxMyAyNTUuNzExLDc3LjkxMyAxMTkuMDk2LDc3LjkxMyAxMTkuMDk2LDMxMS42NTIgIi8+DQo8cG9seWdvbiBzdHlsZT0iZmlsbDojRThFNkU2OyIgcG9pbnRzPSIzOTIuOTA0LDMxMS42NTIgMzkyLjkwNCwxNTUuODI2IDMzNy4yNTIsMTMzLjU2NSAzMTQuOTkxLDc3LjkxMyAyNTUuNzExLDc3LjkxMyANCgkyNTYuMDY3LDMzMy45MTMgIi8+DQo8cG9seWdvbiBzdHlsZT0iZmlsbDojRkZGRkZGOyIgcG9pbnRzPSIzMTQuOTkxLDE1NS44MjYgMzE0Ljk5MSw3Ny45MTMgMzkyLjkwNCwxNTUuODI2ICIvPg0KPHJlY3QgeD0iMTE5LjA5NiIgeT0iMzExLjY1MiIgc3R5bGU9ImZpbGw6IzYxMDM1MzsiIHdpZHRoPSIyNzMuODA5IiBoZWlnaHQ9IjEyMi40MzUiLz4NCjxnPg0KCTxwYXRoIHN0eWxlPSJmaWxsOiNGRkZGRkY7IiBkPSJNMTk5LjUzNSwzODQuNDUzaC0wLjM3OEgxODguOTR2MTQuOTA5aC0xMy40NzF2LTUyLjk3NWgyMy42ODdjMTQuMDAxLDAsMjIuMDIzLDYuNjU5LDIyLjAyMywxOC40NjUNCgkJYzAsOC4wOTctMy40MDYsMTMuOTI1LTkuNjExLDE3LjAyN2wxMS4xMjUsMTcuNDgzaC0xNS4yODdMMTk5LjUzNSwzODQuNDUzeiBNMTk5LjE1NywzNzMuODU4YzUuODI4LDAsOS4yMzMtMi45NTIsOS4yMzMtOC41NTINCgkJYzAtNS41MjUtMy40MDUtOC4zMjQtOS4yMzMtOC4zMjRIMTg4Ljk0djE2Ljg3N2gxMC4yMTdWMzczLjg1OHoiLz4NCgk8cGF0aCBzdHlsZT0iZmlsbDojRkZGRkZGOyIgZD0iTTI0NC4xOTIsMzg5LjZsLTMuODU5LDkuNzYzaC0xMy44NTFsMjIuODU1LTUyLjk3NWgxMy44NDhsMjIuMzI3LDUyLjk3NWgtMTQuMzc5bC0zLjc4My05Ljc2Mw0KCQlIMjQ0LjE5MnogTTI1NS44NDYsMzU5Ljc4MWwtNy43MiwxOS42MDFoMTUuMjg4TDI1NS44NDYsMzU5Ljc4MXoiLz4NCgk8cGF0aCBzdHlsZT0iZmlsbDojRkZGRkZGOyIgZD0iTTMxNi4zOTYsMzg0LjQ1M2gtMC4zNzhIMzA1Ljh2MTQuOTA5aC0xMy40N3YtNTIuOTc1aDIzLjY4OGMxNCwwLDIyLjAyMiw2LjY1OSwyMi4wMjIsMTguNDY1DQoJCWMwLDguMDk3LTMuNDA2LDEzLjkyNS05LjYxMSwxNy4wMjdsMTEuMTI1LDE3LjQ4M2gtMTUuMjg4TDMxNi4zOTYsMzg0LjQ1M3ogTTMxNi4wMTgsMzczLjg1OGM1LjgyNiwwLDkuMjMzLTIuOTUyLDkuMjMzLTguNTUyDQoJCWMwLTUuNTI1LTMuNDA3LTguMzI0LTkuMjMzLTguMzI0SDMwNS44djE2Ljg3N2gxMC4yMThWMzczLjg1OHoiLz4NCjwvZz4NCjwvc3ZnPg==
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling Rar..."
    if apt-get remove -y rar unrar; then
        echo "Rar successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi


# Check if rar is installed
if command -v unrar &>/dev/null; then
    echo "rar is already installed."
    exit 0
fi

echo "rar is not installed. Installing..."

# Update package lists and install rar
if ! apt-get -qq update || ! apt-get install -yqq rar unrar; then
    handle_error
fi

echo "Installation complete."

# Verify installation
if command -v unrar &>/dev/null; then
    echo "rar successfully installed."
    exit 0
fi

echo "Failed to install rar."
exit 1
