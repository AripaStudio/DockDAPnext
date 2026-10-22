namespace DockDAPnext.Service
{
    // One row of the project selector: a D project, a dub.json holder, or both.
    public class SolutionDubProject
    {
        public string Name { get; set; } = string.Empty;

        // Full path of the .visualdproj / .dproj; empty when the project only has dub.json.
        public string DprojPath { get; set; } = string.Empty;

        public string Directory { get; set; } = string.Empty;

        // Empty until 'Create or load File' creates the file in this folder.
        public string DubJsonPath { get; set; } = string.Empty;

        public bool HasDproj => !string.IsNullOrEmpty(DprojPath);

        public bool HasDubJson => !string.IsNullOrEmpty(DubJsonPath);

        // Shown in the selector: a missing file is spelled out, so an empty tab is never a mystery.
        public string DisplayName =>
            Name
            + (HasDproj ? string.Empty : " (no D project)")
            + (HasDubJson ? string.Empty : " — no dub.json");

        // UI Automation reads ToString(): return the readable name, not the type name.
        public override string ToString() => DisplayName;
    }
}
