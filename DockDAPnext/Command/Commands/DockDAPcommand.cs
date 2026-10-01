namespace DockDAPnext.Command
{
    [Command(PackageGuids.guidDockDAPnextPackageCmdSetString,PackageIds.ShowDockDAPwindowCommand)]
    internal sealed class DockDAPcommand : BaseCommand<DockDAPcommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // Open the DockDAP tool window (no debug dialogs)
            await DockDAPwindow.ShowAsync();
        }
    }
}
