#!/bin/bash

rm -rf test-results > /dev/null 2>&1 &
rm -rf logs > /dev/null 2>&1 &

echo "Current directory: $(pwd)"
echo "Relative directory: $FF_RELATIVE"
echo "Host directory Logs: $FF_TEMP_PATH$(pwd)/logs"
echo "Host directory test-results: $FF_TEMP_PATH$(pwd)/test-results"
echo "FF Host Output / log: $FF_HOST_OUTPUT$FF_RELATIVE/logs:/app/FileFlows/Logs"
echo "FF Host Output / test-results: $FF_HOST_OUTPUT$FF_RELATIVE/test-results:/app/test-results"
mkdir -p "$(pwd)/logs"
mkdir -p "$(pwd)/test-results"

# Build the Docker image
echo Building Docker Image
docker build -f Dockerfile -t fileflows-autotests --build-arg TZ=Pacific/Auckland .

# Check for --all argument
RunAllTestsEnv=""
for arg in "$@"; do
    if [ "$arg" == "--all" ]; then
        RunAllTestsEnv="-e RunAllTests=1"
        break
    fi
done

# Run the container
echo Running Docker image
docker run --rm \
    -p 19222:5276 \
    -v "$FF_HOST_OUTPUT$FF_RELATIVE/logs:/app/FileFlows/Logs" \
    -v "$FF_HOST_OUTPUT$FF_RELATIVE/test-results:/app/test-results" \
    -v "/appdata/tools:/tools" \
    -e FF_TEMP_PATH=/app/test-results \
    -e FF_LICENSE_EMAIL=$FF_LICENSE_EMAIL \
    -e FF_LICENSE_KEY=$FF_LICENSE_KEY \
    -e KEEP_PASSED_VIDEOS=$KEEP_PASSED_VIDEOS \
    -e FFURL=$FF_URL \
    -e DOCKER=1 \
    $RunAllTestsEnv \
    fileflows-autotests
