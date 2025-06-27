using System.Text.Json;

namespace DynDungeonCrawler.Engine.Configuration
{
    public class Settings
    {
        public string OpenAIApiKey { get; set; } = "your-api-key-here";

        // Azure OpenAI Service settings
        public string AzureOpenAIApiKey { get; set; } = "your-azure-api-key-here";

        public string AzureOpenAIEndpoint { get; set; } = "https://your-resource-name.openai.azure.com/";
        public string AzureOpenAIDeployment { get; set; } = "your-deployment-name";

        private static readonly string SettingsFilePath = "settings.json";

        public static Settings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                // Create a default settings file
                Settings defaultSettings = new Settings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("Settings file created. Please update 'settings.json' with your actual API key and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
    }
}