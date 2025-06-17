using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;
using DynDungeonCrawler.GeneratorApp.Utilities;

namespace DynDungeonCrawler.GeneratorApp
{
    internal class DungeonGeneratorApp
    {
        private static async Task Main(string[] args)
        {
            // Load settings
            var settings = Settings.Load();

            // Check if OpenAI API key is set
            if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                Console.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Initialize logging
            ILogger logger = new ConsoleLogger();

            // Create LLM client with shared HttpClient
            var httpClient = new HttpClient();
            ILLMClient llmClient = new OpenAIHelper(httpClient, settings.OpenAIApiKey);

            // Get dungeon theme from user
            Console.Write("Enter a theme for the dungeon (or press Enter for default): ");
            string? dungeonTheme = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dungeonTheme))
            {
                dungeonTheme = DungeonDefaults.DefaultDungeonDescription;
            }

            // Initialize the dungeon with the specified theme
            Console.WriteLine($"Initializing dungeon with theme: {dungeonTheme}");
            Dungeon dungeon = DungeonGenerator.GenerateDungeon(
                DungeonDefaults.MaxDungeonWidth,
                DungeonDefaults.MaxDungeonHeight,
                dungeonTheme,
                llmClient,
                logger);

            // Populate rooms with treasure chests and enemies
            Console.WriteLine("Populating rooms with treasure and enemies...");
            await DungeonGenerator.PopulateRoomContentsAsync(
                  dungeon.Rooms.ToList(),
                  dungeonTheme,
                  llmClient,
                  logger,
                  Random.Shared);

            // Print maps
            Console.WriteLine("Dungeon Map (Paths Only):");
            dungeon.PrintDungeonMap(showEntities: false); // Basic view: Entrance/Exit/Path only

            Console.WriteLine("\nDungeon Map (With Entities):");
            dungeon.PrintDungeonMap(showEntities: true); // Detailed view: showing treasure and enemies

            // Export dungeon (rooms + entities) to JSON
            string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DungeonExports");
            Directory.CreateDirectory(exportFolder);
            string exportPath = Path.Combine(exportFolder, "MyDungeon.json");
            dungeon.SaveToJson(exportPath);

            Console.WriteLine($"\nDungeon saved to {exportPath}");
            Console.ReadKey();
        }
    }
}