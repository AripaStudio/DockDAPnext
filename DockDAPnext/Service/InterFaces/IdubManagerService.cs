using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockDAPnext.Service.InterFaces
{
    public interface IdubManagerService
    {
        public Task<DubConfigAP> CreateOrLoadDubFileAP();

        public Task<DubBuildResult> BuildDubAP();

        public Task<DubBuildResult> BuildDebugDubAP();

        public Task<DubBuildResult> BuildReleaseDubAP();

        public Task<DubRunResult> RunBuiltExecutableAP(string workingDirectory);

        public  Task OpenDubFileAP();

        public Task<bool> SaveFileAP(DubConfigAP dubConfigAp);

        public Task<DprojSyncResult> SyncSourceFilesFromDprojAP();
    }
}
