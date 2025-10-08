# PowerShell script to generate GitHub issue markdown files for undocumented .NET APIs
# Processes Excel data and creates individual namespace issues plus a main tracking issue
#
# Features:
# - Reads undocumented API data from Excel
# - Groups APIs by namespace and class
# - Creates separate issue files for each namespace
# - Generates a main tracking issue with links to all namespace issues
# - Uses editable markdown templates for customization
# - Handles large namespaces by splitting content (main issue + comment parts)
# - Formats API documentation links correctly for GitHub
#
# Usage: pwsh generate_markdown_issues.ps1 [-DotNetVersion "10.0"]

param(
    [string]$ExcelFile = "UndocAPIReport_github.com_dotnet_dotnet-api-docs_main_dotnet-api-docs.xlsx",
    [string]$DotNetVersion = "10.0"
)

# Check if ImportExcel module is available
if (-not (Get-Module -ListAvailable -Name ImportExcel)) {
    Write-Host "❌ ImportExcel module not found. Installing..." -ForegroundColor Red
    try {
        Install-Module ImportExcel -Force -Scope CurrentUser
        Write-Host "✅ ImportExcel module installed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to install ImportExcel module: $_" -ForegroundColor Red
        Write-Host "Please install manually: Install-Module ImportExcel -Force" -ForegroundColor Yellow
        exit 1
    }
}

# Check if Excel file exists
if (-not (Test-Path $ExcelFile)) {
    Write-Host "❌ Excel file not found: $ExcelFile" -ForegroundColor Red
    exit 1
}

Write-Host "🔄 Loading Excel data from $ExcelFile..." -ForegroundColor Green
Write-Host "🎯 Target .NET version: $DotNetVersion" -ForegroundColor White

# Import the Excel data
try {
    $ExcelData = Import-Excel -Path $ExcelFile -WorksheetName "Compliance"
    Write-Host "✅ Loaded $($ExcelData.Count) total records from Compliance sheet" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to load Excel data: $_" -ForegroundColor Red
    exit 1
}

# Filter for the specific .NET version (using Version Introduced column)
$NetVersionAPIs = $ExcelData | Where-Object { $_.'Version Introduced' -eq $DotNetVersion }
Write-Host "✅ Found $($NetVersionAPIs.Count) .NET $DotNetVersion APIs" -ForegroundColor Green

if ($NetVersionAPIs.Count -eq 0) {
    Write-Host "❌ No APIs found for .NET version $DotNetVersion" -ForegroundColor Red
    exit 1
}

# Group APIs by namespace for individual issues
$apisByNamespace = $NetVersionAPIs | Group-Object -Property "Namespace" | Sort-Object Name

# Create output directory
$OutputDir = "artifacts\namespace_issues"
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
Write-Host "📁 Created output directory: $OutputDir" -ForegroundColor Green

# Load templates
$NamespaceTemplate = Get-Content "templates\namespace_issue_template.md" -Raw
if (-not $NamespaceTemplate) {
    Write-Host "❌ Failed to load namespace issue template" -ForegroundColor Red
    exit 1
}

# Function to create namespace issue content
function New-NamespaceIssue {
    param(
        [string]$Namespace,
        [array]$APIs,
        [string]$DotNetVersion
    )
    
    # Group APIs by class
    $apisByClass = $APIs | Group-Object -Property "Class" | Sort-Object Name
    
    # Create API list grouped by class
    $apiListContent = ""
    foreach ($classGroup in $apisByClass) {
        $className = $classGroup.Name
        if ([string]::IsNullOrWhiteSpace($className)) {
            $className = "Unknown"
        }
        
        $apiListContent += "### $className`n`n"
        
        foreach ($api in ($classGroup.Group | Sort-Object "Name")) {
            $apiName = $api."Name"
            $docPath = $api."Source File Path"
            
            if (-not [string]::IsNullOrWhiteSpace($docPath)) {
                # Use the API column which already has the formatted link
                $apiListContent += "- " + $api."API" + "`n"
            } else {
                $apiListContent += "- $apiName`n"
            }
        }
        $apiListContent += "`n"
    }
    
    # Replace placeholders in template
    $content = $NamespaceTemplate
    $content = $content -replace '\{\{NAMESPACE\}\}', $Namespace
    $content = $content -replace '\{\{DOTNET_VERSION\}\}', $DotNetVersion
    $content = $content -replace '\{\{API_COUNT\}\}', $APIs.Count
    $content = $content -replace '\{\{API_LIST\}\}', $apiListContent.TrimEnd()
    $content = $content -replace '\{\{MAIN_ISSUE_NUMBER\}\}', '#MAIN_TRACKING_ISSUE_NUMBER'
    
    return @{
        Content = $content
        APIsByClass = $apisByClass
        ApiCount = $APIs.Count
    }
}

# Function to create split namespace issue (header only)
function New-SplitNamespaceIssue {
    param(
        [string]$Namespace,
        [int]$ApiCount,
        [string]$DotNetVersion
    )
    
    # Create header-only content for split issues
    $content = $NamespaceTemplate
    $content = $content -replace '\{\{NAMESPACE\}\}', $Namespace
    $content = $content -replace '\{\{API_COUNT\}\}', $ApiCount
    $content = $content -replace '\{\{API_LIST\}\}', "This namespace has been split into multiple comments below due to GitHub's size limits. Please see the individual class comments for the complete API list."
    $content = $content -replace '\{\{MAIN_ISSUE_NUMBER\}\}', '#MAIN_TRACKING_ISSUE_NUMBER'
    
    return $content
}

# Function to create class comment content
function New-ClassComment {
    param(
        [string]$ClassName,
        [array]$APIs
    )
    
    $content = "### $ClassName`n`n"
    
    foreach ($api in ($APIs | Sort-Object "Name")) {
        $apiName = $api."Name"
        $docPath = $api."Source File Path"
        
        if (-not [string]::IsNullOrWhiteSpace($docPath)) {
            # Use the API column which already has the formatted link
            $content += "- " + $api."API" + "`n"
        } else {
            $content += "- $apiName`n"
        }
    }
    
    return $content
}

# Function to create main tracking issue
function New-MainTrackingIssue {
    param(
        [string]$DotNetVersion,
        [int]$TotalAPIs,
        [int]$TotalNamespaces,
        [array]$NamespaceGroups
    )
    
    # Load main issue template
    $MainTemplate = Get-Content "templates\main_issue_template.md" -Raw
    if (-not $MainTemplate) {
        Write-Host "❌ Failed to load main issue template" -ForegroundColor Red
        return ""
    }
    
    # Create namespace table (no headers since template has them)
    $namespaceTable = ""
    
    foreach ($namespaceGroup in $NamespaceGroups) {
        $namespace = $namespaceGroup.Name
        $apiCount = $namespaceGroup.Count
        $safeName = $namespace -replace '\.', '_' -replace ' ', '_'
        $namespaceTable += "| $namespace | $apiCount | NAMESPACE_ISSUE_PLACEHOLDER_$safeName |`n"
    }
    
    # Replace placeholders in template
    $content = $MainTemplate
    $content = $content -replace '\{\{DOTNET_VERSION\}\}', $DotNetVersion
    $content = $content -replace '\{\{TOTAL_APIS\}\}', $TotalAPIs
    $content = $content -replace '\{\{TOTAL_NAMESPACES\}\}', $TotalNamespaces
    $content = $content -replace '\{\{NAMESPACE_TABLE\}\}', $namespaceTable.TrimEnd()
    
    return $content
}

# Generate individual namespace issues
Write-Host "🔨 Generating namespace issues..." -ForegroundColor Green
$createdCount = 0

foreach ($namespaceGroup in $apisByNamespace) {
    $namespace = $namespaceGroup.Name
    $apis = $namespaceGroup.Group
    
    Write-Host "  Creating issue for $namespace ($($apis.Count) APIs)..." -ForegroundColor White
    
    # Generate the issue content
    $issueData = New-NamespaceIssue -Namespace $namespace -APIs $apis -DotNetVersion $DotNetVersion
    $issueContent = $issueData.Content
    
    # Create safe filename
    $safeName = $namespace -replace '\.', '_' -replace ' ', '_'
    $filename = "$OutputDir\$safeName.md"
    
    # Check if content is too large (GitHub limit is ~65KB, use small buffer)
    if ($issueContent.Length -gt 20000) {
        Write-Host "    ⚠️  Content too large ($($issueContent.Length) chars), will use split strategy" -ForegroundColor Yellow
        
        # Create header-only content for the main issue
        $splitContent = New-SplitNamespaceIssue -Namespace $namespace -ApiCount $issueData.ApiCount -DotNetVersion $DotNetVersion
        $splitContent | Out-File -FilePath $filename -Encoding UTF8
        
        # Create additional files for class groups (will be used as comments)
        $classIndex = 1
        foreach ($classGroup in ($issueData.APIsByClass | Sort-Object Name)) {
            $className = $classGroup.Name
            if ([string]::IsNullOrWhiteSpace($className)) {
                $className = "Unknown"
            }
            
            $classApis = $classGroup.Group
            $classContent = New-ClassComment -ClassName $className -APIs $classApis
            
            # Check if this single class content is too large for one comment (GitHub limit ~65KB)
            if ($classContent.Length -gt 60000) {
                Write-Host "    ⚠️  Class $className too large ($($classContent.Length) chars), splitting into chunks" -ForegroundColor Yellow
                
                # Split APIs into chunks of ~50 APIs each for very large classes
                $chunkSize = 50
                $chunks = [math]::Ceiling($classApis.Count / $chunkSize)
                
                for ($chunkIndex = 0; $chunkIndex -lt $chunks; $chunkIndex++) {
                    $startIndex = $chunkIndex * $chunkSize
                    $endIndex = [math]::Min(($chunkIndex + 1) * $chunkSize - 1, $classApis.Count - 1)
                    $chunkApis = $classApis[$startIndex..$endIndex]
                    
                    # Use "(continued)" naming for subsequent chunks
                    $chunkTitle = if ($chunkIndex -eq 0) { $className } else { "$className (continued)" }
                    $chunkContent = New-ClassComment -ClassName $chunkTitle -APIs $chunkApis
                    
                    $classFilename = "$OutputDir\$safeName" + "_part$($classIndex.ToString('00')).md"
                    $chunkContent | Out-File -FilePath $classFilename -Encoding UTF8
                    Write-Host "    📝 Created part $($classIndex.ToString('00')) for $chunkTitle ($($chunkApis.Count) APIs)" -ForegroundColor Cyan
                    $classIndex++
                }
            } else {
                # Single class fits in one comment
                $classFilename = "$OutputDir\$safeName" + "_part$($classIndex.ToString('00')).md"
                $classContent | Out-File -FilePath $classFilename -Encoding UTF8
                Write-Host "    📝 Created part $($classIndex.ToString('00')) for $className ($($classApis.Count) APIs)" -ForegroundColor Cyan
                $classIndex++
            }
        }
    } else {
        # Normal single-file output
        $issueContent | Out-File -FilePath $filename -Encoding UTF8
    }
    
    $createdCount++
    Write-Host "    ✅ Created $filename" -ForegroundColor Green
}

# Create main tracking issue template
Write-Host "📋 Creating main tracking issue template..." -ForegroundColor Green
$mainIssueContent = New-MainTrackingIssue -DotNetVersion $DotNetVersion -TotalAPIs $NetVersionAPIs.Count -TotalNamespaces $apisByNamespace.Count -NamespaceGroups $apisByNamespace
$mainIssueFile = "artifacts\main_issue.md"
$mainIssueContent | Out-File -FilePath $mainIssueFile -Encoding UTF8
Write-Host "✅ Created main tracking issue template: $mainIssueFile" -ForegroundColor Green

# Create summary JSON file
Write-Host "💾 Creating summary data file..." -ForegroundColor Green

$summaryData = @{
    total_apis = $NetVersionAPIs.Count
    total_namespaces = $apisByNamespace.Count
    dotnet_version = $DotNetVersion
    namespaces = @()
}

foreach ($namespaceGroup in $apisByNamespace) {
    $summaryData.namespaces += @{
        name = $namespaceGroup.Name
        api_count = $namespaceGroup.Count
        filename = ($namespaceGroup.Name -replace '\.', '_' -replace ' ', '_') + ".md"
    }
}

$summaryData | ConvertTo-Json -Depth 3 | Out-File -FilePath "artifacts\issue_data.json" -Encoding UTF8

Write-Host ""
Write-Host "🎉 Generation Complete!" -ForegroundColor Green
Write-Host "📊 Summary:" -ForegroundColor White
Write-Host "   - .NET version: $DotNetVersion" -ForegroundColor White  
Write-Host "   - Generated namespace issues: $createdCount" -ForegroundColor White
Write-Host "   - Total APIs to document: $($NetVersionAPIs.Count)" -ForegroundColor White
Write-Host "   - Output directory: $OutputDir" -ForegroundColor White
Write-Host "   - Summary data: artifacts\issue_data.json" -ForegroundColor White
Write-Host "   - Main tracking issue: artifacts\main_issue.md" -ForegroundColor White
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Green
Write-Host "1. Review the generated issues in $OutputDir" -ForegroundColor White
Write-Host "2. Edit create_github_issues.ps1 to set your target repository" -ForegroundColor White
Write-Host "3. Run: pwsh create_github_issues.ps1" -ForegroundColor White