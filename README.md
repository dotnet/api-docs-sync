# DocsPortingTool

This tool finds and ports triple slash comments found in .NET repos but **do not yet exist** in the dotnet-api-docs repo.
If an API is already documented in dotnet-api-docs, it will be ignored and skipped.


### Instructions

Assumptions for the example in the instructions:

- Your source code project is dotnet/runtime.
    - _But it can be any project that generates documentation, like WPF or WinForms for example._
- Your docs project is dotnet/dotnet-api-docs.
    - _But it can be any documentation project that contains xmls, like AspNetApiDocs._
- The location of your github projects is the D:\ drive.
    - _Your clones can be in any other folder ._
- You want to port comments from the System.IO.FileSystem assembly.
    - _It can be any namespace or group of namespaces._

Steps:

1. Build your source code repo (dotnet/runtime).
2. Clone the docs repo (dotnet/dotnet-api-docs). No need to build it.
3. Clone this repo (carlossanlop/DocsPortingTool). Run `./install-as-tool.ps1` to install as dotnet tool in your PATH.
4. Run the command with the desired arguments.

### Example

```
DocsPortingTool -docs D:\dotnet-api-docs\xml -tripleslash D:\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\,D:\runtime\artifacts\bin\ -includedassemblies System.IO.FileSystem -excludedassemblies Microsoft -save true
```

### Command line options

```
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

The instructions below assume %SourceRepos% is the root folder of all your git cloned projects.

Options:

   bool:           -DisablePrompts          Optional. Default is false (prompts are disabled).
                                             Avoids prompting the user for input to correct some particular errors.
                                                Usage example:
                                                    -disableprompts true



    no arguments:   -h or -Help             Optional.
                                             Displays this help message. If used, nothing else is processed and the program exits.



    folder path:    -Docs                   Mandatory.
                                             The absolute directory root path where the Docs xml files are located.
                                                Known locations:
                                                    > Runtime:      %SourceRepos%\dotnet-api-docs\xml
                                                    > WPF:          %SourceRepos%\dotnet-api-docs\xml
                                                    > WinForms:     %SourceRepos%\dotnet-api-docs\xml
                                                    > ASP.NET MVC:  %SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
                                                    > ASP.NET Core: %SourceRepos%\AspNetApiDocs\aspnet-core\xml
                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs\xml,%SourceRepos%\AspNetApiDocs\aspnet-mvc\xml



    string list:    -ExcludedAssemblies     Optional. Default is empty (does not ignore any assemblies/namespaces).
                                             Comma separated list (no spaces) of specific .NET assemblies/namespaces to ignore.
                                                Usage example:
                                                    -excludedassemblies System.IO.Compression,System.IO.Pipes



    string list:    -IncludedAssemblies     Mandatory.
                                             Comma separated list (no spaces) of assemblies/namespaces to include.
                                                Usage example:
                                                    -includedassemblies System.IO,System.Runtime.Intrinsics



    string list:    -ExcludedTypes          Optional. Default is empty (does not ignore any types).
                                             Comma separated list (no spaces) of names of types to ignore.
                                                Usage example:
                                                    -excludedtypes ArgumentException,Stream



    string list:    -IncludedTypes          Mandatory. Default is empty (includes all types in the desired assemblies/namespaces).
                                             Comma separated list (no spaces) of specific types to include.
                                                Usage example:
                                                    -includedtypes FileStream,DirectoryInfo



    bool:           -PrintUndoc             Optional. Default is false (prints a basic summary).
                                             Prints a detailed summary of all the docs APIs that are undocumented.
                                                Usage example:
                                                    -printundoc true



    bool:           -Save                   Optional. Default is false (does not save changes).
                                             Whether you want to save the changes in the dotnet-api-docs xml files.
                                                Usage example:
                                                    -save true



    bool:           -SkipExceptions         Optional. Default is true (skips exceptions).
                                             Whether you want exceptions to be ported or not.
                                             Setting this to false can result in a lot of noise because there is no easy way
                                             to detect if an exception has been ported already or not.
                                                Usage example:
                                                    -skipexceptions false



    bool:           -SkipRemarks            Optional. Default is false (adds remarks).
                                             Whether you want remarks to be ported or not.
                                                Usage example:
                                                    -skipremarks true



    bool:    -SkipInterfaceImplementations  Optional. Default is false (includes interface implementations).
                                             Whether you want the original interface documentation to be considered to fill the
                                             undocumented API's documentation when the API itself does not provide its own documentation.
                                             Setting this to false will include Explicit Interface Implementations as well.
                                                Usage example:
                                                    -skipinterfaceimplementations true



    bool     -SkipInterfaceRemarks          Optional. Default is true (excludes interface remarks).
                                             Whether you want interface implementation remarks to be used when the API itself has no remarks.
                                             Very noisy and generally the content in those remarks do not apply to the API that implements
                                             the interface API.
                                                Usage example:
                                                    -skipinterfaceremarks false



    folder path:   -TripleSlash             Mandatory.
                                            A comma separated list (no spaces) of absolute directory paths where we should recursively 
                                            look for triple slash comment xml files.
                                                Known locations:
                                                    > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                    > CoreCLR:   %SourceRepos%\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\
                                                    > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                    > WPF:       %SourceRepos%\wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\netcore-3.0\
                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\winforms\artifacts\bin\
```
