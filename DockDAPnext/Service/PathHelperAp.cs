using System;
using System.IO;

namespace DockDAPnext.Service
{
    // Path helpers shared by the services. net48 has no Path.GetRelativePath,
    // so Uri does the job and yields dub's '/' separators.
    public static class PathHelperAp
    {
        // Relative path from one directory to a file, '/' separated; falls back to the
        // absolute target when the two live on different roots or a path is malformed.
        public static string MakeRelative(string fromDirectory, string toFile)
        {
            try
            {
                if (!string.Equals(Path.GetPathRoot(fromDirectory), Path.GetPathRoot(toFile),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return toFile;
                }

                var directory = fromDirectory;
                if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    directory += Path.DirectorySeparatorChar;
                }

                var relative = new Uri(directory).MakeRelativeUri(new Uri(toFile));
                if (relative.IsAbsoluteUri)
                {
                    return toFile;
                }

                return Uri.UnescapeDataString(relative.ToString());
            }
            catch (UriFormatException)
            {
                return toFile;
            }
        }
    }
}
