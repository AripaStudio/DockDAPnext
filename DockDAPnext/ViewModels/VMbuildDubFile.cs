using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using DockDAPnext.Service.InterFaces;
using ReactiveUI;

namespace DockDAPnext.ViewModels
{
    public class VMbuildDubFile : BaseViewModel
    {

        public ReactiveCommand<Unit, Unit> ButtonCommand_buildDebug { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_buildRelease { get; }

         public ReactiveCommand<Unit, Unit> ButtonCommand_buildDefault { get; }

        private IdubManagerService apiDockManager;


        public VMbuildDubFile(IdubManagerService dubManager)
        {

            apiDockManager = dubManager;

            ButtonCommand_buildDebug = ReactiveCommand.CreateFromTask(async () =>
            {
                await apiDockManager.BuildDebugDubAP();
            });

            ButtonCommand_buildDefault = ReactiveCommand.CreateFromTask(async () =>
            {
                await apiDockManager.BuildDubAP();
            });

            ButtonCommand_buildRelease = ReactiveCommand.CreateFromTask(async () =>
            {
                await apiDockManager.BuildReleaseDubAP();
            });
        }
    }
}
