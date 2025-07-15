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

        public const string SettingsFilePath = "generatorapp.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// Ensures all required fields are present and updates the file if needed.
        /// </summary>
        /// <param name="logger">Logger for progress and error messages.</param>
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
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("LogFilePath", out _))
                {
                    settings.LogFilePath = @"C:\temp\GeneratorApp.log";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("DungeonFilePath", out _))
                {
                    settings.DungeonFilePath = "DungeonExports/MyDungeon.json";
                    updated = true;
                }
            }
            // Also ensure fields are not empty
            if (string.IsNullOrWhiteSpace(settings.LogFilePath))
            {
                settings.LogFilePath = @"C:\temp\GeneratorApp.log";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.DungeonFilePath))
            {
                settings.DungeonFilePath = "DungeonExports/MyDungeon.json";
                updated = true;
            }

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