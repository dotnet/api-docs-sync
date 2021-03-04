# Backporting MS Docs documentation to triple slash comments

This document describes the process to backport MS Docs documentation to triple slash comments, so that triple slash comments become the source of truth for a particular assembly.

Each area ownership pod is free to decide if they want to address backporting documentation for one of their assemblies in 6.0 or in Future. In other words, if you cannot get to this task by 6.0 for one of your assemblies, MS Docs will continue as the source of truth for that assembly (we won't block shipping).

In summary, the process consists of 3 steps:

- Moving lengthy remarks to their own md files in dotnet-api-docs.
- Backport docs for an assembly to triple slash.
- Build your assembly.
- Review the PR.

For reference, here are a couple of assemblies that have been addressed:

- System.IO.Compression.ZipFile ([backport PR](https://github.com/dotnet/runtime/pull/48633), [lengthy remarks PR](https://github.com/dotnet/dotnet-api-docs/pull/5363))
- System.Diagnostics.Process ([backport PR](https://github.com/dotnet/runtime/pull/48137), [lengthy remarks PR](https://github.com/dotnet/dotnet-api-docs/pull/5360))

## Requirements

- [.NET 6.0 SDK](https://github.com/dotnet/installer#installers-and-binaries)

## Instructions

1. Choose the assembly you are going to backport. It can be from any source code repo (Runtime, WPF, WinForms, WCF, etc.).

    > Example:
    > Let's use a tiny assembly from the Runtime repo - `System.IO.Compression.ZipFile`.

2. Open the `*.sln` and take note of the public types that belong to that assembly.

    > Example:
    >
    > - Open `runtime/src/libraries/System.IO.Compression.ZipFile/System.IO.Compression.ZipFile.sln`.
    > - There are two public types: `ZipFile` and `ZipFileExtensions`, both of them split among many files.

3. Open the root folder of the docs repo in Visual Studio, and locate the documentation files for all those types.

    > Example:
    > The documentation for Runtime lives in `dotnet-api-docs`, but it can be any other, like `AspNetApiDocs`:
    >
    > - `dotnet-api-docs\xml\System.IO.Compression\ZipFile.xml`.
    > - `dotnet-api-docs\xml\System.IO.Compression\ZipFileExtensions.xml`.

4. Scroll through the whole file and look for any lengthy remarks or remarks that contain code snippets.

    A remarks section is considered "lengthy" if it occupies more than half of the screen, particularly if you imagine it in triple slash comments on top of an API in source code.

    A code snippet is an API usage example wrapped by three backticks and the language name.

    > Example:
    >
    > - The `ZipFile.xml` file has lengthy remarks in the main type section at the top, and in the `Open` method section.
    > - The `ZipFileExtensions.xml` file has lengthy remarks in the main type class section.
    > - The `ZipFile.xml` file has several sections with example files linked as hyperlinks (`[!code-csharp[name](~/samples/path/to/file.cs)]`): Those can stay.

5. Cut the remarks text (**exclude** the `<format>` and `CDATA` tags) and move it to a new file named with this structure.

    > Example:
    >
    > `dotnet-api-docs/includes/remarks/<Assembly>/<Type>/<ApiName>.md`

    You can erase the `## Remarks` header, if found. It's not needed.

    When naming the file, try to follow the naming pattern used by the API name in the xml. For example:

     - A file for remarks of the type itself, must be named after the type: `MyType.md`
     - A constructor file would be named `.ctor.md`.
     - A method overload file that needs to be differentiated from other overload files needs to specify the data type of the parameters (no need to add the fullname), separated by underscores:
        - `MyMethod_Int32_String_StringOptions.md`
        - `MyMethod_Int64_FileStream.md`
     - A file for an API with typeparams can use the _backtick_ (`` ` ``) character and the number of typeparams in the signature:
        - ``MyMethod`1.md``
        - ``MyMethod`2.md``

    > Example: Refer to the `*.md` files in this PR: [dotnet/dotnet-api-docs/pull/5363/files](https://github.com/dotnet/dotnet-api-docs/pull/5363/files)

    Alternatively, if the remarks are short, but there is a code snippet, you can move just the snippet and link it as a code file with the format `[!code-csharp[name](~/samples/path/to/file.cs)]` or `[code-vb[name]](~/samples/path/to/file.vb)`. Example:
      - Correct: [This is a code example](https://github.com/dotnet/dotnet-api-docs/blob/4610b30468d94ae2e387312f9caeeb88216dd111/xml/System.IO.Compression/ZipFile.xml#L74-L75) properly linked as an external file.
      - Incorrect: [This is a code snippet](https://github.com/dotnet/dotnet-api-docs/blob/dc599c5db9e3464765b4e8c319bc22e525f4eebf/xml/ns-System.Diagnostics.PerformanceData.xml#L157-L238) that will have to be moved to its own file.

6. In the xml file, where the remarks used to be, add an `INCLUDE` markdown element that points to the newly created file, making sure it is still wrapped by the `<format>` and `CDATA` tags.

    > Example:
    >
    > `<format type="text/markdown"><![CDATA[ [!INCLUDE[remarks](~/includes/remarks/<Assembly>/<Type>/<ApiName>.md)] ]]></format>`
    >
    > Refer to the `*.xml` files in this PR: [dotnet/dotnet-api-docs/pull/5363/files](https://github.com/dotnet/dotnet-api-docs/pull/5363/files)

7. Submit a PR for dotnet-api-docs. Tag @carlossanlop, @gewarren and all the area pod co-owners.

8. Build your source code repo. If it's runtime, run these two commands:

    ```
    .\build.cmd libs
    
    dotnet build .\src\libraries\System.Runtime.CompilerServices.Unsafe\src
    ```

9. Clone [DocsPortingTool](https://github.com/carlossanlop/DocsPortingTool) and build it. You can also install it as a dotnet global tool (see instructions in [README](https://github.com/carlossanlop/DocsPortingTool/blob/master/README.md)). Run it using arguments that match the csproj you want to port:

    ```cmd
      DocsPortingTool.exe \
      -CsProj %SourceRepos%\runtime\src\libraries\<Assembly>\src\<ProjectName>.csproj \
      -Docs %SourceRepos%\dotnet-api-docs\xml \
      -Direction ToTripleSlash \
      -IncludedAssemblies <AssemblyName1>[,<AssemblyName2>,...,<AssemblyNameN>] \
      -IncludedNamespaces <namespace1>[,<namespace2>,...,<namespaceN>]
    ```

     ### Important

      - Don't pass `System.Private.CoreLib.csproj` to the `-CsProj` argument! If your assembly owns APIs that live inside `System.Private.CoreLib`, the tool should be smart enough to find them in that among the referenced projects of your assembly.

    > Example:
    >
    > - The project is `System.IO.Compression.ZipFile.csproj`.
    > - The assembly is `System.IO.Compression.ZipFile`.
    > - The namespace of the APIs in that assembly is `System.IO.Compression`.
    > - `%SourceRepos%` refers to the root folder of all your cloned GitHub repos. In this example it's `D:\`.

    ```cmd
      DocsPortingTool.exe \
      -CsProj D:\runtime\src\libraries\System.IO.Compression.ZipFile\src\System.IO.Compression.ZipFile.csproj \
      -Docs D:\dotnet-api-docs\xml \
      -Direction ToTripleSlash \
      -IncludedAssemblies System.IO.Compression.ZipFile \
      -IncludedNamespaces System.IO.Compression
    ```

10. After backporting, you will have to fix some things the tool can't fix:

    a. MS Docs supports using `<see cref="DocId" />`elements that point to method overloads, but triple slash comments don’t support this yet (see [existing csharplang issue](https://github.com/dotnet/csharplang/issues/320)). When the tool ports these, it has no way of knowing if a cref is referencing a method overload, so your runtime project will fail with **CS0419**: "`Ambiguous reference in cref attribute`". Fix it by adding the prefix `O:` to the cref DocId, to indicate an overload.

    b. You may encounter `<param name="parameterName" />` elements that point to parameters in the signature that do not exist, causing error **CS1572**: "`XML comment has a param tag but there is no parameter by that name`". In some cases, you will notice that the next parameter will have a very similar or identical description. This means that a parameter had that name in a particular version, and was then renamed in a newer version. To fix this, remove the `<param>` that does not exist anymore (if you see duplicate descriptions), or rename the param to the correct name (if you don't find a param with a duplicate description).

    c. You may encounter `<paramref name="parameterName" />` elements that point to parameters that do not exist, causing failure **CS1734**: "`Xml comment has a parameter tag, but there is no parameter by that name`". This happens when we decide to rename a method parameter after the old name had already been shipped, but we forgot to update the documentation. Fix it by updating the referenced parameter name.

    d. Some `<see cref="DocId" />` elements may point to APIs living in unreferenced assemblies. We don't want to add them as a dependency of the current project just to resolve the documentation problem, so instead of using `<see cref="DocId" />`, use `<see langword="DocId" />`.

    e. Some markdown hyperlinks with the format `[Friendly text](URL)` may not be resolved by the tool (although most cases should be converted). If you find these in plain xml, convert them to `<a href="URL">Friendly text</a>`.

    f. Some remarks have xrefs with normal characters converted to URL entities. The tool can convert URL entities back to normal characters. For example:

      |Before|After|
      |-----|---|
      | %23 | # |
      | %28 | ( |
      | %29 | ) |
      | %2C | , |

    g. Sometimes you will see `#ctor` being used to refer to a constructor method, which is correct in `xrefs`, but incorrect in see `crefs`. Simply change `#ctor` to the actual constructor name.

    h. The tool is capable of converting most markdown to xml when needed, but it may miss a few difficult cases. Make sure to find any unexpected markdown in xml sections (not wrapped by `<format>` and `CDATA`) and convert them to the appropriate xml tag. The elements that the tool will never convert to xml, and will preserve as markdown, are: `[!INCLUDE]`, `[!NOTE]`, `[!IMPORTANT]`, `[!TIP]`.

    i. The tool should be able to preserve double slash comments if they were placed on top of a public API, and will add the new triple slash comments separately. Make sure to verify that no important information is lost, especially the kind that is directed to developers/maintainers.

  > Examples: Refer to the following PRs:
  >
  > - https://github.com/dotnet/runtime/pull/48633
  > - https://github.com/dotnet/runtime/pull/48137

11. Open the `*.csproj` of your assembly's `src` folder, and add the `<GenerateDocumentationFile>true</GenerateDocumentationFile>` element to the first `<PropertyGroup>`, so documentation becomes mandatory for public APIs in this assembly from now on.

12. Build your project to find anything that may not be found visually.

13. Submit a PR to your source code repo (in this case, dotnet/runtime). Tag @carlossanlop, @safern, @gewarren and all the area co-owners.

    > **Important**: Every area owner is free to decide in which milestone they want to deliver their backported documentation.

14. Open the main issue tracking the [New documentation process plan](https://github.com/dotnet/runtime/issues/44969), and a checkbox with your assembly's name to the "Bring documentation from Docs to triple slash" section. You will check the checkbox after merging.

15. During review, make sure to:

    a. Verify that the backport did not replace any comments that were aimed to the code maintainers (information that API users wouldn't care about). If you find a case like this, make sure to bring those comments back as double slash comments (so they don't get sent back to MS Docs).

    b. Check that there is no markdown in the plain xml sections, or viceversa (the build may not catch some of these).

    c. No need to focus on the content of the documentation itself, but if you find a description you'd like to improve, this is the time to offer a suggested change.
