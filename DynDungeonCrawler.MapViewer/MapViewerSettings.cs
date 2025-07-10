using System.IO;
using System.Text.Json;

namespace DynDungeonCrawler.MapViewer
{
    /// <summary>
    /// Represents configuration settings for the MapViewer project.
    /// </summary>
    public class MapViewerSettings
    {
        /// <summary>
        /// Path to the log file.
        /// </summary>
        public string LogFilePath { get; set; } = @"C:\temp\MapViewer.log";

        /// <summary>
        /// Path to the dungeon JSON file.
        /// </summary>
        public string DungeonFilePath { get; set; } = "Dungeons/MyDungeon.json";

        public const string SettingsFilePath = "mapviewer.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// </summary>
        /// <returns>The loaded <see cref="MapViewerSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created and needs user editing.</exception>
        public static MapViewerSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                MapViewerSettings defaultSettings = new MapViewerSettings();
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException($"Settings file created. Please update '{SettingsFilePath}' and restart the application.");
            }

            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<MapViewerSettings>(json) ?? new MapViewerSettings();
        }
    }
}