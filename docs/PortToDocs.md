# PortToDocs

This tool ports intellisense xml documentation to API docs.

The way it works is the following:

- You specify the source dotnet repo (for example: runtime, winforms, wpf, wcf, etc.).
- You specify the API docs target repo (for example: dotnet-api-docs).
- You specify a list of assemblies to port, and optionally, a list of namespaces and/or types.
- The tool will then find all APIs that match the specified filters, both among the intellisense xml files of the dotnet repo and in the xml files of the API docs repo.
- If an API docs xml item is still undocumented (it has the `To be added.` boilerplate message), then the tool will copy all the documentation it can find for that API in its intellisense xml file, and paste it in the API docs xml item.

## Instructions

1. Clone and build your source code dotnet repo (for example: runtime, winforms, wpf, wcf, etc.).
2. Clone the API docs repo (for example: dotnet-api-docs). No need to build it.
3. Clone this repo.
4. Run the command to port documentation:

    ```cmd
        -Docs <pathToDocsXmlFolder>
        -IntelliSense <pathToArtifactsFolder1>[,<pathToArtifactsFolder2>,...,<pathToArtifactsFolderN>]
        -IncludedAssemblies <assembly1>[,<assembly2>,...<assemblyN>]
        -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]
        -Save true
    ```

    Example:

    ```cmd
        PortToDocs \
            -Docs D:\dotnet-api-docs\xml \
            -IntelliSense D:\runtime\artifacts\bin\System.IO.FileSystem\ \
            -IncludedAssemblies System.IO.FileSystem \
            -IncludedNamespaces System.IO \
            -Save true
    ```

To view all the available CLI arguments, run:

```cmd
PortToDocs -h
```
