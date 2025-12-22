namespace DockDAPnext.Command
{
    [Command(PackageIds.ShowDockDAPwindowCommand)]
    internal sealed class DockDAPcommand : BaseCommand<DockDAPcommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await DockDAPwindow.ShowAsync();
        }
    }
}
