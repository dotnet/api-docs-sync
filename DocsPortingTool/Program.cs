using DocsPortingTool.Docs;
using DocsPortingTool.TripleSlash;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool
{
    class Program
    {
        #region Private fields

        private static readonly List<string> ModifiedFiles = new List<string>();
        private static readonly List<string> ModifiedAssemblies = new List<string>();
        private static readonly List<string> ModifiedContainers = new List<string>();
        private static readonly List<string> ModifiedAPIs = new List<string>();
        private static readonly List<string> ProblematicAPIs = new List<string>();
        private static readonly List<string> AddedExceptions = new List<string>();

        private static int TotalModifiedIndividualElements = 0;

        private static TripleSlashCommentsContainer TripleSlashComments = new TripleSlashCommentsContainer();
        private static DocsCommentsContainer DocsComments = new DocsCommentsContainer();

        #endregion

        #region Public methods

        public static void Main(string[] args)
        {
            CLArgumentVerifier.GetConfiguration(args);

            foreach (FileInfo fileInfo in GetTripleSlashXmlFiles())
            {
                TripleSlashComments.LoadFile(fileInfo, printSuccess: true);
            }

            foreach (FileInfo fileInfo in GetDocsCommentsXmlFiles())
            {
                DocsComments.LoadFile(fileInfo);
            }

            PortMissingComments();
            DocsComments.Save();

            PrintUndocumentedAPIs();
            PrintSummary();
        }

        #endregion

        #region Private methods

        private static List<FileInfo> GetDocsCommentsXmlFiles()
        {
            Log.Info("Looking for Docs xml files...");

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo subDir in Configuration.DirDocsXml.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                {
                    foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                    {
                        if (Configuration.HasAllowedAssemblyPrefix(subDir.Name))
                        {
                            fileInfos.Add(fileInfo);
                        }
                    }
                }
            }

            Log.Success("Finished looking for Docs xml files");

            return fileInfos;
        }

        private static List<FileInfo> GetTripleSlashXmlFiles()
        {
            Log.Info("Looking for triple slash xml files...");

            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo dirInfo in Configuration.DirsTripleSlashXmls)
            {
                // 1) Find all the xml files inside the subdirectories within the triple slash xml directory
                foreach (DirectoryInfo subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (!Configuration.ForbiddenDirectories.Contains(subDir.Name) && !subDir.Name.EndsWith(".Tests"))
                    {
                        foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                        {
                            if (Configuration.HasAllowedAssemblyPrefix(fileInfo.Name))
                            {
                                fileInfos.Add(fileInfo);
                            }
                        }
                    }
                }

                // 2) Find all the xml files in the top directory
                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
                {
                    if (Configuration.HasAllowedAssemblyPrefix(fileInfo.Name))
                    {
                        fileInfos.Add(fileInfo);
                    }
                }
            }

            Log.Success("Finished looking for triple slash xml files.");

            return fileInfos;
        }

        /// <summary>
        /// Iterates through all the found triple slash assembly files, finds all Docs types that belong to the same assembly, and looks for comments that can be ported.
        /// </summary>
        private static void PortMissingComments()
        {
            Log.Info("Looking for triple slash comments that can be ported...");
            foreach (TripleSlashMember tsMember in TripleSlashComments.Members)
            {
                if (tsMember.Name.StartsWith("T:"))
                {
                    foreach (DocsType dType in DocsComments.Containers.Where(x => x.DocId == tsMember.Name))
                    {
                        PortMissingCommentsForContainer(tsMember, dType);
                    }
                }
                else
                {
                    foreach (DocsMember dMember in DocsComments.Members.Where(x => x.DocId == tsMember.Name))
                    {
                        PortMissingCommentsForMember(tsMember, dMember);
                    }
                }
            }
            Log.Line();
        }

        private static void PortMissingCommentsForContainer(TripleSlashMember tsMemberToPort, DocsType dTypeToUpdate)
        {
            if (tsMemberToPort.Name == dTypeToUpdate.DocId)
            {
                // The triple slash member is referring to the base type (container) in the docs xml
                if (!IsEmpty(tsMemberToPort.Summary) && IsEmpty(dTypeToUpdate.Summary))
                {
                    PrintModifiedMember("CONTAINER SUMMARY", dTypeToUpdate.FilePath, tsMemberToPort.Name, dTypeToUpdate.DocId, tsMemberToPort.Summary, dTypeToUpdate.Summary);

                    dTypeToUpdate.Summary = tsMemberToPort.Summary;
                    TotalModifiedIndividualElements++;
                }

                if (!IsEmpty(tsMemberToPort.Remarks) && IsEmpty(dTypeToUpdate.Remarks))
                {
                    PrintModifiedMember("CONTAINER REMARKS", dTypeToUpdate.FilePath, tsMemberToPort.Name, dTypeToUpdate.DocId, tsMemberToPort.Remarks, dTypeToUpdate.Remarks);

                    dTypeToUpdate.Remarks = tsMemberToPort.Remarks;
                    TotalModifiedIndividualElements++;
                }

                // Some types (for example: delegates) have params
                foreach (TripleSlashParam tsParam in tsMemberToPort.Params)
                {
                    DocsParam dParam = dTypeToUpdate.Params.FirstOrDefault(x => x.Name == tsParam.Name);
                    bool created = false;

                    if (dParam == null)
                    {
                        ProblematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dTypeToUpdate.DocId}]");

                        created = TryPromptParam(tsParam, dTypeToUpdate, out dParam);
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

                if (dTypeToUpdate.Changed)
                {
                    ModifiedContainers.AddIfNotExists(dTypeToUpdate.DocId);
                }
            }

            if (dTypeToUpdate.Changed)
            {
                ModifiedAPIs.AddIfNotExists(dTypeToUpdate.DocId);
                ModifiedAssemblies.AddIfNotExists(tsMemberToPort.Assembly);
                ModifiedFiles.AddIfNotExists(dTypeToUpdate.FilePath);
            }
        }

        private static void PortMissingCommentsForMember(TripleSlashMember tsMemberToPort, DocsMember dMemberToUpdate)
        {
            if (!IsEmpty(tsMemberToPort.Summary) && IsEmpty(dMemberToUpdate.Summary))
            {
                // Any member can have an empty summary
                PrintModifiedMember("MEMBER SUMMARY", dMemberToUpdate.FilePath, tsMemberToPort.Name, dMemberToUpdate.DocId, tsMemberToPort.Summary, dMemberToUpdate.Summary);

                dMemberToUpdate.Summary = tsMemberToPort.Summary;
                TotalModifiedIndividualElements++;
            }

            if (!IsEmpty(tsMemberToPort.Remarks) && IsEmpty(dMemberToUpdate.Remarks))
            {
                // Any member can have an empty remark
                PrintModifiedMember("MEMBER REMARKS", dMemberToUpdate.FilePath, tsMemberToPort.Name, dMemberToUpdate.DocId, tsMemberToPort.Remarks, dMemberToUpdate.Remarks);

                dMemberToUpdate.Remarks = tsMemberToPort.Remarks;
                TotalModifiedIndividualElements++;
            }

            // Properties sometimes don't have a <value> but have a <returns>
            if (dMemberToUpdate.MemberType == "Property")
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
            else if (dMemberToUpdate.MemberType == "Method")
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

            // Triple slash params may cause errors if they are missing in the code side
            foreach (TripleSlashParam tsParam in tsMemberToPort.Params)
            {
                DocsParam dParam = dMemberToUpdate.Params.FirstOrDefault(x => x.Name == tsParam.Name);
                bool created = false;

                if (dParam == null)
                {
                    ProblematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dMemberToUpdate.DocId}]");

                    created = TryPromptParam(tsParam, dMemberToUpdate, out dParam);
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
                        dException = dMemberToUpdate.AddException(tsException.XEException);
                        created = true;
                    }
                    // If cref exists, check if the text has already been appended
                    else
                    {
                        XElement formattedException = tsException.XEException;
                        XmlHelper.FormatAsNormalElement(formattedException);
                        string nodes = string.Join("", formattedException.Nodes());
                        if (!dException.Value.Contains(nodes))
                        {
                            AddedExceptions.AddIfNotExists($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                            dException.AppendException(nodes);
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

            foreach (TripleSlashTypeParam tsTypeParam in tsMemberToPort.TypeParams)
            {
                DocsTypeParam dTypeParam = dMemberToUpdate.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);
                bool created = false;

                if (dTypeParam == null)
                {
                    ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dMemberToUpdate.DocId}]");
                    dTypeParam = dMemberToUpdate.AddTypeParam(tsTypeParam.XETypeParam);
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

            if (dMemberToUpdate.Changed)
            {
                ModifiedAPIs.AddIfNotExists(dMemberToUpdate.DocId);
                ModifiedAssemblies.AddIfNotExists(tsMemberToPort.Assembly);
                ModifiedFiles.AddIfNotExists(dMemberToUpdate.FilePath);
            }
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
                                        dParam = paramWrapper.Params[paramSelection-1];
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
