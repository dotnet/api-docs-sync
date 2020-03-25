# DocsPortingTool

This tool finds and ports triple slash comments found in .NET repos but **do not yet exist** in the dotnet-api-docs repo.
If an API is already documented in dotnet-api-docs, it will be ignored and skipped.


### Instructions

Assumptions:

- Your source code project is dotnet/runtime.
- Your docs project is dotnet/dotnet-api-docs.
- The location of your github projects is the D:\ drive.
- You want to port comments from the System.IO.FileSystem assembly.

Steps:

1. Build your source code repo (dotnet/runtime).
2. Clone the docs repo (dotnet/dotnet-api-docs). No need to build it.
3. Clone this repo (carlossanlop/DocsPortingTool). Build it.
4. Run the command with the desired arguments.


### Example

```
DocsPortingTool.exe -docs D:\dotnet-api-docs\xml -tripleslash D:\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\,D:\runtime\artifacts\bin\ -includedassemblies System.IO.FileSystem -excludedassemblies Microsoft -save true
```

### Command line options

```
   bool:           -DisablePrompts          Optional. Will avoid prompting the user for input to correct some particular errors. Default is false.

                                                Usage example:
                                                    -disableprompts true



    no arguments:   -h or -Help             Optional. Displays this help message. If used, nothing else will be processed.



    folder path:    -Docs                   Mandatory. The absolute directory root path where your documentation xml files are located.

                                                Known locations:
                                                    > Runtime:      %SourceRepos%\dotnet-api-docs\xml
                                                    > WPF:          %SourceRepos%\dotnet-api-docs\xml
                                                    > WinForms:     %SourceRepos%\dotnet-api-docs\xml
                                                    > ASP.NET MVC:  %SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
                                                    > ASP.NET Core: %SourceRepos%\AspNetApiDocs\aspnet-core\xml

                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs\xml,%SourceRepos%\AspNetApiDocs\aspnet-mvc\xml



    string list:    -ExcludedAssemblies         Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.

                                                Usage example:
                                                    -excludedassemblies System.IO.Compression,System.IO.Pipes



    string list:    -IncludedAssemblies         Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    -includedassemblies System.IO,System.Runtime.Intrinsics


    string list:    -ExcludedTypes              Optional. Comma separated list (no spaces) of specific types to ignore. Default is empty.

                                                Usage example:
                                                    -excludedtypes ArgumentException,Stream



    string list:    -IncludedTypes         Mandatory. Comma separated list (no spaces) of specific types to include. Default is empty and will include all types in the selected assemblies.

                                                Usage example:
                                                    -includedtypes FileStream,DirectoryInfo



    bool:           -PrintUndoc             Optional. Will print a detailed summary of all the docs APIs that are undocumented. Default is false.

                                                Usage example:
                                                    -printundoc true



    bool:           -Save                   Optional. Whether you want to save the changes in the dotnet-api-docs xml files. Default is false.

                                                Usage example:
                                                    -save true



    bool:           -SkipExceptions         Optional. Whether you want exceptions to be ported or not. Setting this to false can result in a lot of noise because there is no way to
                                            detect if an exception has been ported already, but it went through language review and the original text was not preserved. Default is true (skips them).

                                                Usage example:
                                                    -skipexceptions false


    bool:           -SkipRemarks            Optional. Whether you want remarks to be ported or not. Default is false (includes them).

                                                Usage example:
                                                    -skipremarks true


    bool:    -SkipInterfaceImplementations  Optional. Whether you want the original interface documentation to be considered to fill the undocumented API's documentation when the API
                                             itself does not provide its own documentation. This includes Explicit Interface Implementations. Default is false (includes them).

                                                Usage example:
                                                    -skipinterfaceimplementations true


    folder path:   -TripleSlash             Mandatory. A comma separated list (no spaces) of absolute directory paths where we should recursively look for triple slash comment xml files.

                                                Known locations:
                                                    > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                    > CoreCLR:   %SourceRepos%\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\
                                                    > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                    > WPF:       %SourceRepos%\wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\winforms\artifacts\bin\
