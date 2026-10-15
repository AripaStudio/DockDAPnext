using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
    // This is deliberately NOT a chat: every request is stateless and must come back as
    // a {summary, changes} JSON proposal that the user confirms before it is applied.
    public interface IAiService
    {
        string ConfigFilePath { get; }

        // The visible, editable instruction from ai.json (or the built-in default).
        string PromptTemplate { get; }

        bool IsConfigured(out string problem);

        // Writes a commented-by-example template config; never overwrites an existing one.
        bool CreateTemplateConfig();

        // The exact text that will be sent, so the UI can show it before/after sending.
        string BuildDubChangeRequest(string userRequest, string projectContext);

        Task<AiReply> AskAsync(string prompt, CancellationToken cancellationToken);

        // One stateless request that must return the {summary, changes} JSON contract.
        Task<AiChangeProposal> AskForDubChangesAsync(string userRequest, string projectContext, CancellationToken cancellationToken);

        // Validates and extracts the JSON contract from raw reply text.
        AiChangeProposal ParseProposal(string replyText);
    }
}
