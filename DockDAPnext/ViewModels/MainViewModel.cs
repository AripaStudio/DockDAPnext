using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockDAPnext.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public VMaboutMe vmAboutMe { get; }

        public VMshowDubFile vmShowDubFile { get; }

        public VMbuildDubFile vmBuildDubFile { get; }

        public VMprojectDubManager vmProjectDubManager { get; }

        public MainViewModel(VMprojectDubManager vmProjectDubManager , VMaboutMe vmAboutMe , VMbuildDubFile vmBuildDub , VMshowDubFile vmShowDub)
        {
            this.vmProjectDubManager = vmProjectDubManager;
            this.vmAboutMe = vmAboutMe;
            this.vmBuildDubFile = vmBuildDub;
            this.vmShowDubFile = vmShowDub;

        }
    }
}
