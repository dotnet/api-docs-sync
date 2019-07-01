using DocsPortingTool.Docs;
using DocsPortingTool.TripleSlash;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocsPortingTool
{
    /// <summary>
    /// Provides generic extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds a string to a list of strings if the element is not there yet. The method makes sure to escape unexpected curly brackets to prevent formatting exceptions.
        /// </summary>
        /// <param name="list">A string list.</param>
        /// <param name="element">A string.</param>
        public static void AddIfNotExists(this List<string> list, string element)
        {
            string cleanedElement = element.Replace("{", "{{").Replace("}", "}}");
            if (!list.Contains(cleanedElement))
            {
                list.Add(cleanedElement);
            }
        }
    }

    class Program
    {
        #region Private fields

        private static readonly DocsCommentsContainer docsComments = new DocsCommentsContainer();
        private static readonly TripleSlashCommentsContainer tripleSlashComments = new TripleSlashCommentsContainer();

        private static readonly List<string> modifiedFiles = new List<string>();
        private static readonly List<string> modifiedAssemblies = new List<string>();
        private static readonly List<string> modifiedContainers = new List<string>();
        private static readonly List<string> modifiedAPIs = new List<string>();
        private static readonly List<string> problematicAPIs = new List<string>();
        private static readonly List<string> addedExceptions = new List<string>();
        private static int totalModifiedIndividualElements = 0;

        #endregion

        #region Public fields


        #endregion

        #region Public methods

        public static void Main(string[] args)
        {
            CLArgumentVerifier.Verify(args);

            docsComments.Load();
            tripleSlashComments.Load();

            PortMissingComments();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Iterates through all the found triple slash assembly files, finds all Docs types that belong to the same assembly, and looks for comments that can be ported.
        /// </summary>
        private static void PortMissingComments()
        {
            Log.Info("Looking for triple slash comments that can be ported...");
            foreach (TripleSlashAssembly tsAssembly in tripleSlashComments.Assemblies)
            {
                foreach (DocsType dType in docsComments.Types.Where(x =>
                    x.AssemblyInfos.Count(y => y.AssemblyName == tsAssembly.Name) > 0)
                )
                {
                    if (TryPortMissingCommentsForAssembly(tsAssembly, dType))
                    {
                        modifiedAssemblies.AddIfNotExists(tsAssembly.Name);
                        modifiedFiles.AddIfNotExists(dType.FilePath);

                        // SaveXml only saves if CLArgumentVerifier.Save is true
                        dType.SaveXml();
                    }
                }
            }

            PrintSummary();
    }

        /// <summary>
        /// Will compare all the member APIs of the passed triple slash assembly with the member APIs of the passed Docs type, and will port any missing comment or add any unexpectedly missing element.
        /// </summary>
        /// <param name="tsAssembly">A triple slash assembly.</param>
        /// <param name="dType">A docs type.</param>
        /// <returns>True if the passed docs type was modified in any way, false otherwise.</returns>
        private static bool TryPortMissingCommentsForAssembly(TripleSlashAssembly tsAssembly, DocsType dType)
        {
            bool modified = false;

            foreach (TripleSlashMember tsMember in tsAssembly.Members)
            {
                if (tsMember.Name == dType.DocId)
                {
                    // The triple slash member is referring to the base type (container) in the docs xml
                    if (!IsEmpty(tsMember.Summary) && IsEmpty(dType.Summary))
                    {
                        PrintModifiedMember("TYPE SUMMARY", dType.FilePath, tsMember.Name, dType.DocId, tsMember.Summary, dType.Summary);

                        dType.Summary = tsMember.Summary;
                        totalModifiedIndividualElements++;
                        modified = true;
                    }

                    if (!IsEmpty(tsMember.Remarks) && IsEmpty(dType.Remarks))
                    {
                        PrintModifiedMember("TYPE REMARKS", dType.FilePath, tsMember.Name, dType.DocId, tsMember.Remarks, dType.Remarks);

                        dType.Remarks = tsMember.Remarks;
                        totalModifiedIndividualElements++;
                        modified = true;
                    }

                    if (modified)
                    {
                        modifiedContainers.AddIfNotExists(dType.DocId);
                    }
                }
                else
                {
                    // The triple slash member is referring to internal members of the Docs type
                    foreach (DocsMember dMember in dType.Members.FindAll(dx => dx.DocId == tsMember.Name))
                    {
                        if (!IsEmpty(tsMember.Summary) && IsEmpty(dMember.Summary))
                        {
                            // Any member can have an empty summary
                            PrintModifiedMember("MEMBER SUMMARY", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Summary, dMember.Summary);

                            dMember.Summary = tsMember.Summary;
                            totalModifiedIndividualElements++;
                            modified = true;
                        }

                        if (!IsEmpty(tsMember.Remarks) && IsEmpty(dMember.Remarks))
                        {
                            // Any member can have an empty remark
                            PrintModifiedMember("MEMBER REMARKS", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Remarks, dMember.Remarks);

                            dMember.Remarks = tsMember.Remarks;
                            totalModifiedIndividualElements++;
                            modified = true;
                        }

                        // Properties and method returns save their values in different locations
                        if (dMember.MemberType == "Property")
                        {
                            if (!IsEmpty(tsMember.Returns) && IsEmpty(dMember.Value))
                            {
                                PrintModifiedMember("PROPERTY", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Returns, dMember.Value);

                                dMember.Value = tsMember.Returns;
                                totalModifiedIndividualElements++;
                                modified = true;
                            }
                        }
                        else if (dMember.MemberType == "Method")
                        {
                            if (!IsEmpty(tsMember.Returns) && IsEmpty(dMember.Returns))
                            {
                                if (tsMember.Returns != null && dMember.ReturnType == "System.Void")
                                {
                                    problematicAPIs.AddIfNotExists($"Returns=[{tsMember.Returns}] in Method=[{dMember.DocId}]");
                                }
                                else
                                {
                                    PrintModifiedMember("METHOD RETURN", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Returns, dMember.Returns);

                                    dMember.Returns = tsMember.Returns;
                                    totalModifiedIndividualElements++;
                                    modified = true;
                                }
                            }
                        }

                        // Triple slash params may cause errors if they are missing in the code side
                        foreach (TripleSlashParam tsParam in tsMember.Params)
                        {
                            DocsParam dParam = dMember.Params.FirstOrDefault(x => x.Name == tsParam.Name);
                            bool created = false;

                            if (dParam == null)
                            {
                                problematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dMember.DocId}]");

                                created = TryPromptParam(tsParam, dMember, out dParam);
                            }

                            if (created || (!IsEmpty(tsParam.Value) && IsEmpty(dParam.Value)))
                            {
                                PrintModifiedMember(string.Format("PARAM ({0})", created ? "CREATED" : "MODIFIED"), dParam.FilePath, tsParam.Name, dParam.Name, tsParam.Value, dParam.Value);

                                if (!created)
                                {
                                    dParam.Value = tsParam.Value;
                                }
                                totalModifiedIndividualElements++;
                                modified = true;
                            }
                        }

                        // Exceptions are a special case: If a new one is found in code, but does not exist in docs, the whole element needs to be added
                        foreach (TripleSlashException tsException in tsMember.Exceptions)
                        {
                            DocsException dException = dMember.Exceptions.FirstOrDefault(x => x.Cref.EndsWith(tsException.Cref));
                            bool created = false;

                            if (dException == null)
                            {
                                dException = dMember.SaveException(tsException.XEException);
                                addedExceptions.AddIfNotExists($"{dException.Cref} in {dMember.DocId}");
                                created = true;
                            }

                            if (created || (!IsEmpty(tsException.Value) && IsEmpty(dException.Value)))
                            {
                                PrintModifiedMember(string.Format("EXCEPTION ({0})", created ? "CREATED" : "MODIFIED"), dException.FilePath, tsException.Cref, dException.Cref, tsException.Value, dException.Value);

                                if (!created)
                                {
                                    dException.Value = tsException.Value;
                                }
                                totalModifiedIndividualElements++;
                                modified = true;
                            }
                        }

                        foreach (TripleSlashTypeParam tsTypeParam in tsMember.TypeParams)
                        {
                            DocsTypeParam dTypeParam = dMember.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);
                            bool created = false;

                            if (dTypeParam == null)
                            {
                                problematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dMember.DocId}]");
                                dTypeParam = dMember.SaveTypeParam(tsTypeParam.XETypeParam);
                                created = true;
                            }

                            if (created || (!IsEmpty(tsTypeParam.Value) && IsEmpty(dTypeParam.Value)))
                            {
                                PrintModifiedMember(string.Format("TYPE PARAM ({0})", created ? "CREATED" : "MODIFIED"), dTypeParam.FilePath, tsTypeParam.Name, dTypeParam.Name, tsTypeParam.Value, dTypeParam.Value);

                                if (!created)
                                {
                                    dTypeParam.Value = tsTypeParam.Value;
                                }
                                totalModifiedIndividualElements++;
                                modified = true;
                            }
                        }


                        if (modified)
                        {
                            modifiedAPIs.AddIfNotExists(dMember.DocId);
                        }
                    }
                }
            }

            return modified;
        }

        /// <summary>
        /// Checks if the passed string is considered "empty" according to the Docs repo rules.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>True if empty, false otherwise.</returns>
        private static bool IsEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) || s == "To be added.";
        }

        /// <summary>
        /// Standard formatted print message for a modified element.
        /// </summary>
        /// <param name="descriptionAPI">The friendly description of the modified API.</param>
        /// <param name="docsFile">The file where the modified API lives.</param>
        /// <param name="codeAPI">The API name in the triple slash file.</param>
        /// <param name="docsAPI">The API name in the Docs file.</param>
        /// <param name="codeValue">The value that was found in the triple slash file.</param>
        /// <param name="docsValue">The value that was found in the Docs file.</param>
        private static void PrintModifiedMember(string descriptionAPI, string docsFile, string codeAPI, string docsAPI, string codeValue, string docsValue)
        {
            Log.Warning("    File: {0}", docsFile);
            Log.Warning("        {0}", descriptionAPI);
            Log.Warning("            Code: {0} => {1}", codeAPI, codeValue);
            Log.Warning("            Docs: {0} => {1}", docsAPI, docsValue);
            Log.Info("---------------------------------------------------");
        }

        /// <summary>
        /// If a Param is found in the DocsMember that did not exist in the Triple Slash member, it's possible the param was unexpectedly saved in the triple slash comments with a different name, so the user gets prompted to look for it.
        /// </summary>
        /// <param name="tsParam">The problematic triple slash param object.</param>
        /// <param name="dMember">The docs member where the param lives.</param>
        /// <param name="dParam">The docs param that was found to not match the triple slash param.</param>
        /// <returns></returns>
        private static bool TryPromptParam(TripleSlashParam tsParam, DocsMember dMember, out DocsParam dParam)
        {
            dParam = null;
            bool created = false;

            int option = -1;
            while (option == -1)
            {
                Log.Warning("The param '{0}' probably exists, but the src name does not match the ref name in corefx. What would you like to do?", tsParam.Name);
                Log.Warning("    1 - Type the correct name and attempt to detect it.");
                Log.Warning("    2 - Add the newly detected param to the dotnet-api-docs xml.");
                Log.Warning("        Note: Whatever your choice, make sure to double check the dotnet-api-docs xml file after the tool finishes executing.");
                Log.Info(false, "Your answer [1,2]: ");

                if (!int.TryParse(Console.ReadLine(), out option))
                {
                    Log.Error("Invalid selection. Try again.");
                    option = -1;
                }
                else
                {
                    switch (option)
                    {
                        case 1:
                            {
                                string newName = string.Empty;
                                while (string.IsNullOrWhiteSpace(newName))
                                {
                                    Log.Info(false, "Type the new name: ");
                                    newName = Console.ReadLine().Trim();
                                    if (string.IsNullOrWhiteSpace(newName))
                                    {
                                        Log.Error("Invalid selection. Try again.");
                                    }
                                    else if (newName == tsParam.Name)
                                    {
                                        Log.Error("You specified the same name. Try again.");
                                        newName = string.Empty;
                                    }
                                    else
                                    {
                                        dParam = dMember.Params.FirstOrDefault(x => x.Name == newName);
                                        if (dParam == null)
                                        {
                                            Log.Error("Could not find the param with the selected name. Try again.");
                                            newName = string.Empty;
                                        }
                                        else
                                        {
                                            Log.Success("Found the param with the selected name!");
                                        }
                                    }
                                }
                                break;
                            }

                        case 2:
                            {
                                dParam = dMember.SaveParam(tsParam.XEParam);
                                created = true;
                                break;
                            }

                        default:
                            {
                                Log.Error("Invalid selection. Try again.");
                                option = -1;
                                break;
                            }
                    }
                }
            }

            return created;
        }

        /// <summary>
        /// Prints a final summary of the execution findings.
        /// </summary>
        private static void PrintSummary()
        {
            Log.Line();
            Log.Success("---------");
            Log.Success("FINISHED!");
            Log.Success("---------");

            Log.Line();
            Log.Info($"Total modified files: {modifiedFiles.Count}");
            foreach (string file in modifiedFiles)
            {
                Log.Warning($"    - {file}");
            }

            Log.Line();
            Log.Info($"Total modified assemblies: {modifiedAssemblies.Count}");
            foreach (string assembly in modifiedAssemblies)
            {
                Log.Warning($"    - {assembly}");
            }

            Log.Line();
            Log.Info($"Total modified containers: {modifiedContainers.Count}");
            foreach (string container in modifiedContainers)
            {
                Log.Warning($"    - {container}");
            }

            Log.Line();
            Log.Info($"Total modified APIs: {modifiedAPIs.Count}");
            foreach (string api in modifiedAPIs)
            {
                Log.Warning($"    - {api}");
            }

            Log.Line();
            Log.Info($"Total problematic APIs: {problematicAPIs.Count}");
            foreach (string api in problematicAPIs)
            {
                Log.Warning($"    - {api}");
            }

            Log.Line();
            Log.Info($"Total added exceptions: {addedExceptions.Count}");
            foreach (string exception in addedExceptions)
            {
                Log.Warning($"    - {exception}");
            }

            Log.Line();
            Log.Info(false, "Total modified individual elements: ");
            Log.Warning($"{totalModifiedIndividualElements}");
        }

        #endregion
    }
}
