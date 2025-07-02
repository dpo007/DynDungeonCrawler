namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Implementation of LLMClientBase using Azure OpenAI Service.
    /// </summary>
    public class AzureOpenAIHelper : LLMClientBase
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _deployment;
        private readonly string _model;

        /// <param name="httpClient">Shared HttpClient instance.</param>
        /// <param name="apiKey">Azure OpenAI API key.</param>
        /// <param name="endpoint">Azure OpenAI endpoint, e.g. https://your-resource-name.openai.azure.com/</param>
        /// <param name="deployment">Deployment name for the model.</param>
        /// <param name="model">Model name (optional, defaults to 'gpt-4o-mini').</param>
        public AzureOpenAIHelper(HttpClient httpClient, string apiKey, string endpoint, string deployment, string model = "gpt-4o-mini")
            : base(httpClient)
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-azure-api-key-here")
                throw new ArgumentException("Azure OpenAI API key is not set. Please update 'settings.json' with your actual Azure API key.", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Contains("your-resource-name"))
                throw new ArgumentException("Azure OpenAI endpoint is not set. Please update 'settings.json' with your actual Azure endpoint.", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(deployment) || deployment == "your-deployment-name")
                throw new ArgumentException("Azure OpenAI deployment is not set. Please update 'settings.json' with your actual deployment name.", nameof(deployment));

            // Initialize fields
            _apiKey = apiKey;
            _endpoint = endpoint.TrimEnd('/');
            _deployment = deployment;
            _model = model;
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }

        public override async Task<string> GetResponseAsync(string userPrompt, string systemPrompt)
        {
            // Construct request body using base helper
            object body = CreateChatRequestBody(_model, systemPrompt, userPrompt);

            // Azure OpenAI endpoint format:
            // {endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-15-preview
            string url = $"{_endpoint}/openai/deployments/{_deployment}/chat/completions?api-version=2024-02-15-preview";

            string response = await SendPostRequestAsync(url, body);
            return ParseChatCompletionContent(response);
        }
    }
}