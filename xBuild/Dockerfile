FROM ubuntu:25.04 AS base

ARG DEBIAN_FRONTEND=noninteractive
ENV PATH=/dotnet:/dotnet/tools:$PATH
ENV DOTNET_ROOT=/dotnet

ENV NVIDIA_DRIVER_CAPABILITIES="compute,video,utility" \
    NVIDIA_VISIBLE_DEVICES="all" \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN apt-get update && \
    apt-get install -y sudo tzdata wget ca-certificates gnupg curl tar xz-utils libssl-dev apt-transport-https openssl locales libfontconfig1 libfreetype6 pciutils vainfo git

# Install dotnet SDK
RUN wget https://dot.net/v1/dotnet-install.sh  && \
    bash dotnet-install.sh -c 8.0 --install-dir /dotnet && \
    rm -f dotnet-install.sh && \
    chmod 775 /dotnet 

# Install Docker
RUN curl -fsSL https://get.docker.com | sh
    
##########################################
### actual FileFlows stuff happens now ###
##########################################

COPY /deploy /app

RUN chmod +x /app/docker-entrypoint.sh

# expose the ports we need
EXPOSE 5000

# set the working directory
WORKDIR /app

ENTRYPOINT ["/app/docker-entrypoint.sh"]