namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Local LLM integration using Ollama's REST API.
    /// </summary>
    public class OllamaAIHelper : LLMClientBase
    {
        private const string ApiKey = "Ollama"; // Placeholder; Ollama doesn’t usually need auth
        private const string ApiHost = "http://localhost:11434";
        private const string Model = "mistral-nemo";

        public OllamaAIHelper(HttpClient httpClient) : base(httpClient)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public override async Task<string> GetResponseAsync(string userPrompt, string systemPrompt)
        {
            // No max_tokens typically needed for Ollama
            var body = CreateChatRequestBody(Model, systemPrompt, userPrompt);

            var response = await SendPostRequestAsync($"{ApiHost}/v1/chat/completions", body);
            return ParseChatCompletionContent(response);
        }
    }
}