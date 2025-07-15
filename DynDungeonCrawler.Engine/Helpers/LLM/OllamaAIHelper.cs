namespace DynDungeonCrawler.Engine.Helpers.LLM
{
    /// <summary>
    /// Local LLM integration using Ollama's REST API.
    /// </summary>
    public class OllamaAIHelper : LLMClientBase
    {
        private const string ApiKey = "Ollama"; // Placeholder; Ollama doesn’t usually need auth
        private readonly string _apiHost;
        private readonly string _model;

        /// <summary>
        /// Initializes a new instance of <see cref="OllamaAIHelper"/>.
        /// </summary>
        /// <param name="httpClient">The shared HttpClient instance.</param>
        /// <param name="apiHost">The Ollama endpoint URL (must not be null or empty).</param>
        /// <param name="model">The Ollama model name (must not be null or empty).</param>
        /// <exception cref="ArgumentException">Thrown if apiHost or model is null or empty.</exception>
        public OllamaAIHelper(HttpClient httpClient, string apiHost, string model) : base(httpClient)
        {
            if (string.IsNullOrWhiteSpace(apiHost))
            {
                throw new ArgumentException("Ollama endpoint URL is required.", nameof(apiHost));
            }
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Ollama model is required.", nameof(model));
            }

            _apiHost = apiHost;
            _model = model;
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public override async Task<string> GetResponseAsync(string userPrompt, string systemPrompt)
        {
            // No max_tokens typically needed for Ollama
            object body = CreateChatRequestBody(_model, systemPrompt, userPrompt);

            string response = await SendPostRequestAsync($"{_apiHost}/v1/chat/completions", body);
            return ParseChatCompletionContent(response);
        }
    }
}