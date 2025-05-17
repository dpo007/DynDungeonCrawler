using System.Text.Json;

namespace DynDungeonCrawler.Engine.Configuration
{
    public class Settings
    {
        public string OpenAIApiKey { get; set; } = "your-api-key-here";

        private static readonly string SettingsFilePath = "settings.json";

        public static Settings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                // Create a default settings file
                Settings defaultSettings = new Settings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("Settings file created. Please update 'settings.json' with your actual API key.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
    }
}