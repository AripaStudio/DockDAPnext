using System.IO;

namespace DockDAPnext.Service
{
    // The three output folders of a project: Output (default builds), Output\debug and
    // Output\release. Created on demand next to the active project file.
    public struct OutputDirectoryPathsStruct
    {
        public DirectoryInfo OutputDirectoryInfo;
        public DirectoryInfo ReleaseOutputDirectoryInfo;
        public DirectoryInfo DebugOutputDirectoryInfo;
    }
}
