#!/usr/bin/env pwsh

Function InstallAsTool
{
    Param(
        [string]
        [ValidateNotNullOrEmpty()]
        $APP_NAME
    )

    Write-Output "Installing '$APP_NAME' as tool..."

    $ARTIFACTS_DIR = "artifacts/$APP_NAME"
    $BUILD_CONFIGURATION = "Release"
    $EXE_PROJECT = "src/$APP_NAME/src/app/$APP_NAME.csproj"

    Write-Output "Cleaning..."
    $COMMAND="dotnet clean -c $BUILD_CONFIGURATION; Remove-Item -Recurse -ErrorAction Ignore $ARTIFACTS_DIR"
    Write-Output $COMMAND
    Invoke-Expression -Command $COMMAND

    Write-Output "Packing..."
    $COMMAND="dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $EXE_PROJECT"
    Write-Output $COMMAND
    Invoke-Expression -Command $COMMAND

    If ($LASTEXITCODE -ne 0)
    {
        Write-Output "$APP_NAME will not be installed/upgraded."
        Exit
    }

    Write-Output "Updating tool..."
    $COMMAND="dotnet tool update --global --add-source $ARTIFACTS_DIR $APP_NAME"
    Write-Output $COMMAND
    Invoke-Expression -Command $COMMAND
}

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

InstallAsTool "PortToTripleSlash"
InstallAsTool "PortToDocs"