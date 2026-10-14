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
        private const string Github_link = "https://github.com/AripaStudio/DockDAPnext";
        private const string WebSite_link = "https://aripapars.top/";
        private const string Telegram_link = "https://t.me/AripaStudio";

        // Public: WPF binding silently ignores non-public members, so the About buttons did nothing.
        public ReactiveCommand<Unit, Unit> ButtonCommand_GithubLink { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_TelegramLink { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_WebSiteLink { get; }

        public VMaboutMe()
        {
            ButtonCommand_GithubLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAP.OpenLinkAP(Github_link);
            }));

            ButtonCommand_TelegramLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAP.OpenLinkAP(Telegram_link);
            }));

            ButtonCommand_WebSiteLink = Guard(ReactiveCommand.Create(() =>
            {
                BtnManagerAP.OpenLinkAP(WebSite_link);
            }));
        }



    }


}
