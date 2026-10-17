using Newtonsoft.Json;
using System.Collections.Generic;

namespace DockDAPnext.Service
{
    // Shape of %APPDATA%\DockDAPnext\ai.json: endpoint, credentials, model and the
    // visible prompt template the user can edit.
    public class AiConfigAp
    {
        [JsonProperty("endpoint")] public string Endpoint { get; set; }

        [JsonProperty("apiKey")] public string ApiKey { get; set; }

        [JsonProperty("model")] public string Model { get; set; }

        // "chat" = /v1/chat/completions (default, works everywhere);
        // "responses" = /v1/responses (OpenAI's newer interface).
        [JsonProperty("apiFormat")] public string ApiFormat { get; set; }

        // The fixed instruction prepended to every request. It is stored in ai.json on
        // purpose: the user must be able to read and edit exactly what gets sent.
        [JsonProperty("promptTemplate")] public string PromptTemplate { get; set; }

        // The most recent requests the user sent (newest first), so one click can reuse one.
        [JsonProperty("history")] public List<string> History { get; set; } = new List<string>();
    }
}
