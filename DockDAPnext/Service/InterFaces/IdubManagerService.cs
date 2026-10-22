using System.Collections.Generic;
using System.Threading.Tasks;

namespace DockDAPnext.Service.InterFaces
{
    public interface IDubManagerService
    {
        public Task<OperationResult<DubConfigAp>> CreateOrLoadDubFileApAsync();

        public Task<List<SolutionDubProject>> GetSolutionProjectsApAsync();

        public Task<OperationResult<DubConfigAp>> SelectDubProjectApAsync(SolutionDubProject project);

        public Task<OperationResult<string>> GetDubFilePathApAsync();

        public Task<DubBuildResult> BuildDubApAsync();

        public Task<DubBuildResult> BuildDebugDubApAsync();

        public Task<DubBuildResult> BuildReleaseDubApAsync();

        public Task<DubRunResult> RunBuiltExecutableApAsync(string workingDirectory, string outputDirectory);

        public Task<OperationResult<string>> OpenDubFileApAsync();

        public Task<OperationResult<string>> SaveFileApAsync(DubConfigAp dubConfigAp);

        public Task<DprojSyncResult> SyncSourceFilesFromDprojApAsync();
    }
}
