using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers;
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
            ui.WriteLine("*** [bold]Dynamic Dungeon Crawler![/] ***");

            Settings settings = Settings.Load();
            if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                ui.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                ui.WriteLine("Press any key to exit.");
                await ui.ReadKeyAsync();
                return (null, null, null, null, null);
            }

            ILogger logger = new FileLogger(settings.LogFilePath ?? @"C:\temp\ConDungeon.log");

            ILLMClient llmClient;
            switch ((settings.LLMProvider ?? "OpenAI").ToLowerInvariant())
            {
                case "openai":
                default:
                    llmClient = new OpenAIHelper(new HttpClient(), settings.OpenAIApiKey);
                    break;
                    // Add other providers here as needed (e.g., Azure, Ollama)
            }

            string filePath = settings.DungeonFilePath ?? "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);
            ui.WriteLine("[dim]Dungeon loaded successfully.[/]");
            ui.WriteLine($"Dungeon Theme: \"[white]{dungeon.Theme}[/]\"");
            ui.WriteLine($"Total Rooms: [yellow]{dungeon.Rooms.Count}[/]");
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
                    "Generating a name for your adventurer...",
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