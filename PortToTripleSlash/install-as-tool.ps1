#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$BUILD_CONFIGURATION = "Release"
$APP_NAME = "PortToTripleSlash"
$EXE_PROJECT = "PortToTripleSlash"

dotnet clean -c $BUILD_CONFIGURATION; remove-item -Recurse -ErrorAction Ignore $ARTIFACTS_DIR
dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $EXE_PROJECT

If ($LASTEXITCODE -ne 0)
{
	Write-Output "$APP_NAME will not be installed/upgraded."
	Exit
}

dotnet tool update --global --add-source $ARTIFACTS_DIR $APP_NAME
