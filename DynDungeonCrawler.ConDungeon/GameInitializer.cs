using DynDungeonCrawler.ConDungeon.Configuration;
using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers.ContentGeneration;
using DynDungeonCrawler.Engine.Helpers.LLM;
using DynDungeonCrawler.Engine.Helpers.Logging;
using DynDungeonCrawler.Engine.Helpers.UI;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal static class GameInitializer
    {
        /// <summary>
        /// Asynchronously initializes the game by setting up the user interface, logger, LLM client, dungeon, and player.
        /// Prompts the user for required information and loads the dungeon from a JSON file.
        /// If initialization fails (e.g., missing API key), returns a tuple of nulls to signal failure.
        /// </summary>
        /// <returns>
        /// A task that resolves to a tuple containing the initialized user interface, logger, LLM client, dungeon, and player.
        /// Any element may be null if initialization fails.
        /// </returns>
        public static async Task<(IUserInterface? ui, ILogger? logger, ILLMClient? llmClient, Dungeon? dungeon, Adventurer? player)> InitializeGameAsync()
        {
            IUserInterface ui = new SpectreConsoleUserInterface();
            ui.WriteRule("*** [bold]Dynamic Dungeon Crawler![/] ***");

            ConDungeonSettings settings = ConDungeonSettings.Load();
            LLMSettings llmSettings = LLMSettings.Load();

            // Check for missing API key or provider
            if (string.IsNullOrWhiteSpace(llmSettings.LLMProvider))
            {
                ui.WriteLine($"LLM provider is not set. Please update '{LLMSettings.SettingsFilePath}' with your provider.");
                ui.WriteLine("Press any key to exit.");
                await ui.ReadKeyAsync();
                return (null, null, null, null, null);
            }
            if (llmSettings.LLMProvider.ToLowerInvariant() == "openai" && string.IsNullOrWhiteSpace(llmSettings.OpenAIApiKey))
            {
                ui.WriteLine($"OpenAI API key is not set. Please update '{LLMSettings.SettingsFilePath}' with your actual API key.");
                ui.WriteLine("Press any key to exit.");
                await ui.ReadKeyAsync();
                return (null, null, null, null, null);
            }

            ILogger logger = new FileLogger(settings.LogFilePath ?? @"C:\temp\ConDungeon.log");

            ILLMClient llmClient;
            string[] validProviders = { "OpenAI", "Azure", "Ollama", "Dummy" };
            switch (llmSettings.LLMProvider.ToLowerInvariant())
            {
                case "openai":
                    llmClient = new OpenAIHelper(new HttpClient(), llmSettings.OpenAIApiKey);
                    break;

                case "azure":
                    llmClient = new AzureOpenAIHelper(
                        new HttpClient(),
                        llmSettings.AzureOpenAIApiKey,
                        llmSettings.AzureOpenAIEndpoint,
                        llmSettings.AzureOpenAIDeployment
                    );
                    break;

                case "ollama":
                    llmClient = new OllamaAIHelper(
                        new HttpClient(),
                        llmSettings.OllamaEndpoint,
                        llmSettings.OllamaModel
                    );
                    break;

                case "dummy":
                    llmClient = new DummyLLMClient();
                    break;

                default:
                    ui.WriteLine($"Unknown LLM provider: {llmSettings.LLMProvider}. Valid choices: {string.Join(", ", validProviders)}");
                    ui.WriteLine("Press any key to exit.");
                    await ui.ReadKeyAsync();
                    return (null, null, null, null, null);
            }

            string filePath = settings.DungeonFilePath ?? "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);
            ui.WriteLine("[dim]Dungeon loaded successfully.[/]");
            ui.WriteLine();
            ui.WriteLine($"Dungeon Theme: \"[white]{dungeon.Theme}[/]\"");
            ui.WriteLine($"Total Rooms: [yellow]{dungeon.Rooms.Count}[/]");
            ui.WriteLine();

            // Generate chest opening stories
            ui.WriteLine("Generating immersive content for your adventure...");
            await ui.ShowSpinnerAsync(
                "[italic]Creating chest opening narratives...[/]",
                async () =>
                {
                    int storyCount = 5;
                    List<string> stories = await ChestOpeningStoryProvider.GenerateChestOpeningStoriesAsync(
                        dungeon.Theme,
                        llmClient,
                        logger,
                        storyCount);

                    dungeon.ChestOpeningStories = stories;
                    return true;
                }
            );
            ui.WriteLine();

            // Player name and gender
            ui.Write("Enter your adventurer's name [gray](or press Enter to generate one)[/]: ");
            string playerName = (await ui.ReadLineAsync()).Trim();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                AdventurerGender gender = AdventurerGender.Unspecified;
                ui.Write("Enter your adventurer's gender ([[[deepskyblue1]M[/]]]ale/[[[hotpink]F[/]]]emale, [gray]or press Enter for unspecified[/]): ");
                while (true)
                {
                    string keyStr = await ui.ReadKeyAsync(intercept: true);
                    if (string.IsNullOrEmpty(keyStr) || keyStr == "\r" || keyStr == "\n")
                    {
                        ui.WriteLine();
                        break;
                    }
                    else if (keyStr.Equals("M", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ui.WriteLine("[deepskyblue1]M[/]");
                        gender = AdventurerGender.Male;
                        break;
                    }
                    else if (keyStr.Equals("F", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ui.WriteLine("[hotpink]F[/]");
                        gender = AdventurerGender.Female;
                        break;
                    }
                }

                playerName = await ui.ShowSpinnerAsync(
                    "[italic]Generating a name for your adventurer...[/]",
                    () => Adventurer.GenerateNameAsync(llmClient, dungeon.Theme, gender)
                );
            }

            // Log the player's name
            logger.Log($"Adventurer's name: {playerName}");

            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(playerName, entrance);

            ui.WriteLine();
            ui.WriteLine($"Welcome to the dungeon [bold underline]{playerName}[/]!", true);
            ui.WriteLine();
            ui.WriteLine("[dim italic]Press any key to start your adventure...[/]", true);
            ui.ReadKeyAsync(intercept: true, hideCursor: true).Wait(); // Wait for user to acknowledge
            ui.Clear();

            return (ui, logger, llmClient, dungeon, player);
        }
    }
}