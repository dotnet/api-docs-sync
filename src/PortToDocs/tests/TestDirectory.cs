// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace ApiDocsSync.Libraries.Tests
{
    public class TestDirectory : IDisposable
    {
        private readonly DirectoryInfo DirInfo;

        public string FullPath => DirInfo.FullName;

        public TestDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            DirInfo = new DirectoryInfo(path);
            DirInfo.Create();
            Assert.True(DirInfo.Exists, "Verify root test directory exists.");
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
            GC.SuppressFinalize(this);
        }
    }
}
