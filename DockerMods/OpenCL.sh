# ----------------------------------------------------------------------------------------------------
# Name: OpenCL
# Description: This will upgrade your OpenCL version if you have Intel driver installed.
# Author: Idan Bush
# Revision: 1
# Icon: data:image/png;base64,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
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Function to handle errors
function handle_error {
    echo "An error occurred. Exiting..."
    exit 1
}

# Check if the --uninstall option is provided
if [ "$1" == "--uninstall" ]; then
    echo "Uninstalling OpenCL..."
    if apt-get remove -y intel-media-va-driver-non-free intel-opencl-icd; then
        echo "Intel OpenCL successfully uninstalled."
        exit 0
    else
        handle_error
    fi
fi

# Fetch the package information
output=$(apt-cache policy intel-opencl-icd)

# Extract the candidate version number using grep and awk
candidate_version=$(echo "$output" | grep 'Candidate:' | awk '{print $2}')

# Check if the candidate version exists
if [ -z "$candidate_version" ] || [ "$candidate_version" = "(none)" ]; then
    echo "No candidate version available, existing..."
    exit 0
fi

# Extract the major version (the first number before the dot)
major_version=$(echo "$candidate_version" | cut -d '.' -f 1)

# Compare if the major version is greater than 22
if [ "$major_version" -ge 22 ]; then
    echo "The candidate major version ($major_version) is equal to or higher than 22."
    echo "Upgrading the package..."
    if ! apt update || ! apt install -y intel-media-va-driver-non-free intel-opencl-icd; then
        handle_error
    fi
else
    echo "The candidate major version ($major_version) is lower than 22. No upgrade performed."
    exit 0
fi

# Verify OpenCL installed version
installed_version=$(apt-cache policy intel-opencl-icd | grep 'Installed:' | awk '{print $2}')

# Check if the package was successfully installed
if [ "$installed_version" = "(none)" ]; then
    echo "Installation failed or package is not installed."
    handle_error
else
    echo "Successfully installed intel-opencl-icd version: $installed_version"
    exit 0
fi

echo "Failed to install Intel VPL."
exit 1
