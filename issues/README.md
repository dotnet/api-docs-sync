# .NET API Documentation Issue Generator

This project automates the creation of GitHub issues for documenting undocumented .NET APIs. It processes Excel compliance data and generates markdown templates for both individual namespace issues and a main tracking issue.

## Prerequisites

1. **PowerShell 7+** - Modern PowerShell version
2. **ImportExcel Module** - For reading Excel files: `Install-Module ImportExcel`
3. **GitHub CLI** - For creating issues: Install from [cli.github.com](https://cli.github.com)
4. **Excel Data File** - Compliance spreadsheet with API data

## Quick Start

### Step 1: Generate Markdown Issues

```powershell
pwsh generate_markdown_issues.ps1 -ExcelFile UndocAPIReport_github.com_dotnet_dotnet-api-docs_main_dotnet-api-docs.xlsx -DotNetVersion 10.0
```

This creates:
- `artifacts/namespace_issues/*.md` - Individual namespace issues
- `artifacts/main_issue.md` - Main tracking issue template  
- `artifacts/issue_data.json` - Summary data

### Step 2: Create GitHub Issues

```powershell
pwsh create_github_issues.ps1 -Repo "owner/repo"
```

This creates:
- 1 main tracking issue from the generated template
- Individual namespace issues (count varies by .NET version)

## Generated Outputs

## File Structure

```
undocAPI/
├── generate_markdown_issues.ps1    # Generate markdown from Excel data
├── create_github_issues.ps1        # Create GitHub issues from markdown
├── templates/                      # Issue templates (editable)
│   ├── main_issue_template.md      # Main tracking issue template
│   └── namespace_issue_template.md # Namespace issue template
├── artifacts/                      # Generated files (safe to delete)
│   ├── namespace_issues/           # Individual namespace markdown files
│   ├── main_issue.md              # Main tracking issue template
│   └── issue_data.json            # Summary data
└── UndocAPIReport_*.xlsx          # Source Excel compliance data
```

## Template System

The system uses editable template files in the `templates/` directory:

### Template Variables
Templates use `{{VARIABLE_NAME}}` syntax for replacements:
- `{{DOTNET_VERSION}}` - .NET version (e.g., "10.0")
- `{{TOTAL_APIS}}` - Total API count
- `{{TOTAL_NAMESPACES}}` - Total namespace count
- `{{NAMESPACE}}` - Namespace name
- `{{API_COUNT}}` - APIs in namespace
- `{{API_LIST}}` - Generated API list
- `{{NAMESPACE_CHECKLIST}}` - Generated namespace checklist

### Template Comments
Comments in templates (between `<!-- -->`) are automatically removed during processing and can contain documentation about variables and formatting.

## Configuration

### .NET Version
Use the `-DotNetVersion` parameter:
```powershell
pwsh generate_markdown_issues.ps1 -DotNetVersion "11.0"
```

### Repository
Use the `-Repo` parameter:
```powershell
pwsh create_github_issues.ps1 -Repo "ericstj/scratch"
```

## Linking Workflow

The system creates bidirectional links between issues:

1. **Main Tracking Issue**: Created first with placeholders for namespace issue numbers
2. **Namespace Issues**: Created with links back to the main tracking issue
3. **Link Updates**: Main tracking issue is automatically updated with actual namespace issue numbers

### Issue Cross-References
- Each namespace issue links to the main tracking issue: "Please follow the instructions in the main tracking issue (#123)"
- The main tracking issue links to each namespace issue: "- [ ] **Namespace** (X APIs) - #456"

## Data Sources

The system reads from the Excel compliance spreadsheet and filters for:
- APIs matching the specified .NET version
- Non-null API links with proper formatting
- Grouped by namespace for organization

## Clean Workflow

1. **Generate** → `pwsh generate_markdown_issues.ps1 -DotNetVersion "X.X"`
2. **Review** → Check `artifacts/` directory contents
3. **Deploy** → `pwsh create_github_issues.ps1 -Repo "owner/repo"`

The `artifacts/` directory can be safely deleted and regenerated at any time.