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
using MessageBox = System.Windows.Forms.MessageBox;

namespace DockDAPnext.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public VMaboutMe vmAboutMe { get; }

        public VMshowDubFile vmShowDubFile { get; }

        public VMbuildDubFile vmBuildDubFile { get; }

        public VMprojectDubManager vmProjectDubManager { get; }

        public IdubManagerService dubmanager;


        public ReactiveCommand<Unit, Unit> ButtonCommand_CreateOrLoadFile { get; }
        public ReactiveCommand<Unit , Unit> ButtonCommand_SaveFile { get; }
        public MainViewModel(VMprojectDubManager vmProjectDubManager , VMaboutMe vmAboutMe , VMbuildDubFile vmBuildDub , VMshowDubFile vmShowDub , IdubManagerService dubManagerService)
        {
            this.vmProjectDubManager = vmProjectDubManager;
            this.vmAboutMe = vmAboutMe;
            this.vmBuildDubFile = vmBuildDub;
            this.vmShowDubFile = vmShowDub;
            this.dubmanager = dubManagerService;
            ButtonCommand_CreateOrLoadFile = ReactiveCommand.CreateFromTask(async () =>
            {
                var DubFile =  await dubmanager.CreateOrLoadDubFileAP();
                if (DubFile == null)
                {
                    MessageBox.Show("File could not be loaded.");
                    return;
                }
                CurrentDubFileConfigAP.CurrentDubConfigAp = DubFile;
                MessageBox.Show("Create Or Load successfully");

            });

            ButtonCommand_SaveFile = ReactiveCommand.CreateFromTask( async () =>
            {
                if (CurrentDubFileConfigAP.CurrentDubConfigAp != null)
                {
                    await dubmanager.SaveFileAP(CurrentDubFileConfigAP.CurrentDubConfigAp);
                    MessageBox.Show("File saved successfully.");
                }
                
            });

        }
    }
}
