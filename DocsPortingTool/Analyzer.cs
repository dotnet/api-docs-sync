#nullable enable
using DocsPortingTool.Docs;
using DocsPortingTool.TripleSlash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool
{
    public class Analyzer
    {
        private readonly List<string> ModifiedFiles   = new List<string>();
        private readonly List<string> ModifiedTypes   = new List<string>();
        private readonly List<string> ModifiedAPIs    = new List<string>();
        private readonly List<string> ProblematicAPIs = new List<string>();
        private readonly List<string> AddedExceptions = new List<string>();

        private int TotalModifiedIndividualElements = 0;

        private readonly TripleSlashCommentsContainer TripleSlashComments;
        private readonly DocsCommentsContainer DocsComments;

        private Configuration Config { get; set; }

        public Analyzer(Configuration config)
        {
            Config = config;
            TripleSlashComments = new TripleSlashCommentsContainer(config);
            DocsComments = new DocsCommentsContainer(config);
        }

        // Do all the magic.
        public void Start()
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

        private void PortMissingComments()
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
        private void PortMissingCommentsForType(DocsType dTypeToUpdate)
        {
            if (!CanAnalyzeAPI(dTypeToUpdate))
            {
                return;
            }

            TripleSlashMember tsTypeToPort = TripleSlashComments.Members.FirstOrDefault(x => x.Name == dTypeToUpdate.DocIdEscaped);
            if (tsTypeToPort != null)
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

        // Tries to find a triple slash element from which to port documentation for the specified Docs member.
        private void PortMissingCommentsForMember(DocsMember dMemberToUpdate)
        {
            if (!CanAnalyzeAPI(dMemberToUpdate))
            {
                return;
            }

            TripleSlashMember tsMemberToPort = TripleSlashComments.Members.FirstOrDefault(x => x.Name == dMemberToUpdate.DocIdEscaped);
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
                    interfacedMember = DocsComments.Members.FirstOrDefault(x => x.DocId == interfacedMemberDocId);
                    return interfacedMember != null;
                }
            }

            return false;
        }

        // Ports the summary for the specified API if the field is undocumented.
        private void TryPortMissingSummaryForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeSummaries ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberSummaries)
            {
                return;
            }

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
                    string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} summary: {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports the remarks for the specified API if the field is undocumented.
        private void TryPortMissingRemarksForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember, bool skipInterfaceRemarks)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeRemarks ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberRemarks)
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
                // which only happens in docs members (types have a null interfacedMember passed)
                else if (interfacedMember != null && !IsEmpty(interfacedMember.Remarks))
                {
                    DocsMember memberToUpdate = (DocsMember)dApiToUpdate;

                    // Only attempt to port if the member name is the same as the interfaced member docid without prefix
                    if (memberToUpdate.MemberName == interfacedMember.DocId[2..])
                    {
                        string dMemberToUpdateTypeDocIdNoPrefix = memberToUpdate.ParentType.DocId[2..];
                        string interfacedMemberTypeDocIdNoPrefix = interfacedMember.ParentType.DocId[2..];

                        // Special text for EIIs in Remarks
                        string eiiMessage = $"This member is an explicit interface member implementation. It can be used only when the <xref:{dMemberToUpdateTypeDocIdNoPrefix}> instance is cast to an <xref:{interfacedMemberTypeDocIdNoPrefix}> interface.{Environment.NewLine + Environment.NewLine}";

                        string cleanedInterfaceRemarks = string.Empty;
                        if (!interfacedMember.Remarks.Contains(Configuration.ToBeAdded))
                        {
                            cleanedInterfaceRemarks = interfacedMember.Remarks.RemoveSubstrings("##Remarks", "## Remarks", "<![CDATA[", "]]>");
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

                if (!IsEmpty(value))
                {
                    // Any member can have an empty remark
                    string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} remarks: {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dApiToUpdate.FilePath, dApiToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the parameter descriptions for the specified API if any of them is undocumented.
        private void TryPortMissingParamsForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
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
                    if (IsEmpty(dParam.Value))
                    {
                        created = false;
                        isEII = false;
                        name = string.Empty;
                        value = string.Empty;

                        TripleSlashParam tsParam = tsMemberToPort.Params.FirstOrDefault(x => x.Name == dParam.Name);

                        // When not found, it's a bug in Docs (param name not the same as source/ref), so need to ask the user to indicate correct name
                        if (tsParam == null)
                        {
                            ProblematicAPIs.AddIfNotExists($"Param=[{dParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");

                            if (tsMemberToPort.Params.Count() == 0)
                            {
                                ProblematicAPIs.AddIfNotExists($"Param=[{dParam.Name}] in Member DocId=[{dApiToUpdate.DocId}]");
                                Log.Warning($"  There were no triple slash comments for param '{dParam.Name}' in {dApiToUpdate.DocId}");
                            }
                            else
                            {
                                created = TryPromptParam(dParam, tsMemberToPort, out TripleSlashParam? newTsParam);
                                if (newTsParam == null)
                                {
                                    Log.Error($"  There param '{dParam.Name}' was not found in triple slash for {dApiToUpdate.DocId}");
                                }
                                else
                                {
                                    // Now attempt to document it
                                    if (!IsEmpty(newTsParam.Value))
                                    {
                                        // try to port triple slash comments
                                        dParam.Value = newTsParam.Value;
                                        name = newTsParam.Name;
                                        value = newTsParam.Value;
                                    }
                                    // or try to find if it implements a documented interface
                                    else if (interfacedMember != null)
                                    {
                                        DocsParam interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == newTsParam.Name || x.Name == dParam.Name);
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
                        else if (!IsEmpty(tsParam.Value))
                        {
                            // try to port triple slash comments
                            dParam.Value = tsParam.Value;
                            name = tsParam.Name;
                            value = tsParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsParam interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                            if (interfacedParam != null)
                            {
                                dParam.Value = interfacedParam.Value;
                                name = interfacedParam.Name;
                                value = interfacedParam.Value;
                                isEII = true;
                            }
                        }
                        

                        if (!IsEmpty(value))
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
                    if (IsEmpty(dParam.Value))
                    {
                        DocsParam interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                        if (interfacedParam != null && !IsEmpty(interfacedParam.Value))
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
        private void TryPortMissingTypeParamsForAPI(IDocsAPI dApiToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeTypeParams ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberTypeParams)
            {
                return;
            }

            if (tsMemberToPort != null)
            {
                foreach (TripleSlashTypeParam tsTypeParam in tsMemberToPort.TypeParams)
                {
                    bool isEII = false;
                    string name = string.Empty;
                    string value = string.Empty;

                    DocsTypeParam dTypeParam = dApiToUpdate.TypeParams.FirstOrDefault(x => x.Name == tsTypeParam.Name);

                    bool created = false;
                    if (dTypeParam == null)
                    {
                        ProblematicAPIs.AddIfNotExists($"TypeParam=[{tsTypeParam.Name}] in Member=[{dApiToUpdate.DocId}]");
                        dTypeParam = dApiToUpdate.AddTypeParam(tsTypeParam.Name, XmlHelper.GetNodesInPlainText(tsTypeParam.XETypeParam));
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
                        string message = $"{dApiToUpdate.Kind} {GetIsEII(isEII)} ({GetIsCreated(created)}) typeparam {name.Escaped()} = {value.Escaped()}";
                        PrintModifiedMember(message, dTypeParam.ParentAPI.FilePath, dApiToUpdate.DocId);
                        TotalModifiedIndividualElements++;
                    }
                }
            }
        }

        // Tries to document the passed property.
        private void TryPortMissingPropertyForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (!Config.PortMemberProperties)
            {
                return;
            }

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
                    string message = $"Member {GetIsEII(isEII)} property {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath,dMemberToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Tries to document the passed method.
        private void TryPortMissingMethodForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (!Config.PortMemberReturns)
            {
                return;
            }

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
                    string message = $"Method {GetIsEII(isEII)} returns {name.Escaped()} = {value.Escaped()}";
                    PrintModifiedMember(message, dMemberToUpdate.FilePath, dMemberToUpdate.DocId);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the exceptions for the specified API.
        // They are only processed if the user specified in the command arguments to NOT skip exceptions.
        // All exceptions get ported, because there is no easy way to determine if an exception is already documented or not.
        private void TryPortMissingExceptionsForMember(DocsMember dMemberToUpdate, TripleSlashMember? tsMemberToPort)
        {
            if (!Config.PortMemberExceptions)
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
                        string text = XmlHelper.ReplaceExceptionPatterns(XmlHelper.GetNodesInPlainText(tsException.XEException));
                        dException = dMemberToUpdate.AddException(tsException.Cref, text);
                        created = true;
                    }
                    // If cref exists, check if the text has already been appended
                    else
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

                    if (created || (!IsEmpty(tsException.Value) && IsEmpty(dException.Value)))
                    {
                        string message = string.Format($"Exception ({GetIsCreated(created)}) {dException.Cref.Escaped()} = {dException.Value.Escaped()}");
                        PrintModifiedMember(message, dException.ParentAPI.FilePath, dException.Cref);

                        TotalModifiedIndividualElements++;
                    }
                }
            }
        }

        // If a Param is found in a DocsType or a DocsMember that did not exist in the Triple Slash member, it's possible the param was unexpectedly saved in the triple slash comments with a different name, so the user gets prompted to look for it.
        private bool TryPromptParam(DocsParam oldDParam, TripleSlashMember tsMember, out TripleSlashParam? newTsParam)
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
                Log.Info("    1 - Select the correct triple slash param from the existing ones.");
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
                                    Log.Info($"Triple slash params found in member '{tsMember.Name}':");
                                    Log.Warning("    0 - Exit program.");
                                    int paramCounter = 1;
                                    foreach (TripleSlashParam param in tsMember.Params)
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

        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        private bool IsEmpty(string? s)
        {
            return string.IsNullOrWhiteSpace(s) || s == Configuration.ToBeAdded;
        }

        private bool CanAnalyzeAPI(DocsAPI api)
        {
            bool result = IsTypeAllowed(api);
            if (result)
            {
                foreach (DocsAssemblyInfo apiAssembly in api.AssemblyInfos)
                {
                    foreach (string excluded in Config.ExcludedAssemblies)
                    {
                        if (apiAssembly.AssemblyName.StartsWith(excluded))
                        {
                            return false; // No more analysis required
                        }
                    }

                    foreach (string included in Config.IncludedAssemblies)
                    {
                        if (apiAssembly.AssemblyName.StartsWith(included))
                        {
                            result = true; // Almost done, need to check types if needed
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private bool IsTypeAllowed(DocsAPI api)
        {
            // All types are allowed
            if (Config.ExcludedTypes.Count() == 0 &&
                Config.IncludedTypes.Count() == 0)
            {
                return true;
            }

            string typeName;
            string typeFullName;

            if (api is DocsType type)
            {
                typeName = type.Name;
                typeFullName = type.FullName;
            }
            else if (api is DocsMember member)
            {
                typeName = member.ParentType.Name;
                typeFullName = member.ParentType.FullName;
            }
            else
            {
                throw new InvalidCastException();
            }

            if (Config.ExcludedTypes.Count() > 0)
            {
                if (Config.ExcludedTypes.Contains(typeName) || Config.ExcludedTypes.Contains(typeFullName))
                {
                    return false;
                }
            }
            if (Config.IncludedTypes.Count() > 0)
            {
                if (Config.IncludedTypes.Contains(typeName) || Config.IncludedTypes.Contains(typeFullName))
                {
                    return true;
                }
            }

            return false;
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
