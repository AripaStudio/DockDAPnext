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
    public class MainViewModel : BaseViewModel
    {
        public VMaboutMe vmAboutMe { get; }

        public VMshowDubFile vmShowDubFile { get; }

        public VMbuildDubFile vmBuildDubFile { get; }

        public VMprojectDubManager vmProjectDubManager { get; }

        public VMAiAssistant vmAiAssistant { get; }

        public IdubManagerService dubmanager;


        public ReactiveCommand<Unit, Unit> ButtonCommand_CreateOrLoadFile { get; }
        public ReactiveCommand<Unit, Unit> ButtonCommand_SaveFile { get; }
        public MainViewModel(VMprojectDubManager vmProjectDubManager , VMaboutMe vmAboutMe , VMbuildDubFile vmBuildDub , VMshowDubFile vmShowDub , VMAiAssistant vmAiAssistant , IdubManagerService dubManagerService)
        {
            this.vmProjectDubManager = vmProjectDubManager;
            this.vmAboutMe = vmAboutMe;
            this.vmBuildDubFile = vmBuildDub;
            this.vmShowDubFile = vmShowDub;
            this.vmAiAssistant = vmAiAssistant;
            this.dubmanager = dubManagerService;

            DiagLog.Log("MainViewModel: commands being created");
            ButtonCommand_CreateOrLoadFile = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_CreateOrLoadFile");
                var dubFile = await dubmanager.CreateOrLoadDubFileAP();
                if (dubFile == null)
                {
                    ShowWarning("dub.json was not created or loaded: no active project, or the file is corrupted.");
                    return;
                }

                CurrentDubFileConfigAP.CurrentDubConfigAp = dubFile;
                vmProjectDubManager.LoadFromConfig(dubFile);
                ShowInfo("dub.json was created or loaded.");
            }));

            ButtonCommand_SaveFile = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_SaveFile");
                if (!EnsureConfigLoaded())
                {
                    return;
                }

                var saved = await dubmanager.SaveFileAP(CurrentDubFileConfigAP.CurrentDubConfigAp);
                if (saved)
                {
                    ShowInfo("dub.json was saved.");
                }
                else
                {
                    ShowWarning("Save failed: dub.json was not found or could not be written.");
                }
            }));

        }
    }
}
