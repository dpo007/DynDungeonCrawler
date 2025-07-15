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
            GeneratorAppSettings settings = GeneratorAppSettings.Load();
            LLMSettings llmSettings = LLMSettings.Load();

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
            string[] validProviders = { "OpenAI", "Azure", "Ollama", "Dummy" };
            try
            {
                switch ((llmSettings.LLMProvider ?? "OpenAI").ToLowerInvariant())
                {
                    case "openai":
                        llmClient = new OpenAIHelper(httpClient, llmSettings.OpenAIApiKey);
                        break;

                    case "azure":
                        llmClient = new AzureOpenAIHelper(
                            httpClient,
                            llmSettings.AzureOpenAIApiKey,
                            llmSettings.AzureOpenAIEndpoint,
                            llmSettings.AzureOpenAIDeployment
                        );
                        break;

                    case "ollama":
                        llmClient = new OllamaAIHelper(httpClient, llmSettings.OllamaEndpoint);
                        break;

                    case "dummy":
                        llmClient = new DummyLLMClient();
                        break;

                    default:
                        throw new ArgumentException($"Unknown LLM provider: {llmSettings.LLMProvider}. Valid choices: {string.Join(", ", validProviders)}");
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
                Console.Write("Enter a theme for the dungeon (or press Enter for a random one): ");
                dungeonTheme = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(dungeonTheme))
                {
                    // Use DungeonThemeProvider for a random theme
                    dungeonTheme = await DungeonThemeProvider.GetRandomThemeAsync(llmClient, logger);

                    logger.Log($"[Info] Using random theme: {dungeonTheme}");
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