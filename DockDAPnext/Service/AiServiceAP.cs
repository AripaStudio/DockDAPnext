using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DockDAPnext.Service
{
    // Minimal OpenAI-compatible client: works with OpenAI, DeepSeek, Ollama, LM Studio, etc.
    // Credentials live in %APPDATA%\DockDAPnext\ai.json (or DOCKDAP_AI_* env vars) and
    // never ship inside the VSIX (OpenAI-style keys must stay out of client binaries).
    // This half owns the config file: reading it, the template, and the history.
    // The HTTP request/response half lives in AiServiceAp.Request.cs.
    public partial class AiServiceAp : IAiService
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
            "  \"changes\": { <a PARTIAL dub.json object with ONLY the keys that must change, using dub's own schema: name, description, license, targetName, targetType, sourceFiles, dependencies, configurations, ...> }\n" +
            "}\n" +
            "Rules:\n" +
            "- 'changes' must be a valid fragment of dub.json as the D package manager dub understands it.\n" +
            "- Omit every key that must stay unchanged; do not repeat the whole file.\n" +
            "- Never invent keys that dub does not know, and never include personal data.\n" +
            "- If the request needs no change, answer {\"summary\":\"<why no change is needed>\",\"changes\":{}}.";

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
                var template = new AiConfigAp
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

        // Newest-first list of previous requests; read raw from ai.json so it also works
        // before any endpoint is configured (history is not secret and not endpoint-bound).
        public IReadOnlyList<string> GetHistory()
        {
            var history = ReadRawConfig()?.History;
            return history ?? (IReadOnlyList<string>)new List<string>();
        }

        // Remembers a request at the top of the history (capped, no duplicates); writes
        // the raw file so environment-variable overrides are never baked into ai.json.
        public bool AddToHistory(string request)
        {
            var text = (request ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                var config = ReadRawConfig() ?? new AiConfigAp();
                var history = config.History ?? new List<string>();
                history.RemoveAll(item => string.Equals(item?.Trim(), text, StringComparison.Ordinal));
                history.Insert(0, text);
                while (history.Count > MaxHistory)
                {
                    history.RemoveAt(history.Count - 1);
                }

                config.History = history;
                SaveRawConfig(config);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The prompt template belongs to ai.json, not to the request box; this writes it
        // back after the user viewed/edited it. An empty template is never accepted.
        public bool SetPromptTemplate(string template)
        {
            var text = (template ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                var config = ReadRawConfig() ?? new AiConfigAp
                {
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    ApiKey = string.Empty,
                    Model = "gpt-4o-mini",
                    ApiFormat = "chat"
                };
                config.PromptTemplate = text;
                SaveRawConfig(config);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public const int MaxHistory = 10;

        // File-level read/write of ai.json without the DOCKDAP_AI_* overrides, so saving
        // never copies environment-only values into the file.
        private AiConfigAp ReadRawConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    return JsonConvert.DeserializeObject<AiConfigAp>(File.ReadAllText(ConfigFilePath));
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private void SaveRawConfig(AiConfigAp config)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private AiConfigAp LoadConfig()
        {
            AiConfigAp config = ReadRawConfig();

            config = config ?? new AiConfigAp();

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

        private static bool IsLocalEndpoint(string endpoint) =>
            endpoint.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
            endpoint.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
