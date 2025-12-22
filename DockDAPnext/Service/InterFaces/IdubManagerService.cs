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

        public Task BuildDubAP();

        public Task BuildDebugDubAP();

        public Task BuildReleaseDubAP();

        public  Task OpenDubFileAP();

        public Task SaveFileAP(DubConfigAP dubConfigAp);

        public DubConfigAP AddToConfigAp(string name, string description, List<string> authors, string license,
            string targetName, string TargetType, List<string> sourceFiles, Dictionary<string, string> dependencies,
            Dictionary<string, BuildConfiguration> buildConfigurations
            , Dictionary<string, object> otherData);
    }
}
