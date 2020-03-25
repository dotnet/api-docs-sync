using System;
using System.IO;
using Xunit;

namespace DocsPortingTool.Tests
{
    public class TestDirectory : IDisposable
    {
        private DirectoryInfo DirInfo;

        public string FullPath => DirInfo.FullName;

        public TestDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            DirInfo = new DirectoryInfo(path);
            DirInfo.Create();
            Assert.True(DirInfo.Exists);
        }

        public DirectoryInfo CreateSubdirectory(string dirName)
        {
            return DirInfo.CreateSubdirectory(dirName);
        }

        public void Dispose()
        {
            try
            {
                DirInfo.Delete(recursive: true);
            }
            catch
            {
            }
        }
    }
}
