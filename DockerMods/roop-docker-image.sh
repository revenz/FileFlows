# ----------------------------------------------------------------------------------------------------
# Name: roop-docker-image
# Description: Creates a Docker image `roop` that can be deployed in a flow to swap faces in images or videos.
# Author: reven
# Revision: 2
# Icon: fas fa-smile:#1178A0
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Set variables
IMAGE_NAME="roop"      # Name of the Docker image to build

# Function to check if the image exists
image_exists() {
    if [[ "$(docker images -q ${IMAGE_NAME})" ]]; then
        return 0 # Image exists
    else
        return 1 # Image does not exist
    fi
}

# Check if "--uninstall" flag is passed
if [[ "$1" == "--uninstall" ]]; then
    echo "Uninstall flag detected. Removing Docker image '${IMAGE_NAME}'..."

    if image_exists; then
        docker rmi -f ${IMAGE_NAME}
        echo "Docker image '${IMAGE_NAME}' has been removed."
    else
        echo "Docker image '${IMAGE_NAME}' does not exist."
    fi
    exit 0
fi


# Build Dockerfile if image does not exist
if image_exists; then
    echo "Image '${IMAGE_NAME}' already exists."
    exit 0
fi

echo "Image '${IMAGE_NAME}' does not exist. Creating Docker image..."

# Create a Dockerfile
cat > RoopDockerfile << EOL
FROM python:3.10
RUN curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey |\
gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg &&\
curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list |\
sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' |\
tee /etc/apt/sources.list.d/nvidia-container-toolkit.list
RUN apt-get -qq update &&\
apt-get install -yqq \
    ffmpeg \
    wget \
    unzip \
    nvidia-container-toolkit


# Clone the specific branch of the repository
RUN git clone --branch tkinter https://github.com/C0untFloyd/roop-unleashed.git /roop

WORKDIR /roop
RUN pip install -r requirements.txt
RUN pip install onnxruntime-gpu==1.15.1
ENV NVIDIA_VISIBLE_DEVICES=all
ENV NVIDIA_DRIVER_CAPABILITIES=all
ENTRYPOINT ["python", "run.py"]
EOL

# Build the Docker image using RoopDockerfile
docker build -f RoopDockerfile -t ${IMAGE_NAME} .

# Delete the Dockerfile after building the image
rm -f RoopDockerfile

echo "Docker image '${IMAGE_NAME}' built"