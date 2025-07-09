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
            Settings settings = Settings.Load();

            // Initialize logging
            ILogger logger;
            if (!string.IsNullOrWhiteSpace(settings.LogFilePath))
            {
                logger = new FileLogger(settings.LogFilePath);
            }
            else
            {
                logger = new ConsoleLogger();
            }

            // Create LLM client with shared HttpClient
            HttpClient httpClient = new HttpClient();
            ILLMClient llmClient;
            try
            {
                switch ((settings.LLMProvider ?? "OpenAI").ToLowerInvariant())
                {
                    case "openai":
                    default:
                        llmClient = new OpenAIHelper(httpClient, settings.OpenAIApiKey);
                        break;
                        // Add other providers here as needed (e.g., Azure, Ollama)
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Get dungeon theme from user
            string? dungeonTheme = null;
            while (true)
            {
                Console.Write("Enter a theme for the dungeon (or press Enter for default): ");
                dungeonTheme = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(dungeonTheme))
                {
                    dungeonTheme = DungeonDefaults.DefaultDungeonDescription;
                    break;
                }
                if (dungeonTheme.Length > 255)
                {
                    Console.WriteLine("Theme must be 255 characters or fewer. Please try again.\n");
                    continue;
                }
                break;
            }

            // Initialize the dungeon with the specified theme
            Console.WriteLine($"Initializing dungeon with theme: {dungeonTheme}");
            Dungeon dungeon = await DungeonGeneration.GenerateDungeon(
                DungeonDefaults.MaxDungeonWidth,
                DungeonDefaults.MaxDungeonHeight,
                dungeonTheme,
                llmClient,
                logger);

            // Populate rooms with treasure chests and enemies
            Console.WriteLine("Populating rooms with treasure and enemies...");
            await DungeonGeneration.PopulateRoomContentsAsync(
                  dungeon.Rooms.ToList(),
                  dungeonTheme,
                  llmClient,
                  logger,
                  Random.Shared);

            // Print maps
            Console.WriteLine("\nDungeon Map (Paths Only):");
            dungeon.PrintDungeonMap(showEntities: false); // Basic view: Entrance/Exit/Path only

            Console.WriteLine("\nDungeon Map (With Entities):");
            dungeon.PrintDungeonMap(showEntities: true); // Detailed view: showing treasure and enemies

            // Export dungeon (rooms + entities) to JSON
            string exportPath = settings.DungeonFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DungeonExports", "MyDungeon.json");
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
            dungeon.SaveToJson(exportPath);

            Console.WriteLine($"\nDungeon saved to {exportPath}");
            Console.ReadKey();
        }
    }
}