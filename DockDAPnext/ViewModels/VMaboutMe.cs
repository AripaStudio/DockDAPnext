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
        private const string WebSite_link = "https://aripastudio.github.io/AripaStudio/";
        private const string Telegram_link = "https://t.me/AripaStudio";

        private ReactiveCommand<Unit, Unit> ButtonCommand_GithubLink { get; }
        private ReactiveCommand<Unit, Unit> ButtonCommand_TelegramLink { get; }
        private ReactiveCommand<Unit, Unit> ButtonCommand_WebSiteLink { get; }

        public VMaboutMe()
        {
            ButtonCommand_GithubLink = ReactiveCommand.CreateFromTask(async () =>
            {
                BtnManagerAP.OpenLinkAP(Github_link);
            });

            ButtonCommand_TelegramLink = ReactiveCommand.CreateFromTask(async () =>
            {
                BtnManagerAP.OpenLinkAP(Telegram_link);
            });

            ButtonCommand_WebSiteLink = ReactiveCommand.CreateFromTask(async () =>
            {
                BtnManagerAP.OpenLinkAP(WebSite_link);
            });
        }



    }


}
