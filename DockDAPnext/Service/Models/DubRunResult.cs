namespace DockDAPnext.Service
{
    // Outcome of launching the built .exe (run-after-build feature).
    public class DubRunResult
    {
        public bool Started { get; set; }

        public string ExecutablePath { get; set; }

        public string FailureReason { get; set; }

        public static DubRunResult Ok(string exePath) =>
            new DubRunResult { Started = true, ExecutablePath = exePath };

        public static DubRunResult Fail(string reason) =>
            new DubRunResult { Started = false, FailureReason = reason };
    }
}
