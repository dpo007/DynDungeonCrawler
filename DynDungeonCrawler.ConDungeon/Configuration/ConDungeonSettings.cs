using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.ConDungeon.Configuration
{
    /// <summary>
    /// Represents configuration settings for the ConDungeon project.
    /// </summary>
    public class ConDungeonSettings
    {
        /// <summary>
        /// Path to the log file.
        /// </summary>
        public string LogFilePath { get; set; } = @"C:\temp\ConDungeon.log";

        /// <summary>
        /// Path to the dungeon JSON file.
        /// </summary>
        public string DungeonFilePath { get; set; } = "Dungeons/MyDungeon.json";

        public const string SettingsFilePath = "condugeon.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// Ensures all required fields are present and updates the file if needed.
        /// Throws if any required field is empty or whitespace.
        /// </summary>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <returns>The loaded <see cref="ConDungeonSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created, updated, or contains invalid values.</exception>
        public static ConDungeonSettings Load(ILogger? logger = null)
        {
            if (!File.Exists(SettingsFilePath))
            {
                ConDungeonSettings defaultSettings = new ConDungeonSettings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
                throw new InvalidOperationException($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            ConDungeonSettings? settings = JsonSerializer.Deserialize<ConDungeonSettings>(json) ?? new ConDungeonSettings();

            bool updated = false;
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (!doc.RootElement.TryGetProperty("LogFilePath", out _))
                {
                    settings.LogFilePath = @"C:\temp\ConDungeon.log";
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("DungeonFilePath", out _))
                {
                    settings.DungeonFilePath = "Dungeons/MyDungeon.json";
                    updated = true;
                }
            }
            // Also ensure fields are not empty
            if (string.IsNullOrWhiteSpace(settings.LogFilePath))
            {
                settings.LogFilePath = @"C:\temp\ConDungeon.log";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(settings.DungeonFilePath))
            {
                settings.DungeonFilePath = "Dungeons/MyDungeon.json";
                updated = true;
            }

            if (updated)
            {
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                logger?.Log($"Settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
                throw new InvalidOperationException($"Settings file updated with missing defaults. Please review and edit '{SettingsFilePath}' as needed, then restart the application.");
            }

            // Validate required fields (no 'your-' check, just empty/whitespace)
            if (string.IsNullOrWhiteSpace(settings.LogFilePath))
            {
                throw new InvalidOperationException($"LogFilePath is missing or not set. Please update '{SettingsFilePath}' with a valid log file path.");
            }
            if (string.IsNullOrWhiteSpace(settings.DungeonFilePath))
            {
                throw new InvalidOperationException($"DungeonFilePath is missing or not set. Please update '{SettingsFilePath}' with a valid dungeon file path.");
            }

            return settings;
        }
    }
}