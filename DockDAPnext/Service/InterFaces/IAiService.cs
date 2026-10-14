using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.Service.InterFaces
{
    public class AiReply
    {
        public bool Success { get; set; }

        public string Text { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;
    }

    // Contract for talking to an LLM. The extension ships one implementation
    // (OpenAI-compatible REST); other providers or an MCP bridge can implement this too.
    public interface IAiService
    {
        string ConfigFilePath { get; }

        bool IsConfigured(out string problem);

        // Writes a commented-by-example template config; never overwrites an existing one.
        bool CreateTemplateConfig();

        Task<AiReply> AskAsync(string prompt, CancellationToken cancellationToken);
    }
}
