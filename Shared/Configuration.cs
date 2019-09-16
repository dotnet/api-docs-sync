using System.Collections.Generic;
using System.IO;

namespace Shared
{
    public static class Configuration
    {
        #region Public members

        #region Arrays

        public static readonly string[] AllowedAssemblyPrefixes = new string[] { "System", "Microsoft", "Windows" };
        public static readonly string[] ForbiddenDirectories = new[] { "binplacePackages", "docs", "mscorlib", "native", "netfx", "netstandard", "pkg", "Product", "ref", "runtime", "shimsTargetRuntime", "testhost", "tests", "winrt" };

        #endregion

        #region Lists

        public static readonly List<DirectoryInfo> DirsTripleSlashXmls = new List<DirectoryInfo>();
        public static readonly List<string> IncludedAssemblies = new List<string>();
        public static readonly List<string> ExcludedAssemblies = new List<string>();

        #endregion

        #endregion

        #region Public properties

        public static bool Save { get; set; }
        public static DirectoryInfo DirDocsXml { get; set; }
        public static bool PrintUndoc { get; set; }

        #endregion

        #region Public methods

        public static bool HasAllowedAssemblyPrefix(string pathName)
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
