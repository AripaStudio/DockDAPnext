using MessageBox = System.Windows.MessageBox;

namespace DockDAPnext.Command
{
    [Command(PackageGuids.guidDockDAPnextPackageCmdSetString,PackageIds.ShowDockDAPwindowCommand)]
    internal sealed class DockDAPcommand : BaseCommand<DockDAPcommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MessageBox.Show("DockDap Buttons is ready");
            //await DockDAPwindow.ShowAsync();
        }
    }
}
