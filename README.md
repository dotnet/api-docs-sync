# DocsPortingTool

This tool finds and ports triple slash comments found in .NET repos but **do not yet exist** in the dotnet-api-docs repo.
If an API is already documented in dotnet-api-docs, it will be ignored and skipped.


### Instructions

1. Build your source code repo. For example: dotnet/runtime, or dotnet/wpf, or dotnet/winforms, etc.
2. Clone the docs repo (dotnet/dotnet-api-docs). No need to build it.
3. Clone this repo (carlossanlop/DocsPortingTool).
4. Run one of the commands below:

    A) To port from IntelliSense xmls to Docs xmls, specify these parameters:

    ```
        -Direction ToDocs
        -Docs <pathToDocsXmlFolder>
        -IntelliSense <pathToArtifactsFolder1>[,<pathToArtifactsFolder2>,...,<pathToArtifactsFolderN>]
        -IncludedAssemblies <assembly1>[,<assembly2>,...<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]
        -Save true
    ```

    Example:
    ```
        DocsPortingTool \
            -Direction ToDocs \
            -Docs D:\dotnet-api-docs\xml \
            -IntelliSense D:\runtime\artifacts\bin\System.IO.FileSystem\ \
            -IncludedAssemblies System.IO.FileSystem \
            -IncludedNamespaces System.IO \
            -Save true
    ```

    B) To port from Docs xmls to triple slash comments, specify these parameters:

    ```
        -Direction ToTripleSlash
        -CsProj <pathToCsproj>
        -Docs <pathToDocsXmlFolder>
        -IncludedAssemblies <assembly1>[,<assembly2>,...,<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]
        -Save true
    ```

    Example:
    ```
    DocsPortingTool \
        -Direction ToTripleSlash \
        -CsProj D:\runtime\src\libraries\System.IO.Compression.Brotli\src\System.IO.Compression.Brotli.csproj \
        -Docs D:\dotnet-api-docs\xml \
        -IncludedAssemblies System.IO.Compression.Brotli \
        -IncludedNamespaces System.IO.Compression \
        -Save true
    ```

### Install as dotnet tool

You can run `./install-as-tool.ps1` to install as a dotnet tool in your PATH.

Instructions for installing: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install

Don't forget to update it before using in the future, in case any improvements have been added.

Instructions for updating: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-update


### Command line options

```
This tool finds and ports triple slash comments found in .NET repos but do not yet exist in the dotnet-api-docs repo.

The instructions below assume %SourceRepos% is the root folder of all your git cloned projects.

Options:

                               MANDATORY
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------

    -Docs                    comma-separated    A comma separated list (no spaces) of absolute directory paths where the Docs xml files are located.
                                                    The xml files will be searched for recursively.
                                                    If any of the segments in the path may contain spaces, make sure to enclose the path in double quotes.
                             folder paths           Known locations:
                                                        > Runtime:      %SourceRepos%\dotnet-api-docs\xml
                                                        > WPF:          %SourceRepos%\dotnet-api-docs\xml
                                                        > WinForms:     %SourceRepos%\dotnet-api-docs\xml
                                                        > ASP.NET MVC:  %SourceRepos%\AspNetApiDocs\aspnet-mvc\xml
                                                        > ASP.NET Core: %SourceRepos%\AspNetApiDocs\aspnet-core\xml
                                                    Usage example:
                                                        -Docs ""%SourceRepos%\dotnet-api-docs\xml\System.IO.FileSystem\"",%SourceRepos%\AspNetApiDocs\aspnet-mvc\xml

    -IntelliSense           comma-separated     Mandatory only when using '-Direction ToDocs' to port from IntelliSense xml to Docs.
                            folder paths            A comma separated list (no spaces) of absolute directory paths where we the IntelliSense xml files
                                                    are located. Usually it's the 'artifacts/bin' folder in your source code repo.
                                                    The IntelliSense xml files will be searched for recursively. You must specify the root folder (usually 'bin'),
                                                    which contains all the subfolders whose names are assemblies or namespaces. Only those names specified
                                                    with '-IncludedAssemblies' and '-IncludedNamespaces' will be recursed.
                                                    If any of the segments in the path may contain spaces, make sure to enclose the path in double quotes.
                                                    Known locations:
                                                        > Runtime:   %SourceRepos%\runtime\artifacts\bin\
                                                        > CoreCLR:   %SourceRepos%\runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\IL\
                                                        > WinForms:  %SourceRepos%\winforms\artifacts\bin\
                                                        > WPF:       %SourceRepos%\wpf\artifacts\bin\
                                                    Usage example:
                                                        -IntelliSense ""%SourceRepos%\corefx\artifacts\bin\"",%SourceRepos%\winforms\artifacts\bin\

    -IncludedAssemblies     string list         Comma separated list (no spaces) of assemblies to include.
                                                This argument prevents loading everything in the specified folder.
                                                    Usage example:
                                                        -IncludedAssemblies System.IO,System.Runtime

                                                    IMPORTANT: 
                                                    Namespaces usually match the assembly name. There are some exceptions, like with types that live in
                                                    the System.Runtime assembly. For those cases, make sure to also specify the -IncludedNamespaces argument.

    -CsProj                 file path           Mandatory only when using '-Direction ToTripleSlash' to port from Docs to triple slash comments in source.
                                                    An absolute path to a *.csproj file from your repo. Make sure its the src file, not the ref or test file.
                                                    Known locations:
                                                        > Runtime:   %SourceRepos%\runtime\src\libraries\<AssemblyOrNamespace>\src\<AssemblyOrNamespace>.csproj
                                                        > CoreCLR:   %SourceRepos%\runtime\src\coreclr\src\System.Private.CoreLib\System.Private.CoreLib.csproj
                                                        > WPF:       %SourceRepos%\wpf\src\Microsoft.DotNet.Wpf\src\<AssemblyOrNamespace>\<AssemblyOrNamespace>.csproj
                                                        > WinForms:  %SourceRepos%\winforms\src\<AssemblyOrNamespace>\src\<AssemblyOrNamespace>.csproj  
                                                        > WCF:       %SourceRepos%\wcf\src\<AssemblyOrNamespace>\
                                                    Usage example:
                                                        -SourceCode ""%SourceRepos%\runtime\src\libraries\System.IO.FileSystem\"",%SourceRepos%\runtime\src\coreclr\src\System.Private.CoreLib\

                               OPTIONAL
  ------------------------------------------------------------
  |    PARAMETER     |           TYPE          | DESCRIPTION |
  ------------------------------------------------------------

    -h | -Help              no arguments        Displays this help message. If used, all other arguments are ignored and the program exits.

    -BinLog                 bool                Default is false (binlog file generation is disabled).
                                                When set to true, will output a diagnostics binlog file if using '-Direction ToTripleSlash'.

    -Direction              string              Default is 'ToDocs'.
                                                Determines in which direction the comments should flow.
                                                Possible values:
                                                    > ToDocs: Comments are ported from the Intellisense xml files generated in the specified source code repo build,
                                                              to the specified Docs repo containing ECMA xml files.
                                                    > ToTripleSlash: Comments are ported from the specified Docs repo containint ECMA xml files,
                                                              to the triple slash comments on top of each API in the specified source code repo.
                                                              Using this option automatically sets `SkipInterfaceImplementations` to `true`, to avoid loading
                                                              unnecessary interface docs xml files into memory.
                                                Usage example:
                                                    -Direction ToTripleSlash

    -DisablePrompts         bool                Default is false (prompts are disabled).
                                                Avoids prompting the user for input to correct some particular errors.
                                                    Usage example:
                                                        -DisablePrompts true

    -ExceptionCollisionThreshold  int (0-100)   Default is 70 (If >=70% of words collide, the string is not ported).
                                                Decides how sensitive the detection of existing exception strings should be.
                                                The tool compares the Docs exception string with the IntelliSense xml exception string.
                                                If the number of words found in the Docs exception is below the specified threshold,
                                                then the IntelliSense Xml string is appended at the end of the Docs string.
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

    -ExcludedNamespaces     string list         Default is empty (does not exclude any namespaces from the specified assemblies).
                                                Comma separated list (no spaces) of specific namespaces to exclude from the specified assemblies.
                                                    Usage example:
                                                        -ExcludedNamespaces System.Runtime.Intrinsics,System.Reflection.Metadata

    -ExcludedTypes          string list         Default is empty (does not ignore any types).
                                                Comma separated list (no spaces) of names of types to ignore.
                                                    Usage example:
                                                        -ExcludedTypes ArgumentException,Stream

    -IncludedNamespaces     string list         Default is empty (includes all namespaces from the specified assemblies).
                                                Comma separated list (no spaces) of specific namespaces to include from the specified assemblies.
                                                    Usage example:
                                                        -IncludedNamespaces System,System.Data

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
