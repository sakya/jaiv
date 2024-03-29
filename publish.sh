#!/bin/bash
rm -rf ./dist
dotnet clean ImageViewer.sln -c Release
dotnet publish ImageViewer/ImageViewer.csproj -c Release --runtime linux-x64 -p:PublishReadyToRun=true --self-contained --output ./dist/linux-x64
