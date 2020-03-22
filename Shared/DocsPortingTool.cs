#nullable enable
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
        private static readonly List<string> ModifiedFiles   = new List<string>();
        private static readonly List<string> ModifiedTypes   = new List<string>();
        private static readonly List<string> ModifiedAPIs    = new List<string>();
        private static readonly List<string> ProblematicAPIs = new List<string>();
        private static readonly List<string> AddedExceptions = new List<string>();

        private static int TotalModifiedIndividualElements = 0;

        private static readonly TripleSlashCommentsContainer TripleSlashComments = new TripleSlashCommentsContainer();
        private static readonly DocsCommentsContainer DocsComments = new DocsCommentsContainer();

        // Do all the magic.
        public static void Start()
        {
            TripleSlashComments.CollectFiles();

            if (TripleSlashComments.TotalFiles > 0)
            {
                DocsComments.CollectFiles();
                PortMissingComments();
            }
            else
            {
                Log.Error("No triple slash comments found.");
            }

            PrintUndocumentedAPIs();
            PrintSummary();

            DocsComments.Save();
        }

        
        private static void PortMissingComments()
        {
            Log.Info("Looking for triple slash comments that can be ported...");

            foreach (DocsType dTypeToUpdate in DocsComments.Types)
            {
                PortMissingCommentsForType(dTypeToUpdate);
            }

            foreach (DocsMember dMemberToUpdate in DocsComments.Members)
            {
                PortMissingCommentsForMember(dMemberToUpdate);
            }

        }

        // Tries to find a triple slash element from which to port documentation for the specified Docs type.
        private static void PortMissingCommentsForType(DocsType dTypeToUpdate)
        {
            TripleSlashMember tsTypeToPort = TripleSlashComments.Members.FirstOrDefault(x => x.Name == dTypeToUpdate.DocIdEscaped);
            if (tsTypeToPort != null)
            {
                if (tsTypeToPort.Name == dTypeToUpdate.DocIdEscaped)
                {
                    TryPortMissingSummaryForAPI(dTypeToUpdate, tsTypeToPort, null);
                    TryPortMissingRemarksForAPI(dTypeToUpdate, tsTypeToPort, null);
                    TryPortMissingParamsForAPI(dTypeToUpdate, tsTypeToPort, null); // Some types, like delegates, have params
                }

                if (dTypeToUpdate.Changed)
                {
                    ModifiedTypes.AddIfNotExists(dTypeToUpdate.DocId);
                    ModifiedFiles.AddIfNotExists(dTypeToUpdate.FilePath);
                }
            }
        }

        // Tries to find a triple slash element from which to port documentation for the specified Docs member.
        private static void PortMissingCommentsForMember(DocsMember dMemberToUpdate)
        {
            TripleSlashMember tsMemberToPort = TripleSlashComments.Members.FirstOrDefault(x => x.Name == dMemberToUpdate.DocIdEscaped);
            TryGetEIIMember(dMemberToUpdate, out DocsMember? interfacedMember);

            if (tsMemberToPort != null || interfacedMember != null)
            {
                TryPortMissingSummaryForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingRemarksForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingParamsForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingTypeParamsForMember(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingExceptionsForMember(dMemberToUpdate, tsMemberToPort);

                // Properties sometimes don't have a <value> but have a <returns>
                if (dMemberToUpdate.MemberType == "Property")
                {
                    TryPortMissingPropertyForMember(dMemberToUpdate, tsMemberToPort, interfacedMember);
                }
                else if (dMemberToUpdate.MemberType == "Method")
                {
                    TryPortMissingMethodForMember(dMemberToUpdate, tsMemberToPort, interfacedMember);
                }

                if (dMemberToUpdate.Changed)
                {
                    ModifiedAPIs.AddIfNotExists(dMemberToUpdate.DocId);
                    ModifiedFiles.AddIfNotExists(dMemberToUpdate.FilePath);
                }
            }
        }

        // Gets a string indicating if an API is an explicit interface implementation, or empty.
        private static string GetIsEII(bool isEII)
        {
            return isEII ? " (EII) " : string.Empty;
        }

        // Gets a string indicating if an API was created, otherwise it was modified.
        private static string GetIsCreated(bool created)
        {
            return created ? "CREATED" : "MODIFIED";
        }

        // Attempts to obtain the member of the implemented interface.
        private static bool TryGetEIIMember(IDocsAPI dApiToUpdate, out DocsMember? interfacedMember)
        {
            interfacedMember = null;

            if (dApiToUpdate is DocsMember)
            {
                string interfacedMemberDocId = ((DocsMember)dApiToUpdate).ImplementsInterfaceMember;
                if (!string.IsNullOrEmpty(interfacedMemberDocId))
                {
                    interfacedMemberDocId = interfacedMemberDocId.Substring(2);
                    interfacedMember = DocsComments.Members.FirstOrDefault(x => x.DocId.Substring(2) == interfacedMemberDocId);
                    return interfacedMember != null;
                }
            }

            return false;
        }

        // Ports the summary for the specified API if the field is undocumented.
        private static void TryPortMissingSummaryForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            // Only port if undocumented in MS Docs
            if (IsEmpty(dApiToUpdate.Summary))
            {
                bool isEII = false;

                string name = string.Empty;
                string value = string.Empty;

                // Try to port triple slash comments
                if (tsMemberToPort != null && !IsEmpty(tsMemberToPort.Summary))
                {
                    dApiToUpdate.Summary = tsMemberToPort.Summary;
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Summary;
                }
                // or try to find if it implements a documented interface
                else if (interfacedMember != null && !IsEmpty(interfacedMember.Summary))
                {
                    dApiToUpdate.Summary = interfacedMember.Summary;
                    isEII = true;
                    name = interfacedMember.MemberName;
                    value = interfacedMember.Summary;
                }

                if (!IsEmpty(value))
                {
                    // Any member can have an empty summary
                    string message = $"{dApiToUpdate.Prefix} {GetIsEII(isEII)} SUMMARY";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, name, dApiToUpdate.DocId, value, dApiToUpdate.Summary);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports the remarks for the specified API if the field is undocumented.
        private static void TryPortMissingRemarksForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (Configuration.SkipRemarks)
            {
                return;
            }

            if (IsEmpty(dApiToUpdate.Remarks))
            {
                bool isEII = false;
                string name = string.Empty;
                string value = string.Empty;

                // Try to port triple slash comments
                if (tsMemberToPort != null && !IsEmpty(tsMemberToPort.Remarks))
                {
                    dApiToUpdate.Remarks = tsMemberToPort.Remarks;
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Remarks;
                }
                // or try to find if it implements a documented interface
                else if (interfacedMember != null && !IsEmpty(interfacedMember.Remarks))
                {
                    string eiiMessage = string.Empty;
                    // Special text for EIIs in Remarks
                    if (IsEmpty(dApiToUpdate.Remarks) || !dApiToUpdate.Remarks.Contains("This member is an explicit interface member implementation"))
                    {
                        string dMemberToUpdateParentDocId = ((DocsMember)dApiToUpdate).ParentType.DocId.Substring(2);
                        string interfacedMemberParentDocId = interfacedMember.ParentType.DocId.Substring(2);

                        eiiMessage = $"This member is an explicit interface member implementation. It can be used only when the <xref:{dMemberToUpdateParentDocId}> instance is cast to an <xref:{interfacedMemberParentDocId}> interface.{Environment.NewLine + Environment.NewLine}";
                    }

                    string original = string.Empty;
                    if (!interfacedMember.Remarks.Contains("To be added."))
                    {
                        original = dApiToUpdate.Remarks
                            .CleanRemarksText("##Remarks")
                            .CleanRemarksText("## Remarks")
                            .CleanRemarksText("<![CDATA[")
                            .CleanRemarksText("]]>");
                    }
                    interfacedMember.Remarks = eiiMessage + original;
                    name = interfacedMember.MemberName;
                    value = interfacedMember.Remarks;
                    
                    isEII = true;
                }

                if (!IsEmpty(value))
                {
                    // Any member can have an empty remark
                    string message = $"{dApiToUpdate.Prefix} {GetIsEII(isEII)} REMARKS";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, name, dApiToUpdate.DocId, value, dApiToUpdate.Remarks);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the parameter descriptions for the specified API if any of them is undocumented.
        private static void TryPortMissingParamsForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (tsMemberToPort != null)
            {
                string prefix = dApiToUpdate.Prefix;

                foreach (TripleSlashParam tsParam in tsMemberToPort.Params)
                {
                    bool isEII = false;
                    string name = string.Empty;
                    string value = string.Empty;

                    DocsParam? dParam = dApiToUpdate.Params.FirstOrDefault(x => x.Name == tsParam.Name);
                    bool created = false;

                    // When not found, it's a bug in Docs (param name not the same as source/ref), so need to ask the user to indicate correct name
                    if (dParam == null)
                    {
                        ProblematicAPIs.AddIfNotExists($"Param=[{tsParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");

                        created = TryPromptParam(tsParam, dApiToUpdate, out dParam);
                    }

                    if (dParam != null)
                    {
                        // Now attempt to document it
                        if (IsEmpty(dParam.Value))
                        {
                            // try to port triple slash comments
                            if (!IsEmpty(tsParam.Value))
                            {
                                name = tsParam.Name;
                                value = tsParam.Value;
                            }
                            // or try to find if it implements a documented interface
                            else if (interfacedMember != null)
                            {
                                DocsParam interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                                if (interfacedParam != null)
                                {
                                    name = interfacedParam.Name;
                                    value = interfacedParam.Value;
                                    isEII = true;
                                }
                            }
                        }

                        if (!IsEmpty(value))
                        {
                            dParam.Value = value;
                            string message = $"{prefix} {GetIsEII(isEII)} PARAM ({GetIsCreated(created)})";
                            PrintModifiedMember(message, dParam.ParentAPI.FilePath, name, dParam.Name, value, dParam.Value);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }

            
        }

        // Tries to document the passed property.
        private static void TryPortMissingPropertyForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (IsEmpty(dMemberToUpdate.Value))
            {
                string name = string.Empty;
                string value = string.Empty;
                bool isEII = false;

                // Issue: sometimes properties have their TS string in Value, sometimes in Returns
                if (tsMemberToPort != null)
                {
                    name = tsMemberToPort.Name;
                    if (!IsEmpty(tsMemberToPort.Value))
                    {
                        value = tsMemberToPort.Value;
                    }
                    else if (!IsEmpty(tsMemberToPort.Returns))
                    {
                        value = tsMemberToPort.Returns;
                    }
                }
                // or try to find if it implements a documented interface
                else if (interfacedMember != null)
                {
                    name = interfacedMember.MemberName;
                    if (!IsEmpty(interfacedMember.Value))
                    {
                        value = interfacedMember.Value;
                    }
                    else if (!IsEmpty(interfacedMember.Returns))
                    {
                        value = interfacedMember.Returns;
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        isEII = true;
                    }
                }

                if (!IsEmpty(value))
                {
                    dMemberToUpdate.Value = value;
                    string message = $"MEMBER {GetIsEII(isEII)} PROPERTY";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath, name, dMemberToUpdate.DocId, value, dMemberToUpdate.Value);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Tries to document the passed method.
        private static void TryPortMissingMethodForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (IsEmpty(dMemberToUpdate.Returns))
            {
                string name = string.Empty;
                string value = string.Empty;
                bool isEII = false;

                // Bug: Sometimes a void return value shows up as not documented, skip those
                if (dMemberToUpdate.ReturnType == "System.Void")
                {
                    ProblematicAPIs.AddIfNotExists($"Unexpected System.Void return value in Method=[{dMemberToUpdate.DocId}]");
                }
                else if (tsMemberToPort != null && !IsEmpty(tsMemberToPort.Returns))
                {
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Returns;
                }
                else if (interfacedMember != null && !IsEmpty(interfacedMember.Returns))
                {
                    name = interfacedMember.MemberName;
                    value = interfacedMember.Returns;
                    isEII = true;
                }

                if (!IsEmpty(value))
                {
                    dMemberToUpdate.Returns = value;
                    string message = $"METHOD {GetIsEII(isEII)} RETURNS";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath, name, dMemberToUpdate.DocId, value, dMemberToUpdate.Returns);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the type parameter descriptions for the specified API if any of them is undocumented.
        private static void TryPortMissingTypeParamsForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (tsMemberToPort != null)
            {
                foreach (TripleSlashTypeParam tsTypeParam in tsMemberToPort.TypeParams)
                {
                    bool isEII = false;
                    string name = string.Empty;
                    string value = string.Empty;

                    DocsTypeParam dTypeParam = dMemberToUpdate.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);

                    bool created = false;
                    if (dTypeParam == null)
                    {
                        ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dMemberToUpdate.DocId}]");
                        dTypeParam = dMemberToUpdate.AddTypeParam(tsTypeParam.Name, XmlHelper.GetNodesInPlainText(tsTypeParam.XETypeParam));
                        created = true;
                    }

                    // But it can still be empty, try to retrieve it
                    if (IsEmpty(dTypeParam.Value))
                    {
                        // try to port triple slash comments
                        if (!IsEmpty(tsTypeParam.Value))
                        {
                            name = tsTypeParam.Name;
                            value = tsTypeParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsTypeParam interfacedTypeParam = interfacedMember.TypeParams.FirstOrDefault(x => x.Name == dTypeParam.Name);
                            if (interfacedTypeParam != null)
                            {
                                name = interfacedTypeParam.Name;
                                value = interfacedTypeParam.Value;
                                isEII = true;
                            }
                        }
                    }

                    if (!IsEmpty(value))
                    {
                        dTypeParam.Value = value;
                        string message = $"MEMBER {GetIsEII(isEII)} TYPEPARAM ({GetIsCreated(created)})";
                        PrintModifiedMember(message, dTypeParam.ParentAPI.FilePath, name, dTypeParam.Name, value, dTypeParam.Value);
                        TotalModifiedIndividualElements++;
                    }
                }
            }
        }

        // Ports all the exceptions for the specified API.
        // They are only processed if the user specified in the command arguments to NOT skip exceptions.
        // All exceptions get ported, because there is no easy way to determine if an exception is already documented or not.
        private static void TryPortMissingExceptionsForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort)
        {
            if (Configuration.SkipExceptions)
            {
                return;
            }

            if (tsMemberToPort != null)
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

        // If a Param is found in a DocsType or a DocsMember that did not exist in the Triple Slash member, it's possible the param was unexpectedly saved in the triple slash comments with a different name, so the user gets prompted to look for it.
        private static bool TryPromptParam(TripleSlashParam tsParam, IDocsAPI paramWrapper, out DocsParam? dParam)
        {
            dParam = null;
            bool created = false;

            if (Configuration.DisablePrompts)
            {
                Log.Error($"Prompts disabled. Will not process the {tsParam.Name} param.");
                return created;
            }

            int option = -1;
            while (option == -1)
            {
                Log.Error($"Problem in param '{tsParam.Name}' in member '{paramWrapper.DocId}' in file '{paramWrapper.FilePath}'");
                Log.Error($"The param probably exists in code, but the exact name was not found in Docs. What would you like to do?");
                Log.Warning("    0 - Exit program.");
                Log.Info("    1 - Select the correct param from the existing ones detected in Docs for this member.");
                Log.Info("    2 - Overwrite the param name in the Docs file with the detected one (not recommended).");
                Log.Warning("        Note: Whatever your choice, make sure to double check the affected Docs file after the tool finishes executing.");
                Log.Cyan(false, "Your answer [0,1,2]: ");

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

                                    Log.Cyan(false, $"Your answer to match param '{tsParam.Name}'? [0..{paramCounter - 1}]: ");

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

        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        private static bool IsEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) || s == Configuration.ToBeAdded;
        }

        /// <summary>
        /// Standard formatted print message for a modified element.
        /// </summary>
        /// <param name="message">The friendly description of the modified API.</param>
        /// <param name="docsFilePath">The file where the modified API lives.</param>
        /// <param name="tripleSlashAPIName">The API name in the triple slash file.</param>
        /// <param name="docsAPIName">The API name in the Docs file.</param>
        /// <param name="tripleSlashValue">The value that was found in the triple slash file.</param>
        /// <param name="docsValue">The value that was found in the Docs file.</param>
        private static void PrintModifiedMember(string message, string docsFilePath, string tripleSlashAPIName, string docsAPIName, string tripleSlashValue, string docsValue)
        {
            Log.Warning("    File: {0}", docsFilePath);
            Log.Warning("        {0}", message);
            Log.Warning("            Code: {0} => {1}", tripleSlashAPIName, tripleSlashValue);
            Log.Warning("            Docs: {0} => {1}", docsAPIName, docsValue);
            Log.Info("---------------------------------------------------");
            Log.Line();
        }

        // Prints all the undocumented APIs.
        // This is only done if the user specified in the command arguments to print undocumented APIs.
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
                foreach (DocsType docsType in DocsComments.Types)
                {
                    bool undocAPI = false;
                    if (IsEmpty(docsType.Summary))
                    {
                        TryPrintType(ref undocAPI, docsType.DocId);
                        Log.Error($"        Type Summary: {docsType.Summary}");
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

        // Prints a final summary of the execution findings.
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
            Log.Info($"Total modified types: {ModifiedTypes.Count}");
            foreach (string type in ModifiedTypes)
            {
                Log.Warning($"    - {type}");
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
