using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.GeneratorApp
{
    /// <summary>
    /// Represents configuration settings for the GeneratorApp project.
    /// </summary>
    public class GeneratorAppSettings
    {
        /// <summary>
        /// Path to the log file.
        /// </summary>
        public string LogFilePath { get; set; } = @"C:\temp\GeneratorApp.log";

        /// <summary>
        /// Path to the dungeon JSON file.
        /// </summary>
        public string DungeonFilePath { get; set; } = "DungeonExports/MyDungeon.json";

        /// <summary>
        /// LLM provider name (e.g., OpenAI, Azure, Ollama).
        /// </summary>
        public string LLMProvider { get; set; } = "OpenAI";

        /// <summary>
        /// OpenAI API key.
        /// </summary>
        public string OpenAIApiKey { get; set; } = "your-api-key-here";

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

        public const string SettingsFilePath = "generatorapp.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// Ensures all required fields (including LLMProvider and OllamaEndpoint) are present.
        /// Logs a message if the settings file is updated.
        /// </summary>
        /// <returns>The loaded <see cref="GeneratorAppSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created or updated and needs user editing.</exception>
        public static GeneratorAppSettings Load(ILogger? logger = null)
        {
            if (!File.Exists(SettingsFilePath))
            {
                GeneratorAppSettings defaultSettings = new GeneratorAppSettings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
                throw new InvalidOperationException($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            GeneratorAppSettings? settings = JsonSerializer.Deserialize<GeneratorAppSettings>(json) ?? new GeneratorAppSettings();

            bool updated = false;
            // Check for OllamaEndpoint in the raw JSON to catch missing property
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("OllamaEndpoint", out _))
                {
                    settings.OllamaEndpoint = "http://localhost:11434";
                    updated = true;
                }
            }
            // Also ensure OllamaEndpoint is not empty
            if (string.IsNullOrWhiteSpace(settings.OllamaEndpoint))
            {
                settings.OllamaEndpoint = "http://localhost:11434";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.LLMProvider))
            {
                settings.LLMProvider = "OpenAI";
                updated = true;
            }
            // ...add checks for other required fields here...

            if (updated)
            {
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"Settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
                throw new InvalidOperationException($"Settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
            }

            return settings;
        }
    }
}