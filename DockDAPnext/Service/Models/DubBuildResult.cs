namespace DockDAPnext.Service
{
    // Outcome of one 'dub build' run; the ViewModel turns it into visible status + a notification.
    public class DubBuildResult
    {
        public bool Started { get; set; }

        public int ExitCode { get; set; }

        public string FailureReason { get; set; }

        // The folder dub ran in; also where its .exe output lands unless dub.json says otherwise.
        public string WorkingDirectory { get; set; }

        // The folder the built binary lands in for the chosen configuration
        // (its targetPath, the package targetPath, or the dub.json folder).
        public string OutputDirectory { get; set; }

        public bool Success => Started && ExitCode == 0;

        public static DubBuildResult NotStarted(string reason) =>
            new DubBuildResult { Started = false, FailureReason = reason };
    }
}
