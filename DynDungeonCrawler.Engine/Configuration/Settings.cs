using System.Text.Json;

namespace DynDungeonCrawler.Engine.Configuration
{
    public class Settings
    {
        // General settings
        public string LogFilePath { get; set; } = "C://temp//ConDungeon.log";

        public string DungeonFilePath { get; set; } = "DungeonExports/MyDungeon.json";

        public string LLMProvider { get; set; } = "OpenAI"; // e.g., OpenAI, Azure, Ollama

        // OpenAI API settings
        public string OpenAIApiKey { get; set; } = "your-api-key-here";

        // Azure OpenAI Service settings
        public string AzureOpenAIApiKey { get; set; } = "your-azure-api-key-here";

        public string AzureOpenAIEndpoint { get; set; } = "https://your-resource-name.openai.azure.com/";
        public string AzureOpenAIDeployment { get; set; } = "your-deployment-name";

        // Other propterties
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