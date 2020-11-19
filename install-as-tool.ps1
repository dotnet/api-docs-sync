#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$PROJECT_NAME = "Program"
$BUILD_CONFIGURATION = "Release"

dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $PROJECT_NAME
dotnet tool update --global --add-source $ARTIFACTS_DIR $PROJECT_NAME
