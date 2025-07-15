using DynDungeonCrawler.Engine.Interfaces;
using System.Reflection;
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
        /// Validates only the settings relevant to the selected LLM provider using reflection.
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

            // Always validate the LLMProvider field itself
            if (string.IsNullOrWhiteSpace(settings.LLMProvider))
            {
                throw new InvalidOperationException($"LLMProvider is missing or not set. Please update '{SettingsFilePath}' with a valid LLM provider name.");
            }

            // Provider-specific validation using reflection
            string provider = settings.LLMProvider.Trim();
            string prefix = provider.ToLowerInvariant() switch
            {
                "openai" => "OpenAI",
                "azure" => "Azure",
                "ollama" => "Ollama",
                _ => throw new InvalidOperationException($"Unknown provider: {provider}")
            };

            PropertyInfo[] properties = typeof(LLMSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == nameof(LLMProvider))
                {
                    continue;
                }

                if (property.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    object? value = property.GetValue(settings);
                    string? stringValue = value as string;
                    // For Ollama, only check for empty/whitespace (no "your-" hint)
                    if (prefix == "Ollama")
                    {
                        if (string.IsNullOrWhiteSpace(stringValue))
                        {
                            throw new InvalidOperationException($"{property.Name} is missing or not set. Please update '{SettingsFilePath}' with a valid value for {property.Name}.");
                        }
                    }
                    else
                    {
                        // For OpenAI and Azure, check for both empty/whitespace and "your-" hints
                        if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Contains("your-", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"{property.Name} is missing or not set. Please update '{SettingsFilePath}' with a valid value for {property.Name}.");
                        }
                    }
                }
            }

            return settings;
        }
    }
}