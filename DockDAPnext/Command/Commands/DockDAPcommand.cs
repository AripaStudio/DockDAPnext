namespace DockDAPnext.Command
{
    [Command(PackageGuids.guidDockDAPnextPackageCmdSetString,PackageIds.ShowDockDAPwindowCommand)]
    internal sealed class DockDaPcommand : BaseCommand<DockDaPcommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // Open the DockDAP tool window (no debug dialogs)
            await DockDaPwindow.ShowAsync();
        }
    }
}
