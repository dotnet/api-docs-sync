# DocsPortingTool

This tool finds and ports triple slash comments found in .NET repos but **do not yet exist** in the dotnet-api-docs repo.
If an API is already documented in dotnet-api-docs, it will be ignored and skipped.


### TL;DR

Just specify these parameters:

    -Docs <path>
    -TripleSlash <path1>[,<path2>,...,<pathN>]
    -IncludedAssemblies <namespace1>[,<namespace2>,...<namespaceN>]
    -Save true

Example:
```
    DocsPortingTool \
        -Docs D:\dotnet-api-docs\xml \
        -TripleSlash D:\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\,D:\runtime\artifacts\bin\ \
        -IncludedAssemblies System.IO.FileSystem,System.Runtime.Intrinsics \
        -Save true
```

### Instructions

Assumptions for the example in the instructions:

- Your source code project is dotnet/runtime.
    - _But it can be any project that generates xml documentation, like WPF, WinForms or WCF._
- Your docs project is dotnet/dotnet-api-docs.
    - _But it can be any other documentation project that contains xmls, like dotnet/AspNetApiDocs._
- The location of your github projects is the D:\ drive.
    - _Your git clones can be in any other folder ._
- You want to port comments from the System.IO.FileSystem assembly.
    - _It can be any combination of assemblies or namespaces._

Steps:

1. Build your source code repo (dotnet/runtime).
2. Clone the docs repo (dotnet/dotnet-api-docs). No need to build it.
3. Clone this repo (carlossanlop/DocsPortingTool).
4. Run the command with the desired arguments.


### Install

You can run `./install-as-tool.ps1` to install as a dotnet tool in your PATH.

Instructions for installing: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install

Don't forget to update it before using in the future, in case any improvements have been added.

Instructions for updating: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-update


### Command line options

```
                               MANDATORY
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------

    -Docs                 folder path           The absolute directory root path where the Docs xml files are located.
                                                    Known locations:
                                                        > Runtime:      %SourceRepos%\dotnet-api-docs\xml
                                                        > WPF:          %SourceRepos%\dotnet-api-docs\xml
                                                        > WinForms:     %SourceRepos%\dotnet-api-docs\xml
                                                        > ASP.NET MVC:  %SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
                                                        > ASP.NET Core: %SourceRepos%\AspNetApiDocs\aspnet-core\xml
                                                    Usage example:
                                                        -Docs %SourceRepos%\dotnet-api-docs\xml,%SourceRepos%\AspNetApiDocs\aspnet-mvc\xml

    -TripleSlash         comma-separated        A comma separated list (no spaces) of absolute directory paths where we should recursively 
                            folder paths        look for triple slash comment xml files.
                                                    Known locations:
                                                        > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                        > CoreCLR:   %SourceRepos%\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\
                                                        > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                        > WPF:       %SourceRepos%\wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\netcore-3.0\
                                                    Usage example:
                                                        -TripleSlash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\winforms\artifacts\bin\

    -IncludedAssemblies     string list         Comma separated list (no spaces) of assemblies/namespaces to include.
                                                    Usage example:
                                                        -IncludedAssemblies System.IO,System.Runtime.Intrinsics


                               OPTIONAL
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------

    -h | -Help              no arguments        Displays this help message. If used, nothing else is processed and the program exits.

    -DisablePrompts         bool                Default is false (prompts are disabled).
                                                Avoids prompting the user for input to correct some particular errors.
                                                    Usage example:
                                                        -DisablePrompts true

    -ExceptionCollisionThreshold  int (0-100)   Default is 70 (If >=70% of words collide, the string is not ported).
                                                Decides how sensitive the detection of existing exception strings should be.
                                                The tool compares the Docs exception string with the Triple Slash exception string.
                                                If the number of words found in the Docs exception is below the specified threshold,
                                                then the Triple Slash string is appended at the end of the Docs string.
                                                The user is expected to verify the value.
                                                The reason for this is that exceptions go through language review, and may contain more
                                                than one root cause (separated by '-or-'), and there is no easy way to know if the string
                                                has already been ported or not.
                                                    Usage example:
                                                        -ExceptionCollisionThreshold 60

    -ExcludedAssemblies     string list         Default is empty (does not ignore any assemblies/namespaces).
                                                Comma separated list (no spaces) of specific .NET assemblies/namespaces to ignore.
                                                    Usage example:
                                                        -ExcludedAssemblies System.IO.Compression,System.IO.Pipes

    -ExcludedTypes          string list         Default is empty (does not ignore any types).
                                                Comma separated list (no spaces) of names of types to ignore.
                                                    Usage example:
                                                        -ExcludedTypes ArgumentException,Stream

    -IncludedTypes          string list         Default is empty (includes all types in the desired assemblies/namespaces).
                                                Comma separated list (no spaces) of specific types to include.
                                                    Usage example:
                                                        -IncludedTypes FileStream,DirectoryInfo

    -PortExceptionsExisting     bool            Default is false (does not find and append existing exceptions).
                                                Enable or disable finding, porting and appending summaries from existing exceptions.
                                                Setting this to true can result in a lot of noise because there is
                                                no easy way to detect if an exception summary has been ported already or not,
                                                especially after it went through language review.
                                                See `-ExceptionCollisionThreshold` to set the collision sensitivity.
                                                    Usage example:
                                                        -PortExceptionsExisting true

    -PortExceptionsNew          bool            Default is true (ports new exceptions).
                                                Enable or disable finding and porting new exceptions.
                                                    Usage example:
                                                        -PortExceptionsNew false

    -PortMemberParams           bool            Default is true (ports Member parameters).
                                                Enable or disable finding and porting Member parameters.
                                                    Usage example:
                                                        -PortMemberParams false

    -PortMemberProperties       bool            Default is true (ports Member properties).
                                                Enable or disable finding and porting Member properties.
                                                    Usage example:
                                                        -PortMemberProperties false

    -PortMemberReturns          bool            Default is true (ports Member return values).
                                                Enable or disable finding and porting Member return values.
                                                    Usage example:
                                                        -PortMemberReturns false

    -PortMemberRemarks          bool            Default is true (ports Member remarks).
                                                Enable or disable finding and porting Member remarks.
                                                    Usage example:
                                                        -PortMemberRemarks false

    -PortMemberSummaries        bool            Default is true (ports Member summaries).
                                                Enable or disable finding and porting Member summaries.
                                                    Usage example:
                                                        -PortMemberSummaries false

    -PortMemberTypeParams       bool            Default is true (ports Member TypeParams).
                                                Enable or disable finding and porting Member TypeParams.
                                                    Usage example:
                                                        -PortMemberTypeParams false

    -PortTypeParams             bool            Default is true (ports Type Params).
                                                Enable or disable finding and porting Type Params.
                                                    Usage example:
                                                        -PortTypeParams false

    -PortTypeRemarks            bool            Default is true (ports Type remarks).
                                                Enable or disable finding and porting Type remarks.
                                                    Usage example:
                                                        -PortTypeRemarks false

    -PortTypeSummaries          bool            Default is true (ports Type summaries).
                                                Enable or disable finding and porting Type summaries.
                                                    Usage example:
                                                        -PortTypeSummaries false

    -PortTypeTypeParams         bool            Default is true (ports Type TypeParams).
                                                Enable or disable finding and porting Type TypeParams.
                                                    Usage example:
                                                        -PortTypeTypeParams false

    -PrintUndoc                 bool            Default is false (prints a basic summary).
                                                Prints a detailed summary of all the docs APIs that are undocumented.
                                                    Usage example:
                                                        -PrintUndoc true

    -Save                       bool            Default is false (does not save changes).
                                                Whether you want to save the changes in the dotnet-api-docs xml files.
                                                    Usage example:
                                                        -Save true

    -SkipInterfaceImplementations       bool    Default is false (includes interface implementations).
                                                Whether you want the original interface documentation to be considered to fill the
                                                undocumented API's documentation when the API itself does not provide its own documentation.
                                                Setting this to false will include Explicit Interface Implementations as well.
                                                    Usage example:
                                                        -SkipInterfaceImplementations true

     -SkipInterfaceRemarks              bool    Default is true (excludes appending interface remarks).
                                                Whether you want interface implementation remarks to be used when the API itself has no remarks.
                                                Very noisy and generally the content in those remarks do not apply to the API that implements
                                                the interface API.
                                                    Usage example:
                                                        -SkipInterfaceRemarks false
```
