using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Helpers.ContentGeneration;
using DynDungeonCrawler.Engine.Helpers.LLM;
using DynDungeonCrawler.Engine.Helpers.Logging;
using DynDungeonCrawler.Engine.Helpers.UI;
using DynDungeonCrawler.Engine.Interfaces;
using DynDungeonCrawler.GeneratorApp.Utilities;

namespace DynDungeonCrawler.GeneratorApp
{
    internal class DungeonGeneratorApp
    {
        private static IUserInterface _ui = null!;

        private static async Task Main(string[] args)
        {
            // Initialize UI
            _ui = new SpectreConsoleUserInterface();
            _ui.Clear();
            _ui.WriteRule("[bold cyan]Dynamic Dungeon Generator[/]");
            _ui.WriteLine();

            // Load settings
            GeneratorAppSettings settings = _ui.ShowSpinnerAsync(
                "Loading configuration...",
                () => Task.FromResult(GeneratorAppSettings.Load())
            ).Result;

            LLMSettings llmSettings;
            try
            {
                llmSettings = _ui.ShowSpinnerAsync(
                    "Loading LLM settings...",
                    () => Task.FromResult(LLMSettings.Load())
                ).Result;
            }
            catch (InvalidOperationException ex)
            {
                _ui.WriteLine($"[bold red]Error:[/] {ex.Message}");
                _ui.WriteLine("Press any key to exit.");
                await _ui.ReadKeyAsync();
                return;
            }

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
                llmClient = _ui.ShowSpinnerAsync(
                    $"Initializing {llmSettings.LLMProvider ?? "AI"} client...",
                    () => Task.FromResult(CreateLLMClient(httpClient, llmSettings, validProviders))
                ).Result;
            }
            catch (ArgumentException ex)
            {
                _ui.WriteLine($"[bold red]Error:[/] {ex.Message}");
                _ui.WriteLine("Press any key to exit.");
                await _ui.ReadKeyAsync();
                return;
            }

            // Get dungeon theme from user
            _ui.WriteRule("[bold green]Theme Selection[/]");

            string? dungeonTheme = null;
            while (true)
            {
                _ui.Write("Enter a theme for the dungeon ([grey]or press Enter for a random one[/]): ");
                dungeonTheme = await _ui.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(dungeonTheme))
                {
                    // Use spinner for random theme generation
                    dungeonTheme = await _ui.ShowSpinnerAsync(
                        "Generating random dungeon theme...",
                        () => DungeonThemeProvider.GetRandomThemeAsync(llmClient, logger)
                    );

                    _ui.WriteLine($"[bold cyan]Using random theme:[/] {dungeonTheme}");
                    logger.Log($"[Info] Using random theme: {dungeonTheme}");
                    break;
                }

                if (dungeonTheme.Length > 255)
                {
                    _ui.WriteLine("[bold red]Theme must be 255 characters or fewer. Please try again.[/]");
                    continue;
                }
                break;
            }
            _ui.WriteLine();

            // Initialize the dungeon with the specified theme
            _ui.WriteRule("[bold cyan]Dungeon Generation[/]");
            _ui.WriteLine($"Initializing dungeon with theme: [bold yellow]{dungeonTheme}[/]");

            Dungeon dungeon = await _ui.ShowSpinnerAsync<Dungeon>(
                "Generating dungeon layout...",
                () => DungeonGeneration.GenerateDungeon(
                    DungeonDefaults.MaxDungeonWidth,
                    DungeonDefaults.MaxDungeonHeight,
                    dungeonTheme,
                    llmClient,
                    logger,
                    settings,
                    _ui)
            );

            // Populate rooms with treasure chests and enemies
            await _ui.ShowSpinnerAsync<object>(
                "Populating rooms with treasure and enemies...",
                async () =>
                {
                    await DungeonGeneration.PopulateRoomContentsAsync(
                        dungeon.Rooms.ToList(),
                        dungeonTheme,
                        llmClient,
                        logger,
                        Random.Shared,
                        settings,
                        _ui);
                    return null!;
                }
            );
            _ui.WriteLine();

            // Show dungeon summary
            _ui.WriteLine($"Total Rooms: [bold yellow]{dungeon.Rooms.Count}[/]");
            int totalEnemies = dungeon.Rooms.Sum(r => r.Contents.Count(e => e.Type == EntityType.Enemy));
            _ui.WriteLine($"Total Enemies: [bold yellow]{totalEnemies}[/]");
            int totalTreasure = dungeon.Rooms.Sum(r => r.Contents.Count(e => e.Type == EntityType.TreasureChest));
            _ui.WriteLine($"Total Treasure Chests: [bold yellow]{totalTreasure}[/]");
            _ui.WriteLine();

            // Create a dungeon renderer that uses our UI
            DungeonRenderer renderer = new DungeonRenderer(_ui);

            // Print maps using the renderer
            _ui.WriteRule("[bold magenta]Dungeon Map (Paths Only)[/]");
            renderer.RenderDungeon(dungeon, showEntities: false);
            _ui.WriteLine();

            _ui.WriteRule("[bold magenta]Dungeon Map (With Entities)[/]");
            renderer.RenderDungeon(dungeon, showEntities: true);

            // Export dungeon to JSON with spinner
            string exportPath = settings.DungeonFilePath ??
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DungeonExports", "MyDungeon.json");

            await _ui.ShowSpinnerAsync<object>(
                "Saving dungeon to file...",
                () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
                    dungeon.SaveToJson(exportPath);
                    return Task.FromResult<object>(null!);
                }
            );

            // Show completion message
            _ui.WriteLine();
            _ui.WriteSpecialMessage($"Dungeon saved to {exportPath}", center: true, writeLine: true);
            _ui.WriteLine("[grey]Press any key to exit...[/]");
            await _ui.ReadKeyAsync();
        }

        /// <summary>
        /// Creates an LLM client based on the specified settings.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API calls.</param>
        /// <param name="llmSettings">The LLM settings.</param>
        /// <param name="validProviders">Array of valid provider names.</param>
        /// <returns>An initialized LLM client.</returns>
        /// <exception cref="ArgumentException">Thrown when an invalid provider is specified.</exception>
        private static ILLMClient CreateLLMClient(HttpClient httpClient, LLMSettings llmSettings, string[] validProviders)
        {
            switch ((llmSettings.LLMProvider ?? "OpenAI").ToLowerInvariant())
            {
                case "openai":
                    return new OpenAIHelper(httpClient, llmSettings.OpenAIApiKey);

                case "azure":
                    return new AzureOpenAIHelper(
                        httpClient,
                        llmSettings.AzureOpenAIApiKey,
                        llmSettings.AzureOpenAIEndpoint,
                        llmSettings.AzureOpenAIDeployment
                    );

                case "ollama":
                    return new OllamaAIHelper(
                        httpClient,
                        llmSettings.OllamaEndpoint,
                        llmSettings.OllamaModel
                    );

                case "dummy":
                    return new DummyLLMClient();

                default:
                    throw new ArgumentException(
                        $"Unknown LLM provider: {llmSettings.LLMProvider}. Valid choices: {string.Join(", ", validProviders)}"
                    );
            }
        }
    }
}