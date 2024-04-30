# ----------------------------------------------------------------------------------------------------
# Name: .NET 7 SDK
# Description: .NET 7 SDK is a development kit that provides tools and libraries for building and running .NET 7 applications.
# Revision: 1
# Icon: data:image/svg+xml;base64,PHN2ZyB2ZXJzaW9uPSIxLjIiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyIgdmlld0JveD0iMCAwIDQ1NiA0NTYiIHdpZHRoPSI0NTYiIGhlaWdodD0iNDU2Ij48c3R5bGU+LmF7ZmlsbDojNTEyYmQ0fS5ie2ZpbGw6IzI3MTU2NX0uY3tmaWxsOiNmZmZ9PC9zdHlsZT48cGF0aCBjbGFzcz0iYSIgZD0ibTAgMGg0NTZ2NDU2aC00NTZ6Ii8+PHBhdGggY2xhc3M9ImIiIGQ9Im0yMTIuMyAzOTBoLTU2LjRsMTI4LjQtMjc4LjRoLTE2OC45di00NmgyMjQuN3YzNi41eiIvPjxwYXRoIGNsYXNzPSJjIiBkPSJtNzcuMyAyODguM3EtNC45IDAtOC4yLTMuMi0zLjQtMy4zLTMuNC03LjggMC00LjcgMy40LTggMy4zLTMuMyA4LjItMy4zIDQuOSAwIDguMyAzLjMgMy40IDMuMyAzLjQgOCAwIDQuNS0zLjQgNy44LTMuNCAzLjItOC4zIDMuMnoiLz48cGF0aCBjbGFzcz0iYyIgZD0ibTIwNi4yIDI4Ni41aC0yMWwtNTUuMi04Ny4xcS0yLjEtMy4zLTMuNS02LjloLTAuNXEwLjcgMy44IDAuNyAxNi4zdjc3LjdoLTE4LjZ2LTExOC41aDIyLjRsNTMuMyA4NXEzLjQgNS4zIDQuNCA3LjNoMC4zcS0wLjgtNC43LTAuOC0xNS45di03Ni40aDE4LjV6Ii8+PHBhdGggY2xhc3M9ImMiIGQ9Im0yOTYuNCAyODYuNWgtNjQuOHYtMTE4LjVoNjIuM3YxNi43aC00My4ydjMzLjVoMzkuOHYxNi43aC0zOS44djM1aDQ1Ljd6Ii8+PHBhdGggY2xhc3M9ImMiIGQ9Im0zODguNyAxODQuN2gtMzMuMnYxMDEuOGgtMTkuMnYtMTAxLjhoLTMzLjJ2LTE2LjdoODUuNnoiLz48L3N2Zz4=
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if .NET SDK 7 is installed
if ! dotnet --list-sdks | grep -q "7.0"; then
    echo ".NET SDK 7 is not installed. Installing..."

    # Download dotnet-install.sh
    wget -q https://dot.net/v1/dotnet-install.sh

    # Install .NET SDK 7.0 to /dotnet directory
    bash dotnet-install.sh -c 7.0 --install-dir /dotnet

    # Clean up
    rm -f dotnet-install.sh

    echo "Installation complete."
else
    echo ".NET SDK 7 is already installed."
fi

