using System.Threading.Tasks;

namespace DockDAPnext.Service.InterFaces
{
    public interface IDubManagerService
    {
        public Task<OperationResult<DubConfigAp>> CreateOrLoadDubFileApAsync();

        public Task<OperationResult<string>> GetDubFilePathApAsync();

        public Task<DubBuildResult> BuildDubApAsync();

        public Task<DubBuildResult> BuildDebugDubApAsync();

        public Task<DubBuildResult> BuildReleaseDubApAsync();

        public Task<DubRunResult> RunBuiltExecutableApAsync(string workingDirectory, string outputDirectory);

        public Task<OperationResult<string>> OpenDubFileApAsync();

        public Task<bool> SaveFileApAsync(DubConfigAp dubConfigAp);

        public Task<DprojSyncResult> SyncSourceFilesFromDprojApAsync();
    }
}
