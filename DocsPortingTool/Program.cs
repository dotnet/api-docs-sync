using DocsPortingTool.Docs;
using DocsPortingTool.TripleSlash;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocsPortingTool
{
    class Program
    {
        #region Private fields

        private static readonly DocsCommentsContainer docsComments = new DocsCommentsContainer();
        private static readonly TripleSlashCommentsContainer tripleSlashComments = new TripleSlashCommentsContainer();

        private static readonly List<string> ModifiedFiles = new List<string>();
        private static readonly List<string> ModifiedAssemblies = new List<string>();
        private static readonly List<string> ModifiedContainers = new List<string>();
        private static readonly List<string> ModifiedAPIs = new List<string>();
        private static readonly List<string> ProblematicAPIs = new List<string>();
        private static readonly List<string> AddedExceptions = new List<string>();
        private static int TotalModifiedIndividualElements = 0;

        #endregion

        #region Public fields


        #endregion

        #region Public methods

        public static void Main(string[] args)
        {
            CLArgumentVerifier.Verify(args);

            tripleSlashComments.Load();
            docsComments.Load();

            PortMissingComments();

            PrintUndocumentedAPIs();
            PrintSummary();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Iterates through all the found triple slash assembly files, finds all Docs types that belong to the same assembly, and looks for comments that can be ported.
        /// </summary>
        private static void PortMissingComments()
        {
            Log.Info("Looking for triple slash comments that can be ported...");
            foreach (TripleSlashMember tsMember in tripleSlashComments.Members)
            {
                if (tsMember.Name.StartsWith("T:"))
                {
                    foreach (DocsType dType in docsComments.Containers.Where(x => x.DocId == tsMember.Name))
                    {
                        if (TryPortMissingCommentsForContainer(tsMember, dType))
                        {
                            ModifiedAssemblies.AddIfNotExists(tsMember.Assembly);
                            ModifiedFiles.AddIfNotExists(dType.FilePath);

                            dType.SaveXml();
                        }
                    }
                }
                else
                {
                    foreach (DocsMember dMember in docsComments.Members.Where(x => x.DocId == tsMember.Name))
                    {
                        if (TryPortMissingCommentsForMember(tsMember, dMember))
                        {
                            ModifiedAssemblies.AddIfNotExists(tsMember.Assembly);
                            ModifiedFiles.AddIfNotExists(dMember.FilePath);

                            dMember.SaveXml();
                        }
                    }
                }
            }

            Log.Line();
        }

        private static bool TryPortMissingCommentsForContainer(TripleSlashMember tsMember, DocsType dType)
        {
            bool modified = false;

            if (tsMember.Name == dType.DocId)
            {
                // The triple slash member is referring to the base type (container) in the docs xml
                if (!IsEmpty(tsMember.Summary) && IsEmpty(dType.Summary))
                {
                    PrintModifiedMember("CONTAINER SUMMARY", dType.FilePath, tsMember.Name, dType.DocId, tsMember.Summary, dType.Summary);

                    dType.Summary = tsMember.Summary;
                    TotalModifiedIndividualElements++;
                    modified = true;
                }

                if (!IsEmpty(tsMember.Remarks) && IsEmpty(dType.Remarks))
                {
                    PrintModifiedMember("CONTAINER REMARKS", dType.FilePath, tsMember.Name, dType.DocId, tsMember.Remarks, dType.Remarks);

                    dType.Remarks = tsMember.Remarks;
                    TotalModifiedIndividualElements++;
                    modified = true;
                }

                if (modified)
                {
                    ModifiedContainers.AddIfNotExists(dType.DocId);
                }
            }

            if (modified)
            {
                ModifiedAPIs.AddIfNotExists(dType.DocId);
            }

            return modified;
        }

        private static bool TryPortMissingCommentsForMember(TripleSlashMember tsMember, DocsMember dMember)
        {
            bool modified = false;

            if (!IsEmpty(tsMember.Summary) && IsEmpty(dMember.Summary))
            {
                // Any member can have an empty summary
                PrintModifiedMember("MEMBER SUMMARY", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Summary, dMember.Summary);

                dMember.Summary = tsMember.Summary;
                TotalModifiedIndividualElements++;
                modified = true;
            }

            if (!IsEmpty(tsMember.Remarks) && IsEmpty(dMember.Remarks))
            {
                // Any member can have an empty remark
                PrintModifiedMember("MEMBER REMARKS", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Remarks, dMember.Remarks);

                dMember.Remarks = tsMember.Remarks;
                TotalModifiedIndividualElements++;
                modified = true;
            }

            // Properties and method returns save their values in different locations
            if (dMember.MemberType == "Property")
            {
                if (!IsEmpty(tsMember.Returns) && IsEmpty(dMember.Value))
                {
                    PrintModifiedMember("PROPERTY", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Returns, dMember.Value);

                    dMember.Value = tsMember.Returns;
                    TotalModifiedIndividualElements++;
                    modified = true;
                }
            }
            else if (dMember.MemberType == "Method")
            {
                if (!IsEmpty(tsMember.Returns) && IsEmpty(dMember.Returns))
                {
                    if (tsMember.Returns != null && dMember.ReturnType == "System.Void")
                    {
                        ProblematicAPIs.AddIfNotExists($"Returns=[{tsMember.Returns}] in Method=[{dMember.DocId}]");
                    }
                    else
                    {
                        PrintModifiedMember("METHOD RETURN", dMember.FilePath, tsMember.Name, dMember.DocId, tsMember.Returns, dMember.Returns);

                        dMember.Returns = tsMember.Returns;
                        TotalModifiedIndividualElements++;
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
                    ProblematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dMember.DocId}]");

                    created = TryPromptParam(tsParam, dMember, out dParam);
                }

                if (created || (!IsEmpty(tsParam.Value) && IsEmpty(dParam.Value)))
                {
                    PrintModifiedMember(string.Format("PARAM ({0})", created ? "CREATED" : "MODIFIED"), dParam.FilePath, tsParam.Name, dParam.Name, tsParam.Value, dParam.Value);

                    if (!created)
                    {
                        dParam.Value = tsParam.Value;
                    }
                    TotalModifiedIndividualElements++;
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
                    AddedExceptions.AddIfNotExists($"{dException.Cref} in {dMember.DocId}");
                    created = true;
                }

                if (created || (!IsEmpty(tsException.Value) && IsEmpty(dException.Value)))
                {
                    PrintModifiedMember(string.Format("EXCEPTION ({0})", created ? "CREATED" : "MODIFIED"), dException.FilePath, tsException.Cref, dException.Cref, tsException.Value, dException.Value);

                    if (!created)
                    {
                        dException.Value = tsException.Value;
                    }
                    TotalModifiedIndividualElements++;
                    modified = true;
                }
            }

            foreach (TripleSlashTypeParam tsTypeParam in tsMember.TypeParams)
            {
                DocsTypeParam dTypeParam = dMember.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);
                bool created = false;

                if (dTypeParam == null)
                {
                    ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dMember.DocId}]");
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
                    TotalModifiedIndividualElements++;
                    modified = true;
                }
            }

            if (modified)
            {
                ModifiedAPIs.AddIfNotExists(dMember.DocId);
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
                Log.Error("Problem in member {0} in file {1}!", dMember.DocId, dMember.FilePath);
                Log.Warning("The param from triple slash called '{0}' probably exists in code, but the name was not found in Docs. What would you like to do?", tsParam.Name);
                Log.Warning("    1 - Type the correct name as it shows up in Docs.");
                Log.Warning("    2 - Add the newly detected param to the Docs file (not recommended).");
                Log.Warning("        Note: Whatever your choice, make sure to double check the affected Docs file after the tool finishes executing.");
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
        /// Prints all the undocumented APIs.
        /// </summary>
        private static void PrintUndocumentedAPIs()
        {
            if (CLArgumentVerifier.PrintUndoc)
            {
                Log.Line();
                Log.Success("-----------------");
                Log.Success("UNDOCUMENTED APIS");
                Log.Success("-----------------");

                Log.Line();

                static void TryPrintType(ref bool undocAPI, string typeDocId)
                {
                    if (!undocAPI)
                    {
                        Log.Info("    Type: {0}", typeDocId);
                        undocAPI = true;
                    }
                };

                static void TryPrintMember(ref bool undocMember, string memberDocId)
                {
                    if (!undocMember)
                    {
                        Log.Info("            {0}", memberDocId);
                        undocMember = true;
                    }
                };

                int typeSummaries = 0;
                int memberSummaries = 0;
                int memberReturns = 0;
                int memberParams = 0;
                int memberTypeParams = 0;
                int exceptions = 0;

                Log.Info("Undocumented APIs:");
                foreach (DocsType docsType in docsComments.Containers)
                {
                    bool undocAPI = false;
                    if (IsEmpty(docsType.Summary))
                    {
                        TryPrintType(ref undocAPI, docsType.DocId);
                        Log.Error($"        Container Summary: {docsType.Summary}");
                        typeSummaries++;
                    }
                }

                foreach (DocsMember member in docsComments.Members)
                {
                    bool undocMember = false;

                    if (IsEmpty(member.Summary))
                    {
                        TryPrintMember(ref undocMember, member.DocId);

                        Log.Error($"        Member Summary: {member.Summary}");
                        memberSummaries++;
                    }
                    if (member.Returns == "To be added.")
                    {
                        TryPrintMember(ref undocMember, member.DocId);

                        Log.Error($"        Member Returns: {member.Returns}");
                        memberReturns++;
                    }
                    foreach (DocsParam param in member.Params)
                    {
                        if (IsEmpty(param.Value))
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Param: {param.Name}: {param.Value}");
                            memberParams++;
                        }
                    }

                    foreach (DocsTypeParam typeParam in member.TypeParams)
                    {
                        if (IsEmpty(typeParam.Value))
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Type Param: {typeParam.Name}: {typeParam.Value}");
                            memberTypeParams++;
                        }
                    }

                    foreach (DocsException exception in member.Exceptions)
                    {
                        if (IsEmpty(exception.Value))
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Exception: {exception.Cref}: {exception.Value}");
                            exceptions++;
                        }
                    }
                }

                Log.Info($" Undocumented type summaries: {typeSummaries}");
                Log.Info($" Undocumented member summaries: {memberSummaries}");
                Log.Info($" Undocumented member returns: {memberReturns}");
                Log.Info($" Undocumented member params: {memberParams}");
                Log.Info($" Undocumented member type params: {memberTypeParams}");
                Log.Info($" Undocumented exceptions: {exceptions}");

                Log.Line();
            }
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
            Log.Info($"Total modified files: {ModifiedFiles.Count}");
            foreach (string file in ModifiedFiles)
            {
                Log.Warning($"    - {file}");
            }

            Log.Line();
            Log.Info($"Total modified assemblies: {ModifiedAssemblies.Count}");
            foreach (string assembly in ModifiedAssemblies)
            {
                Log.Warning($"    - {assembly}");
            }

            Log.Line();
            Log.Info($"Total modified containers: {ModifiedContainers.Count}");
            foreach (string container in ModifiedContainers)
            {
                Log.Warning($"    - {container}");
            }

            Log.Line();
            Log.Info($"Total modified APIs: {ModifiedAPIs.Count}");
            foreach (string api in ModifiedAPIs)
            {
                Log.Warning($"    - {api}");
            }

            Log.Line();
            Log.Info($"Total problematic APIs: {ProblematicAPIs.Count}");
            foreach (string api in ProblematicAPIs)
            {
                Log.Warning($"    - {api}");
            }

            Log.Line();
            Log.Info($"Total added exceptions: {AddedExceptions.Count}");
            foreach (string exception in AddedExceptions)
            {
                Log.Warning($"    - {exception}");
            }

            Log.Line();
            Log.Info(false, "Total modified individual elements: ");
            Log.Warning($"{TotalModifiedIndividualElements}");
        }

        #endregion
    }
}
