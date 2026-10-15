using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.Service
{
    public class AiConfigAP
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
    }

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

    // Minimal OpenAI-compatible client: works with OpenAI, DeepSeek, Ollama, LM Studio, etc.
    // Credentials live in %APPDATA%\DockDAPnext\ai.json (or DOCKDAP_AI_* env vars) and
    // never ship inside the VSIX (OpenAI-style keys must stay out of client binaries).
    public class AiServiceAP : IAiService
    {
        // Kept short and boring on purpose: it is visible in ai.json and only constrains
        // the shape of the answer (a partial dub.json), never the content of the project.
        public const string DefaultPromptTemplate =
            "You are a dub.json configuration assistant built into a Visual Studio extension.\n" +
            "You receive two blocks: a PROJECT CONTEXT (the current dub.json content and file paths - no personal data) " +
            "and a USER REQUEST about the project's dub.json.\n" +
            "Answer with ONLY one JSON object and nothing else - no prose, no markdown code fences - in this exact shape:\n" +
            "{\n" +
            "  \"summary\": \"<1-2 plain sentences describing what you would change, in the language of the user request>\",\n" +
            "  \"changes\": { <a PARTIAL dub.json object with ONLY the keys that must change, using dub's own schema: name, description, license, targetName, targetType, sourceFiles, dependencies, buildConfigurations, ...> }\n" +
            "}\n" +
            "Rules:\n" +
            "- 'changes' must be a valid fragment of dub.json as the D package manager dub understands it.\n" +
            "- Omit every key that must stay unchanged; do not repeat the whole file.\n" +
            "- Never invent keys that dub does not know, and never include personal data.\n" +
            "- If the request needs no change, answer {\"summary\":\"<why no change is needed>\",\"changes\":{}}.";

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        public string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DockDAPnext", "ai.json");

        // Effective template: whatever ai.json holds, or the built-in default for old configs.
        public string PromptTemplate
        {
            get
            {
                var template = LoadConfig()?.PromptTemplate;
                return string.IsNullOrWhiteSpace(template) ? DefaultPromptTemplate : template;
            }
        }

        public bool IsConfigured(out string problem)
        {
            var config = LoadConfig();
            if (config == null)
            {
                problem = $"AI is not configured. Create {ConfigFilePath} (button: 'Create AI config').";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.Endpoint) || string.IsNullOrWhiteSpace(config.Model))
            {
                problem = $"AI config is incomplete: 'endpoint' and 'model' must be set in {ConfigFilePath}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.ApiKey) && !IsLocalEndpoint(config.Endpoint))
            {
                problem = $"AI config has no 'apiKey' in {ConfigFilePath} (required for remote endpoints; local servers like Ollama or LM Studio need none).";
                return false;
            }

            problem = null;
            return true;
        }

        public bool CreateTemplateConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
                var template = new AiConfigAP
                {
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    ApiKey = "",
                    Model = "gpt-4o-mini",
                    ApiFormat = "chat",
                    PromptTemplate = DefaultPromptTemplate
                };
                File.WriteAllText(ConfigFilePath,
                    JsonConvert.SerializeObject(template, Formatting.Indented));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Builds the exact text that will be sent, so the UI can preview it: the visible
        // template from ai.json, then the project context, then the user's own request.
        public string BuildDubChangeRequest(string userRequest, string projectContext)
        {
            var builder = new StringBuilder();
            builder.AppendLine(PromptTemplate);
            builder.AppendLine();
            builder.AppendLine("[PROJECT CONTEXT]");
            builder.AppendLine(string.IsNullOrWhiteSpace(projectContext)
                ? "(no dub.json is loaded yet - the request can only be generic)"
                : projectContext.Trim());
            builder.AppendLine();
            builder.AppendLine("[USER REQUEST]");
            builder.AppendLine((userRequest ?? string.Empty).Trim());
            return builder.ToString();
        }

        public async Task<AiReply> AskAsync(string prompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Fail("The prompt is empty.");
            }

            if (!IsConfigured(out var problem))
            {
                return Fail(problem);
            }

            var config = LoadConfig();

            try
            {
                var endpoint = NormalizeEndpoint(config.Endpoint, config.ApiFormat);
                var usesResponses = IsResponsesFormat(config.ApiFormat);
                var payload = usesResponses
                    ? JsonConvert.SerializeObject(new { model = config.Model, input = prompt })
                    : JsonConvert.SerializeObject(new
                    {
                        model = config.Model,
                        messages = new[] { new { role = "user", content = prompt } },
                        temperature = 0.2
                    });

                using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                using (var response = await Http.PostAsync(endpoint, content, cancellationToken))
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return Fail($"AI request failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {Truncate(body, 300)}");
                    }

                    var replyText = usesResponses ? ParseResponsesBody(body) : ParseChatBody(body);
                    if (string.IsNullOrEmpty(replyText))
                    {
                        return Fail(usesResponses
                            ? "The AI response did not contain output text (output[].content[].text)."
                            : "The AI response did not contain choices[0].message.content.");
                    }

                    return new AiReply { Success = true, Text = replyText };
                }
            }
            catch (TaskCanceledException)
            {
                return Fail("The AI request timed out or was canceled.");
            }
            catch (HttpRequestException ex)
            {
                return Fail($"The AI endpoint is unreachable: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Fail($"The AI response could not be parsed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"The AI request failed: {ex.Message}");
            }
        }

        // One stateless request that must come back as the {summary, changes} JSON contract.
        public async Task<AiChangeProposal> AskForDubChangesAsync(
            string userRequest, string projectContext, CancellationToken cancellationToken)
        {
            var request = BuildDubChangeRequest(userRequest, projectContext);
            var reply = await AskAsync(request, cancellationToken);

            var proposal = new AiChangeProposal { RawText = reply.Text };

            if (!reply.Success)
            {
                proposal.Success = false;
                proposal.Error = reply.Error;
                return proposal;
            }

            return ParseProposal(reply.Text);
        }

        // The model is told "JSON only", but markdown fences still slip through; strip them
        // and take the outermost {...} so a chatty model cannot break the whole proposal.
        public AiChangeProposal ParseProposal(string replyText)
        {
            var proposal = new AiChangeProposal { RawText = replyText ?? string.Empty };

            var text = (replyText ?? string.Empty).Trim();
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = text.IndexOf('\n');
                text = firstLineEnd >= 0 ? text.Substring(firstLineEnd + 1) : text;
                var fence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (fence >= 0)
                {
                    text = text.Substring(0, fence);
                }
                text = text.Trim();
            }

            var open = text.IndexOf('{');
            var close = text.LastIndexOf('}');
            if (open < 0 || close <= open)
            {
                proposal.Success = false;
                proposal.Error = "The AI answered with text, not the required JSON object. " +
                    "Try again, or lower the temperature in your AI server, or check the model supports instruction following.";
                return proposal;
            }

            JObject parsed;
            try
            {
                parsed = JObject.Parse(text.Substring(open, close - open + 1));
            }
            catch (JsonException ex)
            {
                proposal.Success = false;
                proposal.Error = $"The AI reply is not valid JSON: {ex.Message}";
                return proposal;
            }

            var summary = parsed["summary"]?.ToString() ?? string.Empty;
            var changesToken = parsed["changes"];

            if (changesToken != null && changesToken.Type != JTokenType.Object)
            {
                proposal.Success = false;
                proposal.Error = "The AI reply has a 'changes' field that is not a JSON object.";
                return proposal;
            }

            proposal.Success = true;
            proposal.Summary = summary;
            proposal.Changes = changesToken as JObject ?? new JObject();
            return proposal;
        }

        private AiConfigAP LoadConfig()
        {
            AiConfigAP config = null;

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    config = JsonConvert.DeserializeObject<AiConfigAP>(File.ReadAllText(ConfigFilePath));
                }
            }
            catch (Exception)
            {
                config = null;
            }

            config = config ?? new AiConfigAP();

            // Environment variables win over the file (CI / shared machines).
            config.Endpoint = Environment.GetEnvironmentVariable("DOCKDAP_AI_ENDPOINT") ?? config.Endpoint;
            config.ApiKey = Environment.GetEnvironmentVariable("DOCKDAP_AI_API_KEY") ?? config.ApiKey;
            config.Model = Environment.GetEnvironmentVariable("DOCKDAP_AI_MODEL") ?? config.Model;

            if (string.IsNullOrWhiteSpace(config.Endpoint) &&
                string.IsNullOrWhiteSpace(config.ApiKey) &&
                string.IsNullOrWhiteSpace(config.Model))
            {
                return null;
            }

            return config;
        }

        // Accept a plain base URL (http://localhost:11434, https://api.openai.com/v1, ...)
        // so local servers need no path knowledge; an explicit .../chat/completions or
        // .../responses URL is always used as-is.
        private static string NormalizeEndpoint(string endpoint, string apiFormat)
        {
            var trimmed = (endpoint ?? string.Empty).Trim().TrimEnd('/');
            if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (!trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                trimmed += "/v1";
            }

            return trimmed + (IsResponsesFormat(apiFormat) ? "/responses" : "/chat/completions");
        }

        private static bool IsResponsesFormat(string apiFormat) =>
            string.Equals(apiFormat?.Trim(), "responses", StringComparison.OrdinalIgnoreCase);

        private static string ParseChatBody(string body) =>
            JObject.Parse(body)["choices"]?[0]?["message"]?["content"]?.ToString();

        private static string ParseResponsesBody(string body)
        {
            var json = JObject.Parse(body);
            var builder = new StringBuilder();

            if (json["output"] is JArray output)
            {
                foreach (var item in output)
                {
                    if (item["type"]?.ToString() != "message" || !(item["content"] is JArray content))
                    {
                        continue;
                    }

                    foreach (var part in content)
                    {
                        var partText = part["text"]?.ToString();
                        if (!string.IsNullOrEmpty(partText))
                        {
                            builder.AppendLine(partText);
                        }
                    }
                }
            }

            return builder.ToString().Trim();
        }

        private static bool IsLocalEndpoint(string endpoint) =>
            endpoint.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
            endpoint.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Truncate(string text, int maxLength) =>
            string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";

        private static AiReply Fail(string error) => new AiReply { Success = false, Error = error };
    }
}
