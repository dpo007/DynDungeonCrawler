using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class OpenAIHelper : ILLMClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIHelper(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GetResponseAsync(string userPrompt, string systemPrompt = LLMDefaults.DefaultSystemPrompt)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                max_tokens = 500 // you can adjust
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API call failed: {response.StatusCode} - {error}");
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