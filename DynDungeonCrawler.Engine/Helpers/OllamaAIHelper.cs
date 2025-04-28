using DynDungeonCrawler.Engine.Interfaces;
using System.Text;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class OllamaAIHelper : ILLMClient
    {
        private const string ApiKey = "Ollama";
        private const string ApiHost = "http://localhost:11434";
        private const string Model = "mistral-nemo";
        private const int DefaultKeepAliveMinutes = 5;

        private readonly HttpClient _httpClient;

        public OllamaAIHelper()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public async Task<string> GetResponseAsync(string userPrompt, string systemPrompt = "")
        {
            var requestBody = new
            {
                model = Model,
                keep_alive = DefaultKeepAliveMinutes,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"{ApiHost}/api/completions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OllamaAI API call failed: {response.StatusCode} - {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseString);
            var result = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result ?? "";
        }
    }
}
