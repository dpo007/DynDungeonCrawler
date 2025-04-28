using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    public static class AdventurerSaveHelper
    {
        private static readonly string SaveDirectory = "Saves";

        static AdventurerSaveHelper()
        {
            // Ensure the Saves directory exists
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        /// <summary>
        /// Saves the given Adventurer object to a JSON file.
        /// </summary>
        /// <param name="adventurer">The Adventurer to save.</param>
        public static void SaveAdventurer(Adventurer adventurer)
        {
            string fileName = $"Adventurer_{adventurer.Name}.json";
            string filePath = Path.Combine(SaveDirectory, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(adventurer, options);

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads an Adventurer object from a JSON file.
        /// </summary>
        /// <param name="playerName">The name of the player to load.</param>
        /// <returns>The loaded Adventurer object.</returns>
        public static Adventurer? LoadAdventurer(string playerName)
        {
            string fileName = $"Adventurer_{playerName}.json";
            string filePath = Path.Combine(SaveDirectory, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Save file for player '{playerName}' not found.");
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Adventurer>(json);
        }
    }
}
