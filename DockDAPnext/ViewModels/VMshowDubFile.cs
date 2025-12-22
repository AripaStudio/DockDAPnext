using DockDAPnext.Service.InterFaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DockDAPnext.ViewModels
{
    public class VMshowDubFile : BaseViewModel
    {

        public ReactiveCommand<Unit, Unit> ButtonCommand_OpenDub { get; }

        private IdubManagerService apiDockManager;

        public VMshowDubFile(IdubManagerService DockManager)
        {
            apiDockManager = DockManager;

            ButtonCommand_OpenDub = ReactiveCommand.CreateFromTask(async () =>
            {
                await apiDockManager.OpenDubFileAP();
            });
        }


    }
}
