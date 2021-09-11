#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
Push-Location $(Split-Path $MyInvocation.MyCommand.Path)

$ARTIFACTS_DIR = "artifacts"
$BUILD_CONFIGURATION = "Release"
$APP_NAME = "DocsPortingTool"
$EXE_PROJECT = "Program"

Write-Output "Building $APP_NAME..."
dotnet pack -c $BUILD_CONFIGURATION -o $ARTIFACTS_DIR $EXE_PROJECT

# Prevent uninstalling an existing tool below if the build fails
If ($LASTEXITCODE -ne 0)
{
	Write-Output "$APP_NAME will not be installed/upgraded."
	Exit
}

$toolList = dotnet tool list --global
# Output example when there are tools installed:

#   Package Id                 Version                 Commands
#   ------------------------------------------------------------------
#   docsportingtool            3.0.0                   DocsPortingTool
#   microsoft.dotnet.darc      1.1.0-beta.21458.1      darc

# When there are no tools installed, the output only contains:

#   Package Id                 Version                 Commands
#   ------------------------------------------------------------------

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

Write-Output "Installing $APP_NAME as a global tool..."
dotnet tool install --global --add-source $ARTIFACTS_DIR $APP_NAME
