#!/usr/bin/env bash
set -e

if [ "$1" = '--help' ]; then
    printf "Usage: [OPTIONS]\n"
    printf "  --help              Show this help message and exit\n"
    printf "  --server            Enable server mode\n"
    printf "  --node              Enable node mode\n"
elif [ "$1" = '--server' ]; then
    printf "Launching server\n"
    exec dotnet /app/FileFlows.Server.dll --urls=http://*:5000 --docker "$@"
elif [ "$1" = '--node' ]; then
    printf "Launching node\n"
    exec dotnet /app/FileFlows.Node.dll "$@"
fi

# Allow user to specify custom entrypoint
exec "$@"
