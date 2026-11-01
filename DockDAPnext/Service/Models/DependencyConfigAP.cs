#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockDAPnext.Service.Models
{
    public enum DependencyKind
    {
        Registry,
        Git,
        Path,

        // Entries dub accepts but this editor cannot shape; kept visible instead of dropped.
        Unsupported
    }

    public record DubDependencyInfo
    {
        public string Name { get; init; } = string.Empty;
        public DependencyKind Kind { get; init; }
        public string? Version { get; init; }
        public string? Repository { get; init; }
        public string? Path { get; init; }
        public string? RawJson { get; init; }

        // One label per row that also makes the three dub shapes visibly different.
        public string DisplayText
        {
            get
            {
                switch (Kind)
                {
                    case DependencyKind.Git:
                        return string.IsNullOrEmpty(Version)
                            ? $"{Name}: {Repository} (git)"
                            : $"{Name}: {Repository} @ {Version} (git)";

                    case DependencyKind.Path:
                        return string.IsNullOrEmpty(Version)
                            ? $"{Name}: {Path} (path)"
                            : $"{Name}: {Path} @ {Version} (path)";

                    case DependencyKind.Unsupported:
                        return $"{Name}: {RawJson} (unsupported)";

                    default:
                        return $"{Name}: {Version} (registry)";
                }
            }
        }
    }
}
