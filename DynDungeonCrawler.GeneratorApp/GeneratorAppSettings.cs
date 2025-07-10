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

        public const string SettingsFilePath = "generatorapp.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// </summary>
        /// <returns>The loaded <see cref="GeneratorAppSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created and needs user editing.</exception>
        public static GeneratorAppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                GeneratorAppSettings defaultSettings = new GeneratorAppSettings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<GeneratorAppSettings>(json) ?? new GeneratorAppSettings();
        }
    }
}