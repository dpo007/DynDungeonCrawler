namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Local LLM integration using Ollama's REST API.
    /// </summary>
    public class OllamaAIHelper : LLMClientBase
    {
        private const string ApiKey = "Ollama"; // Placeholder; Ollama doesn’t usually need auth
        private readonly string _apiHost;
        private const string Model = "mistral-nemo";

        /// <summary>
        /// Initializes a new instance of <see cref="OllamaAIHelper"/>.
        /// </summary>
        /// <param name="httpClient">The shared HttpClient instance.</param>
        /// <param name="apiHost">The Ollama endpoint URL (must not be null or empty).</param>
        /// <exception cref="ArgumentException">Thrown if apiHost is null or empty.</exception>
        public OllamaAIHelper(HttpClient httpClient, string apiHost) : base(httpClient)
        {
            if (string.IsNullOrWhiteSpace(apiHost))
            {
                throw new ArgumentException("Ollama endpoint URL is required.", nameof(apiHost));
            }

            _apiHost = apiHost;
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public override async Task<string> GetResponseAsync(string userPrompt, string systemPrompt)
        {
            // No max_tokens typically needed for Ollama
            object body = CreateChatRequestBody(Model, systemPrompt, userPrompt);

            string response = await SendPostRequestAsync($"{_apiHost}/v1/chat/completions", body);
            return ParseChatCompletionContent(response);
        }
    }
}