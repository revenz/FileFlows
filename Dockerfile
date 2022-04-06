FROM ubuntu:22.04 AS base

ARG DEBIAN_FRONTEND=noninteractive
ENV PATH=$PATH:/root/.dotnet:/root/.dotnet/tools
ENV DOTNET_ROOT=/root/.dotnet

# Add intel hardware encoding support
ENV LIBVA_DRIVERS_PATH="/usr/lib/x86_64-linux-gnu/dri" \
    LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu" \
    NVIDIA_DRIVER_CAPABILITIES="compute,video,utility" \
    NVIDIA_VISIBLE_DEVICES="all" \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

ARG DEPS="git wget dos2unix software-properties-common libssl-dev comskip"
ARG VAAPI_DEPS="vainfo intel-media-va-driver-non-free libva-dev libmfx-dev intel-media-va-driver-non-free intel-media-va-driver-non-free i965-va-driver-shaders mesa-va-drivers"
RUN apt-get update && \
    ARCH=$(dpkg --print-architecture) && \
    if [ $ARCH -eq 'amd64' ]; \
    then apt-get install -y ${DEPS} ${VAAPI_DEPS}; \
    else apt-get install -y ${DEPS}; \
    fi && \
    add-apt-repository universe && \
    rm -rf /var/lib/apt/lists/*


# Install Dotnet runtime
RUN wget https://dot.net/v1/dotnet-install.sh  && \
    bash dotnet-install.sh -c Current --runtime aspnetcore && \
    rm -f dotnet-install.sh

# Install ffmpeg from jellyfin
ARG FFMPEG_URL="https://github.com/jellyfin/jellyfin-ffmpeg/releases/download/v4.4.1-4/jellyfin-ffmpeg_4.4.1-4-jammy"
RUN ARCH=$(dpkg --print-architecture) && \
    wget "${FFMPEG_URL}_${ARCH}.deb" && \
    apt-get update && \
    apt-get install -y ./jellyfin-ffmpeg*.deb && \
    rm -rf ./jellyfin-ffmpeg*.deb && \
    ln /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg && \
    rm -rf /var/lib/apt/lists/* && \
    ffmpeg --help



FROM base AS builder

# Install dotnet SDK
RUN wget https://dot.net/v1/dotnet-install.sh  && \
    bash dotnet-install.sh -c Current && \
    rm -f dotnet-install.sh

# Install powershell
#RUN dotnet tool install --global PowerShell 

# Copy FileFlows
COPY deploy/ /app/
WORKDIR /app
RUN ls /app/FileFlows*.tar.gz | xargs -n1 tar -xzvf && \
    rm -rf /app/FileFlows*.tar.gz /app/FileFlows*.msi



FROM base

# expose the ports we need
EXPOSE 5000

# copy the deploy file into the app directory
COPY --from=builder /app /app

# set the working directory
WORKDIR /app

COPY docker-entrypoint.sh /

# dos2unix to remove invalid linux characters from the script
RUN dos2unix /docker-entrypoint.sh && \
    chmod +x /docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["--help"]
