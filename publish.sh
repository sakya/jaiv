#!/bin/bash
dotnet clean ImageViewer.sln -c Release
dotnet publish ImageViewer.sln -c Release --runtime linux-x64 -p:PublishReadyToRun=true --self-contained --output ./dist/linux-x64
