# DocsPortingTool

This tool finds and ports triple slash comments found in .NET repos but **do not yet exist** in the dotnet-api-docs repo.
If an API is already documented in dotnet-api-docs, it will be ignored and skipped.

### Usage example:

This command will look for triple slash comments in APIs from System.IO* and System.Text* (except System.IO.Compression and System.IO.FileSystem), within the corefx and coreclr repos, then check if they are missing in the dotnet-api-docs repo, and if they are, then they get automatically added to the appropriate xml file:

```
DocsPortingTool.exe -docs D:\dotnet-api-docs\xml -include System.IO,System.Text -exclude System.IO.Compression,System.IO.FileSystem -save true -tripleslash D:\coreclr\bin\Product\Windows_NT.x64.Debug\IL\,D:\corefx\artifacts\bin\
```

### Options:

    no arguments:   -h or -help             Optional. Displays this help message. If used, nothing else will be processed.


    folder path:    -docs                   Mandatory. The absolute directory path to the Docs repo.

                                                Usage example:
                                                    -docs %SourceRepos%\dotnet-api-docs\xml


    string list:    -exclude                Optional. Comma separated list (no spaces) of specific .NET assemblies to ignore. Default is empty.
                                                Usage example:
                                                    -exclude System.IO.Compression,System.IO.Pipes


    string:         -include                Mandatory. Comma separated list (no spaces) of assemblies to include.

                                                Usage example:
                                                    System.IO,System.Runtime.Intrinsics


    bool:           -save                   Optional. Wether we want to save the changes in the dotnet-api-docs xml files. Default is false.
                                                Usage example:
                                                    -save true


    folder paths:   -tripleslash            Mandatory. List of absolute directory paths (comma separated) where we should look for triple slash comment xml files.

                                                Known locations:
                                                    > CoreCLR:   coreclr\bin\Product\Windows_NT.x64.Debug\IL\
                                                    > CoreFX:    corefx\artifacts\bin\
                                                    > WinForms:  winforms\artifacts\bin\
                                                    > WPF:       wpf\.tools\native\bin\dotnet-api-docs_netcoreapp3.0\0.0.0.1\_intellisense\\netcore-3.0\

                                                Usage example:
                                                    -tripleslash %SourceRepos%\corefx\artifacts\bin\,%SourceRepos%\coreclr\bin\Product\Windows_NT.x64.Debug\IL\

