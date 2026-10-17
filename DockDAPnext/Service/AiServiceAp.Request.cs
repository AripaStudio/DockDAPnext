using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.Service
{
    // The request/response half of the OpenAI-compatible client: sending one prompt,
    // and turning the raw reply into a validated {summary, changes} proposal.
    public partial class AiServiceAp
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

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

        private static string Truncate(string text, int maxLength) =>
            string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";

        private static AiReply Fail(string error) => new AiReply { Success = false, Error = error };
    }
}
