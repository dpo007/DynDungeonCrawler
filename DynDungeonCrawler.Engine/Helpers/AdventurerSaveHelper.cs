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
            // Use GUID and Name in the filename
            string fileName = $"Adventurer_{adventurer.Id}_{adventurer.Name}.json";
            string filePath = Path.Combine(SaveDirectory, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(adventurer, options);

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads an Adventurer object from a JSON file using their GUID.
        /// </summary>
        /// <param name="adventurerId">The GUID of the adventurer to load.</param>
        /// <returns>The loaded Adventurer object, or null if not found.</returns>
        public static Adventurer? LoadAdventurer(Guid adventurerId)
        {
            // Search for a file matching the GUID
            string[] files = Directory.GetFiles(SaveDirectory, $"Adventurer_{adventurerId}_*.json");

            if (files.Length == 0)
            {
                Console.WriteLine($"Save file for adventurer with GUID '{adventurerId}' not found.");
                return null;
            }

            // Load the first matching file (there should only be one)
            string filePath = files[0];
            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<Adventurer>(json);
        }
    }
}
