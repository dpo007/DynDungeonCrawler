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
        /// The maximum number of treasure chests that can appear in a single room.
        /// </summary>
        public int MaxChestsPerRoom { get; set; } = 1;

        /// <summary>
        /// The maximum number of enemies that can appear in a single room.
        /// </summary>
        public int MaxEnemiesPerRoom { get; set; } = 2;

        /// <summary>
        /// Chance (0.0 to 1.0) of a treasure chest being added to a room.
        /// </summary>
        public double ChestSpawnChance { get; set; } = 0.10;

        /// <summary>
        /// Chance (0.0 to 1.0) of a chest being locked.
        /// </summary>
        public double ChestLockChance { get; set; } = 0.30;

        /// <summary>
        /// Chance (0.0 to 1.0) of the first enemy in a room with a chest.
        /// </summary>
        public double ChestRoomFirstEnemyChance { get; set; } = 0.40;

        /// <summary>
        /// Chance (0.0 to 1.0) of a second enemy in a room with a chest.
        /// </summary>
        public double ChestRoomSecondEnemyChance { get; set; } = 0.05;

        /// <summary>
        /// Chance (0.0 to 1.0) of the first enemy in an empty room.
        /// </summary>
        public double EmptyRoomFirstEnemyChance { get; set; } = 0.10;

        /// <summary>
        /// Chance (0.0 to 1.0) of a second enemy in an empty room.
        /// </summary>
        public double EmptyRoomSecondEnemyChance { get; set; } = 0.03;

        /// <summary>
        /// Minimum strength value for the strongest enemy guarding the lock pick.
        /// </summary>
        public int StrongestEnemyMinStrength { get; set; } = 20;

        public const string SettingsFilePath = "generatorapp.settings.json";

        /// <summary>
        /// Loads settings from the project-specific JSON file, creating a default if missing.
        /// Ensures all required fields are present and updates the file if needed.
        /// Throws if any required field is empty or whitespace.
        /// </summary>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <returns>The loaded <see cref="GeneratorAppSettings"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the settings file is created, updated, or contains invalid values.</exception>
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
                // Check for existing file paths
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

                // Check for entity spawn settings
                if (!doc.RootElement.TryGetProperty("MaxChestsPerRoom", out _))
                {
                    settings.MaxChestsPerRoom = 1;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("MaxEnemiesPerRoom", out _))
                {
                    settings.MaxEnemiesPerRoom = 2;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("ChestSpawnChance", out _))
                {
                    settings.ChestSpawnChance = 0.10;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("ChestLockChance", out _))
                {
                    settings.ChestLockChance = 0.30;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("ChestRoomFirstEnemyChance", out _))
                {
                    settings.ChestRoomFirstEnemyChance = 0.40;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("ChestRoomSecondEnemyChance", out _))
                {
                    settings.ChestRoomSecondEnemyChance = 0.05;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("EmptyRoomFirstEnemyChance", out _))
                {
                    settings.EmptyRoomFirstEnemyChance = 0.10;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("EmptyRoomSecondEnemyChance", out _))
                {
                    settings.EmptyRoomSecondEnemyChance = 0.03;
                    updated = true;
                }
                if (!doc.RootElement.TryGetProperty("StrongestEnemyMinStrength", out _))
                {
                    settings.StrongestEnemyMinStrength = 20;
                    updated = true;
                }
            }

            // Also ensure string fields are not empty
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

            // Validate numerical settings for common errors
            if (settings.MaxChestsPerRoom <= 0)
            {
                settings.MaxChestsPerRoom = 1;
                updated = true;
            }
            if (settings.MaxEnemiesPerRoom <= 0)
            {
                settings.MaxEnemiesPerRoom = 2;
                updated = true;
            }

            // Ensure probabilities are between 0 and 1
            if (settings.ChestSpawnChance < 0 || settings.ChestSpawnChance > 1)
            {
                settings.ChestSpawnChance = 0.10;
                updated = true;
            }
            if (settings.ChestLockChance < 0 || settings.ChestLockChance > 1)
            {
                settings.ChestLockChance = 0.30;
                updated = true;
            }
            if (settings.ChestRoomFirstEnemyChance < 0 || settings.ChestRoomFirstEnemyChance > 1)
            {
                settings.ChestRoomFirstEnemyChance = 0.40;
                updated = true;
            }
            if (settings.ChestRoomSecondEnemyChance < 0 || settings.ChestRoomSecondEnemyChance > 1)
            {
                settings.ChestRoomSecondEnemyChance = 0.05;
                updated = true;
            }
            if (settings.EmptyRoomFirstEnemyChance < 0 || settings.EmptyRoomFirstEnemyChance > 1)
            {
                settings.EmptyRoomFirstEnemyChance = 0.10;
                updated = true;
            }
            if (settings.EmptyRoomSecondEnemyChance < 0 || settings.EmptyRoomSecondEnemyChance > 1)
            {
                settings.EmptyRoomSecondEnemyChance = 0.03;
                updated = true;
            }
            if (settings.StrongestEnemyMinStrength <= 0)
            {
                settings.StrongestEnemyMinStrength = 20;
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