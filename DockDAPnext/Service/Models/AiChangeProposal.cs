using Newtonsoft.Json.Linq;

namespace DockDAPnext.Service
{
    // The AI never touches code: it may only propose a partial dub.json. This is the
    // parsed, validated proposal the user gets to confirm before anything is written.
    public class AiChangeProposal
    {
        public bool Success { get; set; }

        public string Summary { get; set; } = string.Empty;

        public JObject Changes { get; set; }

        public string RawText { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        public bool HasChanges => Success && Changes != null && Changes.Count > 0;
    }
}
