#!/bin/sh

# Download dotnet-install script
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh

# Install .NET 9.0.305 SDK locally
./dotnet-install.sh -Version 9.0.305 -InstallDir ./dotnet

# Verify installation
./dotnet/dotnet --version

# Publish your Blazor WASM app
./dotnet/dotnet publish -c Release -o output
