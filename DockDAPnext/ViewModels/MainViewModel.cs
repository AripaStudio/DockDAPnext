using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using ReactiveUI;

namespace DockDAPnext.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        public VMaboutMe VmAboutMe { get; }

        public VMshowDubFile VmShowDubFile { get; }

        public VMbuildDubFile VmBuildDubFile { get; }

        public VMprojectDubManager VmProjectDubManager { get; }

        public VmAiAssistant VmAiAssistant { get; }

        public IDubManagerService Dubmanager;


        public ReactiveCommand<Unit, Unit> ButtonCommandCreateOrLoadFile { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommandSaveFile { get; }
        public MainViewModel(VMprojectDubManager vmProjectDubManager , VMaboutMe vmAboutMe , VMbuildDubFile vmBuildDub , VMshowDubFile vmShowDub , VmAiAssistant vmAiAssistant , IDubManagerService dubManagerService)
        {
            this.VmProjectDubManager = vmProjectDubManager;
            this.VmAboutMe = vmAboutMe;
            this.VmBuildDubFile = vmBuildDub;
            this.VmShowDubFile = vmShowDub;
            this.VmAiAssistant = vmAiAssistant;
            this.Dubmanager = dubManagerService;

            ButtonCommandCreateOrLoadFile = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                // OperationResult carries the exact reason, so the user sees why a load
                // failed instead of a generic "corrupted" guess.
                var result = await Dubmanager.CreateOrLoadDubFileApAsync();
                if (!result.IsSuccess)
                {
                    ShowWarning(result.Message);
                    return;
                }

                CurrentDubFileConfigAp.CurrentDubConfigAp = result.Value;
                vmProjectDubManager.LoadFromConfig(result.Value);
                ShowInfo(result.Message ?? "dub.json was created or loaded.");

                // The fresh dub.json changes the row's label, so the selector list is refreshed.
                await RefreshSolutionProjectsAsync();
            }));

            ButtonCommandSaveFile = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                var saved = await Dubmanager.SaveFileApAsync(CurrentDubFileConfigAp.CurrentDubConfigAp);
                if (!saved.IsSuccess)
                {
                    // The service knows which file and why: never replace that with a generic guess.
                    ShowWarning("Save failed: " + saved.Message);
                    return;
                }

                ShowInfo("dub.json was saved.");
            }));

            InitializeSolutionProjects();
        }
    }
}
