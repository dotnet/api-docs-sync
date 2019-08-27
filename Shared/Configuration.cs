using System.Collections.Generic;
using System.IO;

namespace Shared
{
    public class Configuration
    {
        #region Constructor

        public Configuration()
        {
        }

        #endregion

        #region Public members

        #region Arrays

        public readonly string[] AllowedAssemblyPrefixes = new string[] { "System", "Microsoft", "Windows" };
        public readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "testhost", "tests", "winrt" };

        #endregion

        #region Lists

        public readonly List<DirectoryInfo> DirsTripleSlashXmls = new List<DirectoryInfo>();
        public readonly List<string> IncludedAssemblies = new List<string>();
        public readonly List<string> ExcludedAssemblies = new List<string>();

        #endregion

        #endregion

        #region Public properties

        public DirectoryInfo DirDocsXml { get; set; }
        public bool Save { get; set; }
        public bool PrintUndoc { get; set; }

        #endregion

        #region Public methods

        public bool HasAllowedAssemblyPrefix(string pathName)
        {
            foreach (string prefix in AllowedAssemblyPrefixes)
            {
                if (pathName.StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
