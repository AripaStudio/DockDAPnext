namespace DockDAPnext.Service
{
    // Outcome of one sourceFiles sync between the .visualdproj and dub.json.
    public class DprojSyncResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public bool Changed { get; set; }

        public int Added { get; set; }

        public int Removed { get; set; }

        public int Total { get; set; }

        public DubConfigAp Config { get; set; }

        public string DubJsonPath { get; set; }

        public string DprojPath { get; set; }
    }
}
