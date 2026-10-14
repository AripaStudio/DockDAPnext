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
    }

    // Minimal OpenAI-compatible client: POST {endpoint} with {model, messages} and read
    // choices[0].message.content. Works with OpenAI, DeepSeek, Ollama, LM Studio, etc.
    // Credentials live in %APPDATA%\DockDAPnext\ai.json (or DOCKDAP_AI_* env vars) and
    // never ship inside the VSIX (OpenAI-style keys must stay out of client binaries).
    public class AiServiceAP : IAiService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        public string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DockDAPnext", "ai.json");

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
                problem = $"AI config has no 'apiKey' in {ConfigFilePath} (required for remote endpoints).";
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
                    Model = "gpt-4o-mini"
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
                var payload = JsonConvert.SerializeObject(new
                {
                    model = config.Model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.2
                });

                using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                using (var response = await Http.PostAsync(config.Endpoint, content, cancellationToken))
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return Fail($"AI request failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {Truncate(body, 300)}");
                    }

                    var replyText = JObject.Parse(body)["choices"]?[0]?["message"]?["content"]?.ToString();
                    if (string.IsNullOrEmpty(replyText))
                    {
                        return Fail("The AI response did not contain choices[0].message.content.");
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

        private static bool IsLocalEndpoint(string endpoint) =>
            endpoint.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
            endpoint.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Truncate(string text, int maxLength) =>
            string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";

        private static AiReply Fail(string error) => new AiReply { Success = false, Error = error };
    }
}
