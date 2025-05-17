using DynDungeonCrawler.Engine.Interfaces;
using System.Text;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Base class for LLM clients, handling common request construction and response parsing.
    /// </summary>
    public abstract class LLMClientBase : ILLMClient
    {
        protected readonly HttpClient _httpClient;

        protected LLMClientBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Subclasses must implement this to send user/system prompts to their specific LLM API.
        /// </summary>
        public abstract Task<string> GetResponseAsync(string userPrompt, string systemPrompt);

        /// <summary>
        /// Sends a POST request with a JSON-encoded payload.
        /// </summary>
        protected async Task<string> SendPostRequestAsync(string url, object requestBody)
        {
            StringContent content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API call failed: {response.StatusCode} - {error}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Constructs a standard chat completion request body for most LLM APIs.
        /// Automatically includes system and user messages.
        /// </summary>
        protected static object CreateChatRequestBody(string model, string systemPrompt, string userPrompt, int? maxTokens = null)
        {
            var messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            };

            // Some APIs require 'max_tokens'; others ignore it.
            return maxTokens.HasValue
                ? new { model, messages, max_tokens = maxTokens.Value }
                : new { model, messages };
        }

        /// <summary>
        /// Extracts the assistant's response content from a standard OpenAI-style chat completion JSON.
        /// </summary>
        protected static string ParseChatCompletionContent(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out JsonElement msg) &&
                msg.TryGetProperty("content", out JsonElement content))
            {
                return content.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}