#nullable enable
using Libraries.Docs;
using Libraries.IntelliSenseXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Libraries
{
    public class ToDocsPorter
    {
        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;
        private readonly IntelliSenseXmlCommentsContainer IntelliSenseXmlComments;

        private readonly List<string> ModifiedFiles   = new List<string>();
        private readonly List<string> ModifiedTypes   = new List<string>();
        private readonly List<string> ModifiedAPIs    = new List<string>();
        private readonly List<string> ProblematicAPIs = new List<string>();
        private readonly List<string> AddedExceptions = new List<string>();

        private int TotalModifiedIndividualElements = 0;

        public ToDocsPorter(Configuration config)
        {
            if (config.Direction != Configuration.PortingDirection.ToDocs)
            {
                throw new InvalidOperationException($"Unexpected porting direction: {config.Direction}");
            }
            Config = config;
            DocsComments = new DocsCommentsContainer(config);
            IntelliSenseXmlComments = new IntelliSenseXmlCommentsContainer(config);

        }

        public void Start()
        {
            IntelliSenseXmlComments.CollectFiles();

            if (!IntelliSenseXmlComments.Members.Any())
            {
                Log.Error("No IntelliSense xml comments found.");
            }

            DocsComments.CollectFiles();
            if (!DocsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found.");
            }

            PortMissingComments();

            PrintUndocumentedAPIs();
            PrintSummary();

            DocsComments.Save();
        }

        private void PortMissingComments()
        {
            Log.Info("Looking for IntelliSense xml comments that can be ported...");

            foreach (DocsType dTypeToUpdate in DocsComments.Types.Values)
            {
                PortMissingCommentsForType(dTypeToUpdate);
            }

            foreach (DocsMember dMemberToUpdate in DocsComments.Members.Values)
            {
                PortMissingCommentsForMember(dMemberToUpdate);
            }
        }

        // Tries to find an IntelliSense xml element from which to port documentation for the specified Docs type.
        private void PortMissingCommentsForType(DocsType dTypeToUpdate)
        {
            if (IntelliSenseXmlComments.Members.TryGetValue(dTypeToUpdate.DocIdEscaped, out IntelliSenseXmlMember? tsTypeToPort))
            {
                if (tsTypeToPort.Name == dTypeToUpdate.DocIdEscaped)
                {
                    TryPortMissingSummaryForAPI(dTypeToUpdate, tsTypeToPort, null);
                    TryPortMissingRemarksForAPI(dTypeToUpdate, tsTypeToPort, null, skipInterfaceRemarks: true);
                    TryPortMissingParamsForAPI(dTypeToUpdate, tsTypeToPort, null); // Some types, like delegates, have params
                    TryPortMissingTypeParamsForAPI(dTypeToUpdate, tsTypeToPort, null); // Type names ending with <T> have TypeParams
                }

                if (dTypeToUpdate.Changed)
                {
                    ModifiedTypes.AddIfNotExists(dTypeToUpdate.DocId);
                    ModifiedFiles.AddIfNotExists(dTypeToUpdate.FilePath);
                }
            }
        }

        // Tries to find an IntelliSense xml element from which to port documentation for the specified Docs member.
        private void PortMissingCommentsForMember(DocsMember dMemberToUpdate)
        {
            string docId = dMemberToUpdate.DocIdEscaped;
            IntelliSenseXmlComments.Members.TryGetValue(docId, out IntelliSenseXmlMember? tsMemberToPort);
            TryGetEIIMember(dMemberToUpdate, out DocsMember? interfacedMember);

            if (tsMemberToPort != null || interfacedMember != null)
            {
                TryPortMissingSummaryForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingRemarksForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember, Config.SkipInterfaceRemarks);
                TryPortMissingParamsForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
                TryPortMissingTypeParamsForAPI(dMemberToUpdate, tsMemberToPort, interfacedMember);
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
        private string GetIsEII(bool isEII)
        {
            return isEII ? " (EII) " : string.Empty;
        }

        // Gets a string indicating if an API was created, otherwise it was modified.
        private string GetIsCreated(bool created)
        {
            return created ? "Created" : "Modified";
        }

        // Attempts to obtain the member of the implemented interface.
        private bool TryGetEIIMember(IDocsAPI dApiToUpdate, out DocsMember? interfacedMember)
        {
            interfacedMember = null;

            if (!Config.SkipInterfaceImplementations && dApiToUpdate is DocsMember member)
            {
                string interfacedMemberDocId = member.ImplementsInterfaceMember;
                if (!string.IsNullOrEmpty(interfacedMemberDocId))
                {
                    DocsComments.Members.TryGetValue(interfacedMemberDocId, out interfacedMember);
                    return interfacedMember != null;
                }
            }

            return false;
        }

        // Ports the summary for the specified API if the field is undocumented.
        private void TryPortMissingSummaryForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeSummaries ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberSummaries)
            {
                return;
            }

            // Only port if undocumented in MS Docs
            if (dApiToUpdate.Summary.IsDocsEmpty())
            {
                bool isEII = false;

                string name = string.Empty;
                string value = string.Empty;

                // Try to port IntelliSense xml comments
                if (tsMemberToPort != null && !tsMemberToPort.Summary.IsDocsEmpty())
                {
                    dApiToUpdate.Summary = tsMemberToPort.Summary;
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Summary;
                }
                // or try to find if it implements a documented interface
                else if (interfacedMember != null && !interfacedMember.Summary.IsDocsEmpty())
                {
                    dApiToUpdate.Summary = interfacedMember.Summary;
                    isEII = true;
                    name = interfacedMember.MemberName;
                    value = interfacedMember.Summary;
                }

                if (!value.IsDocsEmpty())
                {
                    // Any member can have an empty summary
                    string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} summary: {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports the remarks for the specified API if the field is undocumented.
        private void TryPortMissingRemarksForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember, bool skipInterfaceRemarks)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeRemarks ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberRemarks)
            {
                return;
            }

            if (dApiToUpdate.Remarks.IsDocsEmpty())
            {
                bool isEII = false;
                string name = string.Empty;
                string value = string.Empty;

                // Try to port IntelliSense xml comments
                if (tsMemberToPort != null && !tsMemberToPort.Remarks.IsDocsEmpty())
                {
                    dApiToUpdate.Remarks = tsMemberToPort.Remarks;
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Remarks;
                }
                // or try to find if it implements a documented interface
                // which only happens in docs members (types have a null interfacedMember passed)
                else if (interfacedMember != null && !interfacedMember.Remarks.IsDocsEmpty())
                {
                    DocsMember memberToUpdate = (DocsMember)dApiToUpdate;

                    // Only attempt to port if the member name is the same as the interfaced member docid without prefix
                    if (memberToUpdate.MemberName == interfacedMember.DocId[2..])
                    {
                        string dMemberToUpdateTypeDocIdNoPrefix = memberToUpdate.ParentType.DocId[2..];
                        string interfacedMemberTypeDocIdNoPrefix = interfacedMember.ParentType.DocId[2..];

                        // Special text for EIIs in Remarks
                        string eiiMessage = $"This member is an explicit interface member implementation. It can be used only when the <xref:{dMemberToUpdateTypeDocIdNoPrefix}> instance is cast to an <xref:{interfacedMemberTypeDocIdNoPrefix}> interface.";

                        string cleanedInterfaceRemarks = string.Empty;
                        if (!interfacedMember.Remarks.Contains(Configuration.ToBeAdded))
                        {
                            cleanedInterfaceRemarks += Environment.NewLine;

                            string interfaceMemberRemarks = interfacedMember.Remarks.RemoveSubstrings("##Remarks", "## Remarks", "<![CDATA[", "]]>");
                            foreach (string line in interfaceMemberRemarks.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                cleanedInterfaceRemarks += Environment.NewLine + line;
                            }
                        }

                        // Only port the interface remarks if the user desired that
                        if (!skipInterfaceRemarks)
                        {
                            dApiToUpdate.Remarks = eiiMessage + cleanedInterfaceRemarks;
                        }
                        // Otherwise, always add the EII special message
                        else
                        {
                            dApiToUpdate.Remarks = eiiMessage;
                        }

                        name = interfacedMember.MemberName;
                        value = dApiToUpdate.Remarks;

                        isEII = true;
                    }
                }

                if (!value.IsDocsEmpty())
                {
                    // Any member can have an empty remark
                    string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} remarks: {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the parameter descriptions for the specified API if any of them is undocumented.
        private void TryPortMissingParamsForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeParams ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberParams)
            {
                return;
            }

            bool created;
            bool isEII;
            string name;
            string value;

            if (tsMemberToPort != null)
            {
                foreach (DocsParam dParam in dApiToUpdate.Params)
                {
                    if (dParam.Value.IsDocsEmpty())
                    {
                        created = false;
                        isEII = false;
                        name = string.Empty;
                        value = string.Empty;

                        IntelliSenseXmlParam? tsParam = tsMemberToPort.Params.FirstOrDefault(x => x.Name == dParam.Name);

                        // When not found, it's a bug in Docs (param name not the same as source/ref), so need to ask the user to indicate correct name
                        if (tsParam == null)
                        {
                            ProblematicAPIs.AddIfNotExists($"Param=[{dParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");

                            if (tsMemberToPort.Params.Count() == 0)
                            {
                                ProblematicAPIs.AddIfNotExists($"Param=[{dParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");
                                Log.Warning($"  There were no IntelliSense xml comments for param '{dParam.Name}' in {dApiToUpdate.DocId}");
                            }
                            else
                            {
                                created = TryPromptParam(dParam, tsMemberToPort, out IntelliSenseXmlParam? newTsParam);
                                if (newTsParam == null)
                                {
                                    Log.Error($"  The param '{dParam.Name}' was not found in IntelliSense xml for {dApiToUpdate.DocId}.");
                                }
                                else
                                {
                                    // Now attempt to document it
                                    if (!newTsParam.Value.IsDocsEmpty())
                                    {
                                        // try to port IntelliSense xml comments
                                        dParam.Value = newTsParam.Value;
                                        name = newTsParam.Name;
                                        value = newTsParam.Value;
                                    }
                                    // or try to find if it implements a documented interface
                                    else if (interfacedMember != null)
                                    {
                                        DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == newTsParam.Name || x.Name == dParam.Name);
                                        if (interfacedParam != null)
                                        {
                                            dParam.Value = interfacedParam.Value;
                                            name = interfacedParam.Name;
                                            value = interfacedParam.Value;
                                            isEII = true;
                                        }
                                    }
                                }
                            }
                        }
                        // Attempt to port
                        else if (!tsParam.Value.IsDocsEmpty())
                        {
                            // try to port IntelliSense xml comments
                            dParam.Value = tsParam.Value;
                            name = tsParam.Name;
                            value = tsParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                            if (interfacedParam != null)
                            {
                                dParam.Value = interfacedParam.Value;
                                name = interfacedParam.Name;
                                value = interfacedParam.Value;
                                isEII = true;
                            }
                        }
                        

                        if (!value.IsDocsEmpty())
                        {
                            string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} ({GetIsCreated(created)}) param {name.Escaped()} = {value.Escaped()}";
                            PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
            else if (interfacedMember != null)
            {
                foreach (DocsParam dParam in dApiToUpdate.Params)
                {
                    if (dParam.Value.IsDocsEmpty())
                    {
                        DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                        if (interfacedParam != null && !interfacedParam.Value.IsDocsEmpty())
                        {
                            dParam.Value = interfacedParam.Value;

                            string message = $"{dApiToUpdate.Kind} EII ({GetIsCreated(false)}) param {dParam.Name.Escaped()} = {dParam.Value.Escaped()}";
                            PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
        }

        // Ports all the type parameter descriptions for the specified API if any of them is undocumented.
        private void TryPortMissingTypeParamsForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeTypeParams ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberTypeParams)
            {
                return;
            }

            if (tsMemberToPort != null)
            {
                foreach (IntelliSenseXmlTypeParam tsTypeParam in tsMemberToPort.TypeParams)
                {
                    bool isEII = false;
                    string name = string.Empty;
                    string value = string.Empty;

                    DocsTypeParam? dTypeParam = dApiToUpdate.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);

                    bool created = false;
                    if (dTypeParam == null)
                    {
                        ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dApiToUpdate.DocId}]");
                        dTypeParam = dApiToUpdate.AddTypeParam(tsTypeParam.Name, XmlHelper.GetNodesInPlainText(tsTypeParam.XETypeParam));
                        created = true;
                    }

                    // But it can still be empty, try to retrieve it
                    if (dTypeParam.Value.IsDocsEmpty())
                    {
                        // try to port IntelliSense xml comments
                        if (!tsTypeParam.Value.IsDocsEmpty())
                        {
                            name = tsTypeParam.Name;
                            value = tsTypeParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsTypeParam? interfacedTypeParam = interfacedMember.TypeParams.FirstOrDefault(x => x.Name == dTypeParam.Name);
                            if (interfacedTypeParam != null)
                            {
                                name = interfacedTypeParam.Name;
                                value = interfacedTypeParam.Value;
                                isEII = true;
                            }
                        }
                    }

                    if (!value.IsDocsEmpty())
                    {
                        dTypeParam.Value = value;
                        string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} ({GetIsCreated(created)}) typeparam {name.Escaped()} = {value.Escaped()}";
                        PrintModifiedMember(message, dTypeParam.ParentAPI.FilePath, dApiToUpdate.DocId);
                        TotalModifiedIndividualElements++;
                    }
                }
            }
        }

        // Tries to document the passed property.
        private void TryPortMissingPropertyForMember(DocsMember dMemberToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (!Config.PortMemberProperties)
            {
                return;
            }

            if (dMemberToUpdate.Value.IsDocsEmpty())
            {
                string name = string.Empty;
                string value = string.Empty;
                bool isEII = false;

                // Issue: sometimes properties have their TS string in Value, sometimes in Returns
                if (tsMemberToPort != null)
                {
                    name = tsMemberToPort.Name;
                    if (!tsMemberToPort.Value.IsDocsEmpty())
                    {
                        value = tsMemberToPort.Value;
                    }
                    else if (!tsMemberToPort.Returns.IsDocsEmpty())
                    {
                        value = tsMemberToPort.Returns;
                    }
                }
                // or try to find if it implements a documented interface
                else if (interfacedMember != null)
                {
                    name = interfacedMember.MemberName;
                    if (!interfacedMember.Value.IsDocsEmpty())
                    {
                        value = interfacedMember.Value;
                    }
                    else if (!interfacedMember.Returns.IsDocsEmpty())
                    {
                        value = interfacedMember.Returns;
                    }
                    if (!string.IsNullOrEmpty(value))
                    {
                        isEII = true;
                    }
                }

                if (!value.IsDocsEmpty())
                {
                    dMemberToUpdate.Value = value;
                    string message = $"Member {GetIsEII(isEII)} property {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath,dMemberToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Tries to document the passed method.
        private void TryPortMissingMethodForMember(DocsMember dMemberToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (!Config.PortMemberReturns)
            {
                return;
            }

            if (dMemberToUpdate.Returns.IsDocsEmpty())
            {
                string name = string.Empty;
                string value = string.Empty;
                bool isEII = false;

                // Bug: Sometimes a void return value shows up as not documented, skip those
                if (dMemberToUpdate.ReturnType == "System.Void")
                {
                    ProblematicAPIs.AddIfNotExists($"Unexpected System.Void return value in Method=[{dMemberToUpdate.DocId}]");
                }
                else if (tsMemberToPort != null && !tsMemberToPort.Returns.IsDocsEmpty())
                {
                    name = tsMemberToPort.Name;
                    value = tsMemberToPort.Returns;
                }
                else if (interfacedMember != null && !interfacedMember.Returns.IsDocsEmpty())
                {
                    name = interfacedMember.MemberName;
                    value = interfacedMember.Returns;
                    isEII = true;
                }

                if (!value.IsDocsEmpty())
                {
                    dMemberToUpdate.Returns = value;
                    string message = $"Method {GetIsEII(isEII)} returns {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath, dMemberToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the exceptions for the specified API.
        // They are only processed if the user specified in the command arguments to NOT skip exceptions.
        // All exceptions get ported, because there is no easy way to determine if an exception is already documented or not.
        private void TryPortMissingExceptionsForMember(DocsMember dMemberToUpdate, IntelliSenseXmlMember? tsMemberToPort)
        {
            if (!Config.PortExceptionsExisting && !Config.PortExceptionsNew)
            {
                return;
            }

            if (tsMemberToPort != null)
            {
                // Exceptions are a special case: If a new one is found in code, but does not exist in docs, the whole element needs to be added
                foreach (IntelliSenseXmlException tsException in tsMemberToPort.Exceptions)
                {
                    DocsException? dException = dMemberToUpdate.Exceptions.FirstOrDefault(x => x.Cref == tsException.Cref);
                    bool created = false;

                    // First time adding the cref
                    if (dException == null && Config.PortExceptionsNew)
                    {
                        AddedExceptions.AddIfNotExists($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                        string text = XmlHelper.ReplaceExceptionPatterns(XmlHelper.GetNodesInPlainText(tsException.XEException));
                        dException = dMemberToUpdate.AddException(tsException.Cref, text);
                        created = true;
                    }
                    // If cref exists, check if the text has already been appended
                    else if (dException != null && Config.PortExceptionsExisting)
                    {
                        XElement formattedException = tsException.XEException;
                        string value = XmlHelper.ReplaceExceptionPatterns(XmlHelper.GetNodesInPlainText(formattedException));
                        if (!dException.WordCountCollidesAboveThreshold(value, Config.ExceptionCollisionThreshold))
                        {
                            AddedExceptions.AddIfNotExists($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                            dException.AppendException(value);
                            created = true;
                        }
                    }

                    if (dException !=  null)
                    {
                        if (created || (!tsException.Value.IsDocsEmpty() && dException.Value.IsDocsEmpty()))
                        {
                            string message = string.Format($"Exception ({GetIsCreated(created)}) {dException.Cref.Escaped()} = {dException.Value.Escaped()}");
                            PrintModifiedMember(message, dException.ParentAPI.FilePath, dException.Cref);

                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
        }

        // If a Param is found in a DocsType or a DocsMember that did not exist in the IntelliSense xml member, it's possible the param was unexpectedly saved in the IntelliSense xml comments with a different name, so the user gets prompted to look for it.
        private bool TryPromptParam(DocsParam oldDParam, IntelliSenseXmlMember tsMember, out IntelliSenseXmlParam? newTsParam)
        {
            newTsParam = null;

            if (Config.DisablePrompts)
            {
                Log.Error($"Prompts disabled. Will not process the '{oldDParam.Name}' param.");
                return false;
            }

            bool created = false;
            int option = -1;
            while (option == -1)
            {
                Log.Error($"Problem in param '{oldDParam.Name}' in member '{tsMember.Name}' in file '{oldDParam.ParentAPI.FilePath}'");
                Log.Error($"The param probably exists in code, but the exact name was not found in Docs. What would you like to do?");
                Log.Warning("    0 - Exit program.");
                Log.Info("    1 - Select the correct IntelliSense xml param from the existing ones.");
                Log.Info("    2 - Ignore this param.");
                Log.Warning("      Note:Make sure to double check the affected Docs file after the tool finishes executing.");
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
                            {
                                Log.Info("Goodbye!");
                                Environment.Exit(0);
                                break;
                            }

                        case 1:
                            {
                                int paramSelection = -1;
                                while (paramSelection == -1)
                                {
                                    Log.Info($"IntelliSense xml params found in member '{tsMember.Name}':");
                                    Log.Warning("    0 - Exit program.");
                                    int paramCounter = 1;
                                    foreach (IntelliSenseXmlParam param in tsMember.Params)
                                    {
                                        Log.Info($"    {paramCounter} - {param.Name}");
                                        paramCounter++;
                                    }

                                    Log.Cyan(false, $"Your answer to match param '{oldDParam.Name}'? [0..{paramCounter - 1}]: ");

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
                                        newTsParam = tsMember.Params[paramSelection - 1];
                                        Log.Success($"Selected: {newTsParam.Name}");
                                    }
                                }

                                break;
                            }

                        case 2:
                            {
                                Log.Info("Skipping this param.");
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
        /// Standard formatted print message for a modified element.
        /// </summary>
        /// <param name="message">The friendly description of the modified API.</param>
        /// <param name="docsFilePath">The file where the modified API lives.</param>
        /// <param name="docId">The API unique identifier.</param>
        private void PrintModifiedMember(string message, string docsFilePath, string docId)
        {
            Log.Warning($"    File: {docsFilePath}");
            Log.Warning($"        DocID: {docId}");
            Log.Warning($"        {message}");
            Log.Info("---------------------------------------------------");
            Log.Line();
        }

        // Prints all the undocumented APIs.
        // This is only done if the user specified in the command arguments to print undocumented APIs.
        private void PrintUndocumentedAPIs()
        {
            if (Config.PrintUndoc)
            {
                Log.Line();
                Log.Success("-----------------");
                Log.Success("UNDOCUMENTED APIS");
                Log.Success("-----------------");

                Log.Line();

                void TryPrintType(ref bool undocAPI, string typeDocId)
                {
                    if (!undocAPI)
                    {
                        Log.Info("    Type: {0}", typeDocId);
                        undocAPI = true;
                    }
                };

                void TryPrintMember(ref bool undocMember, string memberDocId)
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

                foreach (DocsType docsType in DocsComments.Types.Values)
                {
                    bool undocAPI = false;
                    if (docsType.Summary.IsDocsEmpty())
                    {
                        TryPrintType(ref undocAPI, docsType.DocId);
                        Log.Error($"        Type Summary: {docsType.Summary}");
                        typeSummaries++;
                    }
                }

                foreach (DocsMember member in DocsComments.Members.Values)
                {
                    bool undocMember = false;

                    if (member.Summary.IsDocsEmpty())
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
                        if (param.Value.IsDocsEmpty())
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Param: {param.Name}: {param.Value}");
                            memberParams++;
                        }
                    }

                    foreach (DocsTypeParam typeParam in member.TypeParams)
                    {
                        if (typeParam.Value.IsDocsEmpty())
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Type Param: {typeParam.Name}: {typeParam.Value}");
                            memberTypeParams++;
                        }
                    }

                    foreach (DocsException exception in member.Exceptions)
                    {
                        if (exception.Value.IsDocsEmpty())
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
        private void PrintSummary()
        {
            Log.Line();
            Log.Success("---------");
            Log.Success("FINISHED!");
            Log.Success("---------");

            Log.Line();
            Log.Info($"Total modified files: {ModifiedFiles.Count}");
            foreach (string file in ModifiedFiles)
            {
                Log.Success($"    - {file}");
            }

            Log.Line();
            Log.Info($"Total modified types: {ModifiedTypes.Count}");
            foreach (string type in ModifiedTypes)
            {
                Log.Success($"    - {type}");
            }

            Log.Line();
            Log.Info($"Total modified APIs: {ModifiedAPIs.Count}");
            foreach (string api in ModifiedAPIs)
            {
                Log.Success($"    - {api}");
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
                Log.Success($"    - {exception}");
            }

            Log.Line();
            Log.Info(false, "Total modified individual elements: ");
            Log.Success($"{TotalModifiedIndividualElements}");
        }
    }
}
