using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Configuration
{
    /// <summary>
    /// Centralized configuration for LLM providers and credentials.
    /// </summary>
    public class LLMSettings
    {
        /// <summary>
        /// LLM provider name (e.g., OpenAI, Azure, Ollama).
        /// </summary>
        public string LLMProvider { get; set; } = "OpenAI";

        /// <summary>
        /// OpenAI API key.
        /// </summary>
        public string OpenAIApiKey { get; set; } = "your-openai-api-key-here";

        /// <summary>
        /// Azure OpenAI API key.
        /// </summary>
        public string AzureOpenAIApiKey { get; set; } = "your-azure-api-key-here";

        /// <summary>
        /// Azure OpenAI endpoint URL.
        /// </summary>
        public string AzureOpenAIEndpoint { get; set; } = "https://your-resource-name.openai.azure.com/";

        /// <summary>
        /// Azure OpenAI deployment name.
        /// </summary>
        public string AzureOpenAIDeployment { get; set; } = "your-deployment-name";

        /// <summary>
        /// Ollama endpoint URL.
        /// </summary>
        public string OllamaEndpoint { get; set; } = "http://localhost:11434";

        public const string SettingsFilePath = "llm.settings.json";

        /// <summary>
        /// Loads LLM settings from the central JSON file, creating defaults if missing.
        /// Ensures all required fields are present and updates the file if needed.
        /// Throws if any hint field is empty, whitespace, or still contains 'your-'.
        /// </summary>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <returns>The loaded <see cref="LLMSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created, updated, or contains invalid hint values.</exception>
        public static LLMSettings Load(ILogger? logger = null)
        {
            if (!File.Exists(SettingsFilePath))
            {
                LLMSettings defaultSettings = new LLMSettings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"LLM settings file created. Please update '{SettingsFilePath}' and restart the application.");
                throw new InvalidOperationException($"LLM settings file created. Please update '{SettingsFilePath}' and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            LLMSettings? settings = JsonSerializer.Deserialize<LLMSettings>(json) ?? new LLMSettings();

            bool updated = false;
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("LLMProvider", out _))
                {
                    settings.LLMProvider = "OpenAI";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("OpenAIApiKey", out _))
                {
                    settings.OpenAIApiKey = "your-openai-api-key-here";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("AzureOpenAIApiKey", out _))
                {
                    settings.AzureOpenAIApiKey = "your-azure-api-key-here";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("AzureOpenAIEndpoint", out _))
                {
                    settings.AzureOpenAIEndpoint = "https://your-resource-name.openai.azure.com/";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("AzureOpenAIDeployment", out _))
                {
                    settings.AzureOpenAIDeployment = "your-deployment-name";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("OllamaEndpoint", out _))
                {
                    settings.OllamaEndpoint = "http://localhost:11434";
                    updated = true;
                }
            }
            // Also ensure fields are not empty
            if (string.IsNullOrWhiteSpace(settings.LLMProvider))
            {
                settings.LLMProvider = "OpenAI";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.OllamaEndpoint))
            {
                settings.OllamaEndpoint = "http://localhost:11434";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey))
            {
                settings.OpenAIApiKey = "your-openai-api-key-here";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIApiKey))
            {
                settings.AzureOpenAIApiKey = "your-azure-api-key-here";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIEndpoint))
            {
                settings.AzureOpenAIEndpoint = "https://your-resource-name.openai.azure.com/";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIDeployment))
            {
                settings.AzureOpenAIDeployment = "your-deployment-name";
                updated = true;
            }

            if (updated)
            {
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"LLM settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
                throw new InvalidOperationException($"LLM settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
            }

            // Validate only LLM/API credential fields for 'your-' and empty/whitespace
            if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey) || settings.OpenAIApiKey.Contains("your-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"OpenAI API key is missing or not set. Please update '{SettingsFilePath}' with your actual OpenAI API key.");
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIApiKey) || settings.AzureOpenAIApiKey.Contains("your-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Azure OpenAI API key is missing or not set. Please update '{SettingsFilePath}' with your actual Azure OpenAI API key.");
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIEndpoint) || settings.AzureOpenAIEndpoint.Contains("your-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Azure OpenAI endpoint is missing or not set. Please update '{SettingsFilePath}' with your actual Azure OpenAI endpoint.");
            }
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIDeployment) || settings.AzureOpenAIDeployment.Contains("your-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Azure OpenAI deployment name is missing or not set. Please update '{SettingsFilePath}' with your actual deployment name.");
            }

            // For non-LLM settings, only check for empty/whitespace
            if (string.IsNullOrWhiteSpace(settings.LLMProvider))
            {
                throw new InvalidOperationException($"LLMProvider is missing or not set. Please update '{SettingsFilePath}' with a valid LLM provider name.");
            }
            if (string.IsNullOrWhiteSpace(settings.OllamaEndpoint))
            {
                throw new InvalidOperationException($"OllamaEndpoint is missing or not set. Please update '{SettingsFilePath}' with a valid Ollama endpoint URL.");
            }

            return settings;
        }
    }
}