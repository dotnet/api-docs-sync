using DocsPortingTool.Docs;
using DocsPortingTool.TripleSlash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool
{
    public static class DocsPortingTool
    {
        private static readonly List<string> ModifiedFiles = new List<string>();
        private static readonly List<string> ModifiedAssemblies = new List<string>();
        private static readonly List<string> ModifiedContainers = new List<string>();
        private static readonly List<string> ModifiedAPIs = new List<string>();
        private static readonly List<string> ProblematicAPIs = new List<string>();
        private static readonly List<string> AddedExceptions = new List<string>();

        private static int TotalModifiedIndividualElements = 0;

        private static readonly TripleSlashCommentsContainer TripleSlashComments = new TripleSlashCommentsContainer();
        private static readonly DocsCommentsContainer DocsComments = new DocsCommentsContainer();

        public static void Start()
        {
            TripleSlashComments.CollectFiles();
            DocsComments.CollectFiles();

            PortMissingComments();

            PrintUndocumentedAPIs();
            PrintSummary();

            DocsComments.Save();
        }

        /// <summary>
        /// Iterates through all the found triple slash assembly files, finds all Docs types that belong to the same assembly, and looks for comments that can be ported.
        /// </summary>
        private static void PortMissingComments()
        {
            Log.Info("Looking for triple slash comments that can be ported...");
            foreach (TripleSlashMember tsMemberToPort in TripleSlashComments.Members)
            {
                if (tsMemberToPort.Name.StartsWith("T:"))
                {
                    PortMissingCommentsForContainers(tsMemberToPort);
                }
                else
                {
                    PortMissingCommentsForMembers(tsMemberToPort);
                }
            }
            Log.Line();
        }

        private static void PortMissingCommentsForContainers(TripleSlashMember tsMemberToPort)
        {
            foreach (DocsType dTypeToUpdate in DocsComments.Containers.Where(x => x.DocIdEscaped == tsMemberToPort.Name))
            {
                if (tsMemberToPort.Name == dTypeToUpdate.DocIdEscaped)
                {
                    TryPortMissingSummaryForAPI(tsMemberToPort, dTypeToUpdate);
                    TryPortMissingRemarksForAPI(tsMemberToPort, dTypeToUpdate);
                    TryPortMissingParamsForAPI(tsMemberToPort, dTypeToUpdate); // Some types (for example: delegates) have params
                }

                if (dTypeToUpdate.Changed)
                {
                    ModifiedContainers.AddIfNotExists(dTypeToUpdate.DocId);
                    ModifiedAssemblies.AddIfNotExists(tsMemberToPort.Assembly);
                    ModifiedFiles.AddIfNotExists(dTypeToUpdate.FilePath);
                }
            }
        }

        private static void PortMissingCommentsForMembers(TripleSlashMember tsMemberToPort)
        {
            foreach (DocsMember dMemberToUpdate in DocsComments.Members.Where(x => x.DocIdEscaped == tsMemberToPort.Name))
            {
                TryPortMissingSummaryForAPI(tsMemberToPort, dMemberToUpdate);
                TryPortMissingRemarksForAPI(tsMemberToPort, dMemberToUpdate);
                TryPortMissingParamsForAPI(tsMemberToPort, dMemberToUpdate);
                TryPortMissingTypeParamsForMember(tsMemberToPort, dMemberToUpdate);
                TryPortMissingExceptionsForMember(tsMemberToPort, dMemberToUpdate);

                // Properties sometimes don't have a <value> but have a <returns>
                if (dMemberToUpdate.MemberType == "Property")
                {
                    TryPortMissingPropertyForMember(tsMemberToPort, dMemberToUpdate);
                }
                else if (dMemberToUpdate.MemberType == "Method")
                {
                    TryPortMissingMethodForMember(tsMemberToPort, dMemberToUpdate);
                }

                if (dMemberToUpdate.Changed)
                {
                    ModifiedAPIs.AddIfNotExists(dMemberToUpdate.DocId);
                    ModifiedAssemblies.AddIfNotExists(tsMemberToPort.Assembly);
                    ModifiedFiles.AddIfNotExists(dMemberToUpdate.FilePath);
                }
            }
        }

        private static void TryPortMissingSummaryForAPI(TripleSlashMember tsMemberToPort, IDocsAPI dApiToUpdate)
        {
            // The triple slash member is referring to the base type (container) in the docs xml
            if (!IsEmpty(tsMemberToPort.Summary) && IsEmpty(dApiToUpdate.Summary))
            {
                // Any member can have an empty summary
                PrintModifiedMember($"{dApiToUpdate.Identifier} SUMMARY", dApiToUpdate.FilePath, tsMemberToPort.Name, dApiToUpdate.DocId, tsMemberToPort.Summary, dApiToUpdate.Summary);

                dApiToUpdate.Summary = tsMemberToPort.Summary;
                TotalModifiedIndividualElements++;
            }
        }

        private static void TryPortMissingRemarksForAPI(TripleSlashMember tsMemberToPort, IDocsAPI dApiToUpdate)
        {
            if (!IsEmpty(tsMemberToPort.Remarks) && IsEmpty(dApiToUpdate.Remarks))
            {
                // Any member can have an empty remark
                PrintModifiedMember($"{dApiToUpdate.Identifier} REMARKS", dApiToUpdate.FilePath, tsMemberToPort.Name, dApiToUpdate.DocId, tsMemberToPort.Remarks, dApiToUpdate.Remarks);

                dApiToUpdate.Remarks = tsMemberToPort.Remarks;
                TotalModifiedIndividualElements++;
            }
        }

        private static void TryPortMissingParamsForAPI(TripleSlashMember tsMemberToPort, IDocsAPI dApiToUpdate)
        {
            foreach (TripleSlashParam tsParam in tsMemberToPort.Params)
            {
                DocsParam dParam = dApiToUpdate.Params.FirstOrDefault(x => x.Name == tsParam.Name);
                bool created = false;

                if (dParam == null)
                {
                    ProblematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");

                    created = TryPromptParam(tsParam, dApiToUpdate, out dParam);
                }

                if (created || (!IsEmpty(tsParam.Value) && IsEmpty(dParam.Value)))
                {
                    string message = string.Format("PARAM ({0})", created ? "CREATED" : "MODIFIED");
                    PrintModifiedMember(message, dParam.ParentAPI.FilePath, tsParam.Name, dParam.Name, tsParam.Value, dParam.Value);

                    if (!created)
                    {
                        dParam.Value = tsParam.Value;
                    }
                    TotalModifiedIndividualElements++;
                }
            }

            
        }

        private static void TryPortMissingPropertyForMember(TripleSlashMember tsMemberToPort, DocsMember dMemberToUpdate)
        {
            string value = string.Empty;
            if (!IsEmpty(tsMemberToPort.Value) && IsEmpty(dMemberToUpdate.Value))
            {
                value = tsMemberToPort.Value;
            }
            if (!IsEmpty(tsMemberToPort.Returns) && IsEmpty(dMemberToUpdate.Value))
            {
                value = tsMemberToPort.Returns;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                PrintModifiedMember("PROPERTY", dMemberToUpdate.FilePath, tsMemberToPort.Name, dMemberToUpdate.DocId, tsMemberToPort.Returns, dMemberToUpdate.Value);

                dMemberToUpdate.Value = value;
                TotalModifiedIndividualElements++;
            }
        }

        private static void TryPortMissingMethodForMember(TripleSlashMember tsMemberToPort, DocsMember dMemberToUpdate)
        {
            if (!IsEmpty(tsMemberToPort.Returns) && IsEmpty(dMemberToUpdate.Returns))
            {
                // Methods that return void should NOT have any <returns> documentation
                if (tsMemberToPort.Returns != null && dMemberToUpdate.ReturnType == "System.Void")
                {
                    ProblematicAPIs.AddIfNotExists($"Returns=[{tsMemberToPort.Returns}] in Method=[{dMemberToUpdate.DocId}]");
                }
                else
                {
                    PrintModifiedMember("METHOD RETURN", dMemberToUpdate.FilePath, tsMemberToPort.Name, dMemberToUpdate.DocId, tsMemberToPort.Returns, dMemberToUpdate.Returns);

                    dMemberToUpdate.Returns = tsMemberToPort.Returns;
                    TotalModifiedIndividualElements++;
                }
            }
        }

        private static void TryPortMissingTypeParamsForMember(TripleSlashMember tsMemberToPort, DocsMember dMemberToUpdate)
        {
            foreach (TripleSlashTypeParam tsTypeParam in tsMemberToPort.TypeParams)
            {
                DocsTypeParam dTypeParam = dMemberToUpdate.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);
                bool created = false;

                if (dTypeParam == null)
                {
                    ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dMemberToUpdate.DocId}]");
                    dTypeParam = dMemberToUpdate.AddTypeParam(tsTypeParam.Name, XmlHelper.GetNodesInPlainText(tsTypeParam.XETypeParam));
                    created = true;
                }

                if (created || (!IsEmpty(tsTypeParam.Value) && IsEmpty(dTypeParam.Value)))
                {
                    string message = string.Format("TYPE PARAM ({0})", created ? "CREATED" : "MODIFIED");
                    PrintModifiedMember(message, dTypeParam.ParentAPI.FilePath, tsTypeParam.Name, dTypeParam.Name, tsTypeParam.Value, dTypeParam.Value);

                    if (!created)
                    {
                        dTypeParam.Value = tsTypeParam.Value;
                    }
                    TotalModifiedIndividualElements++;
                }
            }
        }

        private static void TryPortMissingExceptionsForMember(TripleSlashMember tsMemberToPort, DocsMember dMemberToUpdate)
        {
            if (!Configuration.SkipExceptions)
            {
                // Exceptions are a special case: If a new one is found in code, but does not exist in docs, the whole element needs to be added
                foreach (TripleSlashException tsException in tsMemberToPort.Exceptions)
                {
                    DocsException dException = dMemberToUpdate.Exceptions.FirstOrDefault(x => x.Cref == tsException.Cref);
                    bool created = false;

                    // First time adding the cref
                    if (dException == null)
                    {
                        AddedExceptions.AddIfNotExists($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                        dException = dMemberToUpdate.AddException(tsException.Cref, XmlHelper.GetNodesInPlainText(tsException.XEException));
                        created = true;
                    }
                    // If cref exists, check if the text has already been appended
                    else
                    {
                        XElement formattedException = tsException.XEException;
                        string value = XmlHelper.GetNodesInPlainText(formattedException);
                        if (!dException.Value.Contains(value))
                        {
                            AddedExceptions.AddIfNotExists($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                            dException.AppendException(value);
                            created = true;
                        }
                    }

                    if (created || (!IsEmpty(tsException.Value) && IsEmpty(dException.Value)))
                    {
                        string message = string.Format("EXCEPTION ({0})", created ? "CREATED" : "MODIFIED");
                        PrintModifiedMember(message, dException.ParentAPI.FilePath, tsException.Cref, dException.Cref, tsException.Value, dException.Value);

                        TotalModifiedIndividualElements++;
                    }
                }
            }

        }

        /// <summary>
        /// If a Param is found in a DocsType or a DocsMember that did not exist in the Triple Slash member, it's possible the param was unexpectedly saved in the triple slash comments with a different name, so the user gets prompted to look for it.
        /// </summary>
        /// <param name="tsParam">The problematic triple slash param object.</param>
        /// <param name="dMember">The docs member where the param lives.</param>
        /// <param name="dParam">The docs param that was found to not match the triple slash param.</param>
        /// <returns></returns>
        private static bool TryPromptParam(TripleSlashParam tsParam, IDocsAPI paramWrapper, out DocsParam dParam)
        {
            dParam = null;
            bool created = false;

            int option = -1;
            while (option == -1)
            {
                Log.Error($"Problem in param '{tsParam.Name}' in member '{paramWrapper.DocId}' in file '{paramWrapper.FilePath}'");
                Log.Error($"The param probably exists in code, but the exact name was not found in Docs. What would you like to do?");
                Log.Warning("    0 - Exit program.");
                Log.Info("    1 - Select the correct param from the existing ones detected in Docs for this member.");
                Log.Info("    2 - Overwrite the param name in the Docs file with the detected one (not recommended).");
                Log.Warning("        Note: Whatever your choice, make sure to double check the affected Docs file after the tool finishes executing.");
                Log.Working(false, "Your answer [0,1,2]: ");

                if (!int.TryParse(Console.ReadLine(), out option))
                {
                    Log.Error("Not a number. Try again.");
                    option = -1;
                }
                else
                {
                    switch (option)
                    {
                        case 0:
                            Log.Info("Goodbye!");
                            Environment.Exit(0);
                            break;
                        case 1:
                            {
                                int paramSelection = -1;
                                while (paramSelection == -1)
                                {
                                    Log.Info($"Params detected in member '{paramWrapper.DocId}':");
                                    Log.Warning("    0 - Exit program.");
                                    int paramCounter = 1;
                                    foreach (DocsParam param in paramWrapper.Params)
                                    {
                                        Log.Info($"    {paramCounter} - {param.Name}");
                                        paramCounter++;
                                    }

                                    Log.Working(false, $"Your answer to match param '{tsParam.Name}'? [0..{paramCounter - 1}]: ");

                                    if (!int.TryParse(Console.ReadLine(), out paramSelection))
                                    {
                                        Log.Error("Not a number. Try again.");
                                        paramSelection = -1;
                                    }
                                    else if (paramSelection < 0 || paramSelection >= paramCounter)
                                    {
                                        Log.Error("Invalid selection. Try again.");
                                        paramSelection = -1;
                                    }
                                    else if (paramSelection == 0)
                                    {
                                        Log.Info("Goodbye!");
                                        Environment.Exit(0);
                                    }
                                    else
                                    {
                                        dParam = paramWrapper.Params[paramSelection - 1];
                                        Log.Success($"Selected: {dParam.Name}");
                                    }
                                }

                                break;
                            }

                        case 2:
                            {
                                Log.Warning("Overwriting param...");
                                dParam = paramWrapper.SaveParam(tsParam.XEParam);
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
        /// Checks if the passed string is considered "empty" according to the Docs repo rules.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>True if empty, false otherwise.</returns>
        private static bool IsEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) || s == Configuration.ToBeAdded;
        }

        /// <summary>
        /// Standard formatted print message for a modified element.
        /// </summary>
        /// <param name="message">The friendly description of the modified API.</param>
        /// <param name="docsFile">The file where the modified API lives.</param>
        /// <param name="tripleSlashAPI">The API name in the triple slash file.</param>
        /// <param name="docsAPI">The API name in the Docs file.</param>
        /// <param name="tripleSlashValue">The value that was found in the triple slash file.</param>
        /// <param name="docsValue">The value that was found in the Docs file.</param>
        private static void PrintModifiedMember(string message, string docsFile, string tripleSlashAPI, string docsAPI, string tripleSlashValue, string docsValue)
        {
            Log.Warning("    File: {0}", docsFile);
            Log.Warning("        {0}", message);
            Log.Warning("            Code: {0} => {1}", tripleSlashAPI, tripleSlashValue);
            Log.Warning("            Docs: {0} => {1}", docsAPI, docsValue);
            Log.Info("---------------------------------------------------");
        }

        /// <summary>
        /// Prints all the undocumented APIs.
        /// </summary>
        private static void PrintUndocumentedAPIs()
        {
            if (Configuration.PrintUndoc)
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
                int memberValues = 0;
                int memberReturns = 0;
                int memberParams = 0;
                int memberTypeParams = 0;
                int exceptions = 0;

                Log.Info("Undocumented APIs:");
                foreach (DocsType docsType in DocsComments.Containers)
                {
                    bool undocAPI = false;
                    if (IsEmpty(docsType.Summary))
                    {
                        TryPrintType(ref undocAPI, docsType.DocId);
                        Log.Error($"        Container Summary: {docsType.Summary}");
                        typeSummaries++;
                    }
                }

                foreach (DocsMember member in DocsComments.Members)
                {
                    bool undocMember = false;

                    if (IsEmpty(member.Summary))
                    {
                        TryPrintMember(ref undocMember, member.DocId);

                        Log.Error($"        Member Summary: {member.Summary}");
                        memberSummaries++;
                    }

                    if (member.MemberType == "Property")
                    {
                        if (member.Value == Configuration.ToBeAdded)
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Property Value: {member.Value}");
                            memberValues++;
                        }
                    }
                    else if (member.MemberType == "Method")
                    {
                        if (member.Returns == Configuration.ToBeAdded)
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Method Returns: {member.Returns}");
                            memberReturns++;
                        }
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
                Log.Info($" Undocumented method returns: {memberReturns}");
                Log.Info($" Undocumented property values: {memberValues}");
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
    }
}
