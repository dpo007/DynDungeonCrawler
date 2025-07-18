﻿namespace DynDungeonCrawler.Engine.Helpers.LLM
{
    /// <summary>
    /// Implementation of LLMClientBase using OpenAI's API.
    /// </summary>
    public class OpenAIHelper : LLMClientBase
    {
        private readonly string _apiKey;

        public OpenAIHelper(HttpClient httpClient, string apiKey) : base(httpClient)
        {
            // Validate API key
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-api-key-here")
            {
                throw new ArgumentException("OpenAI API key is not set. Please update 'settings.json' with your actual API key.", nameof(apiKey));
            }

            // Initialize fields
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public override async Task<string> GetResponseAsync(string userPrompt, string systemPrompt)
        {
            // Construct request body using base helper
            object body = CreateChatRequestBody("gpt-4o-mini", systemPrompt, userPrompt);

            // Send request and parse content
            string response = await SendPostRequestAsync("https://api.openai.com/v1/chat/completions", body);
            return ParseChatCompletionContent(response);
        }
    }
}