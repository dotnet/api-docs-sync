#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$BUILD_CONFIGURATION = "Release"
$APP_NAME = "DocsPortingTool"
$EXE_PROJECT = "Program"

$toolList = dotnet tool list --global
If ($toolList.Length -gt 2)
{
	For ($i = 2; $i -lt $toolList.Length; $i++)
	{
		If ($toolList[$i].ToLowerInvariant().Contains($APP_NAME.ToLowerInvariant()))
		{
			Write-Output "Uninstalling existing $APP_NAME instance..."
			dotnet tool uninstall --global $APP_NAME
			Break;
		}
	}
}

Write-Output "Building $APP_NAME..."
dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $EXE_PROJECT

Write-Output "Installing $APP_NAME as a global tool..."
dotnet tool install --global --add-source $ARTIFACTS_DIR $APP_NAME
