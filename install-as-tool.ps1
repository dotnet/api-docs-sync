#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$PROJECT_PATH = "Program"
$NUGET_PACKAGE_ID = "DocsPortingTool"
$BUILD_CONFIGURATION = "Release"

dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $PROJECT_PATH
dotnet tool update --global --add-source $ARTIFACTS_DIR $NUGET_PACKAGE_ID
