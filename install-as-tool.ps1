#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$BUILD_CONFIGURATION = "Release"

dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR "Program"
dotnet tool update --global --add-source $ARTIFACTS_DIR "DocsPortingTool"
