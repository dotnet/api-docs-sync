# Create GitHub issues from generated markdown files
# 
# This script reads markdown files from artifacts/namespace_issues/ and creates:
# - 1 main tracking issue from artifacts/main_issue.md
# - Individual namespace issues from artifacts/namespace_issues/
#
# Prerequisites:
# - GitHub CLI (gh) installed and authenticated
# - Generated markdown files from generate_markdown_issues.ps1
#
# Usage: 
# pwsh create_github_issues.ps1 -Repo "owner/repo"

param(
    [Parameter(Mandatory=$true)]
    [string]$Repo
)

# Parse repository owner and name from format "owner/repo"
if ($Repo -notmatch '^([^/]+)/([^/]+)$') {
    Write-Error "❌ Invalid repository format. Use: owner/repo (e.g., 'ericstj/scratch')"
    exit 1
}

$RepoOwner = $Matches[1]
$RepoName = $Matches[2]

Write-Host "🎯 Target repository: $RepoOwner/$RepoName" -ForegroundColor Cyan

# Read summary data to get .NET version and counts
$SummaryFile = "artifacts\issue_data.json"
if (-not (Test-Path $SummaryFile)) {
    Write-Error "❌ Summary file not found: $SummaryFile"
    Write-Host "Please run generate_markdown_issues.ps1 first to create the artifacts." -ForegroundColor Yellow
    exit 1
}

$SummaryData = Get-Content $SummaryFile | ConvertFrom-Json
$DotNetVersion = $SummaryData.dotnet_version
$TotalAPIs = $SummaryData.total_apis
$TotalNamespaces = $SummaryData.total_namespaces

Write-Host "🔄 Creating .NET $DotNetVersion documentation issues..." -ForegroundColor Green
Write-Host "📊 Will create $TotalNamespaces namespace issues for $TotalAPIs APIs" -ForegroundColor White

# Create main tracking issue
Write-Host "📝 Creating main tracking issue..."
$MainIssueFile = "artifacts\main_issue.md"

if (-not (Test-Path $MainIssueFile)) {
    Write-Error "❌ Main issue template not found: $MainIssueFile"
    Write-Host "Please run generate_markdown_issues.ps1 first to create the template." -ForegroundColor Yellow
    exit 1
}

# Create the main tracking issue
Write-Host "📝 Creating main tracking issue..."
$MainIssueTitle = ".NET $DotNetVersion API Documentation Tracking Issue"
$MainIssueUrl = gh issue create --repo "$RepoOwner/$RepoName" --title $MainIssueTitle --body-file $MainIssueFile

if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrEmpty($MainIssueUrl)) {
    Write-Host "✅ Created main tracking issue: $MainIssueUrl" -ForegroundColor Green
    
    # Extract the main issue number from the URL
    $MainIssueNumber = ($MainIssueUrl -split '/')[-1]
    Write-Host "📋 Main tracking issue number: #$MainIssueNumber" -ForegroundColor White
} else {
    Write-Host "❌ Failed to create main tracking issue" -ForegroundColor Red
    Write-Host "Please check your GitHub CLI authentication and repository permissions." -ForegroundColor Yellow
    exit 1
}

# Create individual namespace issues
Write-Host "📚 Creating namespace issues..."
$NamespaceFiles = Get-ChildItem "artifacts\namespace_issues\*.md" | Where-Object { $_.Name -notlike "*_part*.md" } | Sort-Object Name
$CreatedCount = 0
$NamespaceIssueMap = @{}

foreach ($File in $NamespaceFiles) {
    $Namespace = $File.BaseName.Replace('_', '.')
    $Title = "$Namespace docs for .NET $DotNetVersion APIs"
    
    Write-Host "  Creating: $Title"
    
    try {
        # Read the template and replace the main tracking issue placeholder
        $TemplateContent = Get-Content $File.FullName -Raw
        $UpdatedContent = $TemplateContent -replace '#MAIN_TRACKING_ISSUE_NUMBER', "#$MainIssueNumber"
        
        # Create a temporary file with updated content
        $TempFile = [System.IO.Path]::GetTempFileName()
        $UpdatedContent | Out-File -FilePath $TempFile -Encoding UTF8
        
        $IssueUrl = gh issue create --repo "$RepoOwner/$RepoName" --title $Title --body-file $TempFile
        
        # Clean up temp file
        Remove-Item $TempFile -Force
        
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrEmpty($IssueUrl)) {
            Write-Host "    ✅ Created: $IssueUrl" -ForegroundColor Green
            $CreatedCount++
            
            # Extract issue number and store mapping
            $IssueNumber = ($IssueUrl -split '/')[-1]
            $NamespaceIssueMap[$Namespace] = $IssueNumber
            
            # Check for split parts and add them as comments
            $SafeName = $File.BaseName
            $PartFiles = Get-ChildItem "artifacts\namespace_issues\$($SafeName)_part*.md" -ErrorAction SilentlyContinue
            
            if ($PartFiles.Count -gt 0) {
                Write-Host "    📝 Adding $($PartFiles.Count) comment parts..." -ForegroundColor Cyan
                
                foreach ($PartFile in ($PartFiles | Sort-Object Name)) {
                    $PartContent = Get-Content $PartFile.FullName -Raw
                    
                    try {
                        # Create a temporary file for the comment body to avoid command line length limits
                        $TempCommentFile = [System.IO.Path]::GetTempFileName()
                        $PartContent | Out-File -FilePath $TempCommentFile -Encoding UTF8
                        
                        gh issue comment $IssueNumber --repo "$RepoOwner/$RepoName" --body-file $TempCommentFile | Out-Null
                        
                        # Clean up temp file
                        Remove-Item $TempCommentFile -Force
                        
                        if ($LASTEXITCODE -eq 0) {
                            Write-Host "      ✅ Added comment from $($PartFile.Name)" -ForegroundColor Green
                        } else {
                            Write-Host "      ❌ Failed to add comment from $($PartFile.Name)" -ForegroundColor Red
                        }
                    }
                    catch {
                        Write-Host "      ❌ Error adding comment from $($PartFile.Name): $_" -ForegroundColor Red
                    }
                    
                    # Small delay between comments
                    Start-Sleep -Milliseconds 100
                }
            }
        } else {
            Write-Host "    ❌ Failed to create issue for $Namespace" -ForegroundColor Red
        }
        
        # Small delay to avoid rate limiting
        Start-Sleep -Milliseconds 250
    }
    catch {
        Write-Host "    ❌ Failed to create issue for $Namespace : $_" -ForegroundColor Red
    }
}

# Update the main tracking issue with actual namespace issue links
if ($NamespaceIssueMap.Count -gt 0) {
    Write-Host "🔗 Updating main tracking issue with namespace issue links..."
    
    try {
        # Read the main issue template
        $MainIssueContent = Get-Content $MainIssueFile -Raw
        
        # Replace each namespace placeholder with actual issue number
        # Sort by namespace length (longest first) to avoid substring matching issues
        $SortedEntries = $NamespaceIssueMap.GetEnumerator() | Sort-Object { $_.Key.Length } -Descending
        
        foreach ($NamespaceEntry in $SortedEntries) {
            $Namespace = $NamespaceEntry.Key
            $IssueNumber = $NamespaceEntry.Value
            $SafeName = $Namespace -replace '\.', '_' -replace ' ', '_'
            
            # Find and replace the exact placeholder with GitHub issue reference
            $Pattern = "NAMESPACE_ISSUE_PLACEHOLDER_$([regex]::Escape($SafeName))\b"
            $Replacement = "#$IssueNumber"
            $MainIssueContent = $MainIssueContent -replace $Pattern, $Replacement
        }
        
        # Create temp file with updated content
        $TempMainFile = [System.IO.Path]::GetTempFileName()
        $MainIssueContent | Out-File -FilePath $TempMainFile -Encoding UTF8
        
        # Update the main issue
        gh issue edit $MainIssueNumber --repo "$RepoOwner/$RepoName" --body-file $TempMainFile | Out-Null
        
        # Clean up temp file
        Remove-Item $TempMainFile -Force
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Updated main tracking issue with namespace links" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Failed to update main tracking issue with links" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "⚠️ Failed to update main tracking issue: $_" -ForegroundColor Yellow
    }
}

Write-Host "`n🎉 GitHub Issue Creation Complete!" -ForegroundColor Green
Write-Host "📊 Summary:" -ForegroundColor Yellow
Write-Host "   - .NET version: $DotNetVersion" -ForegroundColor White
Write-Host "   - Main tracking issue created" -ForegroundColor White  
Write-Host "   - Namespace issues created: $CreatedCount" -ForegroundColor White
Write-Host "   - Total APIs to be documented: $TotalAPIs" -ForegroundColor White
Write-Host "   - Repository: https://github.com/$RepoOwner/$RepoName/issues" -ForegroundColor White
Write-Host "`n✅ All issues created with properly formatted API links!" -ForegroundColor Green