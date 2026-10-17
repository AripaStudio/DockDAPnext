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

        public ReactiveCommand<Unit, Unit> ButtonCommandOpenDub { get; }

        private IDubManagerService _apiDockManager;

        public VMshowDubFile(IDubManagerService dockManager)
        {
            _apiDockManager = dockManager;

            ButtonCommandOpenDub = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                var result = await _apiDockManager.OpenDubFileApAsync();
                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                }
            }));
        }


    }
}
