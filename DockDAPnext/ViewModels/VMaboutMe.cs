using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using DockDAPnext.Service;
using ReactiveUI;

namespace DockDAPnext.ViewModels
{

    
    public class VMaboutMe : BaseViewModel
    {
        private const string GithubLink = "https://github.com/AripaStudio/DockDAPnext";
        private const string WebSiteLink = "https://aripapars.top/";
        private const string TelegramLink = "https://t.me/AripaStudio";

        // The manifest's version, so the About card cannot drift from the real release number.
        public string VersionText => "v" + Vsix.Version;

        // Public: WPF binding silently ignores non-public members, so the About buttons did nothing.
        public ReactiveCommand<Unit, Unit> ButtonCommandGithubLink { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommandTelegramLink { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommandWebSiteLink { get; }

        public VMaboutMe()
        {
            ButtonCommandGithubLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAp.OpenLinkAp(GithubLink);
            }));

            ButtonCommandTelegramLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAp.OpenLinkAp(TelegramLink);
            }));

            ButtonCommandWebSiteLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAp.OpenLinkAp(WebSiteLink);
            }));
        }



    }


}
