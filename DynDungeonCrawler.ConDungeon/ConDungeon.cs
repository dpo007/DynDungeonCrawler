using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal class ConDungeon
    {
        /// <summary>
        /// Entry point for the console dungeon crawler. Initializes the game and starts the main game loop asynchronously.
        /// </summary>
        private static async Task Main(string[] args)
        {
            // Capture the nullable tuple
            (IUserInterface? ui, ILogger? logger, ILLMClient? llmClient, Dungeon? dungeon, Adventurer? player) init = await InitializeGameAsync();

            // Bail out if any element is null
            if (init.ui == null ||
                init.logger == null ||
                init.llmClient == null ||
                init.dungeon == null ||
                init.player == null)
            {
                return;
            }

            // Safe to deconstruct into non-nullable locals
            (IUserInterface ui,
             ILogger logger,
             ILLMClient llmClient,
             Dungeon dungeon,
             Adventurer player) = init;

            // Start the game loop
            await GameLoopAsync(ui, logger, llmClient, dungeon, player);
        }

        /// <summary>
        /// Asynchronously initializes the game by setting up the user interface, logger, LLM client, dungeon, and player.
        /// Prompts the user for required information and loads the dungeon from a JSON file.
        /// If initialization fails (e.g., missing API key), returns a tuple of nulls to signal failure.
        /// </summary>
        /// <returns>
        /// A task that resolves to a tuple containing the initialized user interface, logger, LLM client, dungeon, and player.
        /// Any element may be null if initialization fails.
        /// </returns>
        private static async Task<(IUserInterface? ui, ILogger? logger, ILLMClient? llmClient, Dungeon? dungeon, Adventurer? player)> InitializeGameAsync()
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
                playerName = await Adventurer.GenerateNameAsync(llmClient, dungeon.Theme, gender);
            }

            ui.WriteLine();
            ui.WriteLine($"Welcome to the dungeon [bold underline]{playerName}[/]!");
            ui.WriteLine();

            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(playerName, entrance);

            return (ui, logger, llmClient, dungeon, player);
        }

        /// <summary>
        /// Runs the main game loop, handling player input and game state updates
        /// until the player either exits, dies, or reaches the exit room.
        /// </summary>
        /// <param name="ui">The user interface for input/output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        /// <param name="llmClient">AI client for generating descriptions as needed.</param>
        /// <param name="dungeon">The Dungeon instance containing rooms and state.</param>
        /// <param name="player">The Adventurer representing the player.</param>
        private static async Task GameLoopAsync(
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player)
        {
            while (true)
            {
                // Game-ending conditions
                if (player.Health <= 0)
                {
                    ui.WriteLine("You have perished in the dungeon. Game over!");
                    break;
                }
                if (player.CurrentRoom?.Type == RoomType.Exit)
                {
                    DrawRoom(ui, player);

                    ui.ShowSpecialMessage("Congratulations! You have found the exit and escaped the dungeon!", center: true, writeLine: true);
                    break;
                }
                if (player.CurrentRoom == null)
                {
                    ui.WriteLine("You are lost in the void. Game over!");
                    break;
                }

                DrawRoom(ui, player);

                List<string> directions = GetAvailableDirections(player.CurrentRoom);
                char cmdChar = await HandleInputAsync(ui, directions);

                if (!await ProcessCommandAsync(cmdChar, ui, logger, llmClient, dungeon, player, directions))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Displays the current room's description and contents to the user interface.
        /// If the player's current room is null, an error message is shown instead.
        /// </summary>
        /// <param name="ui">The user interface used for output.</param>
        /// <param name="player">The adventurer whose current room will be displayed.</param>
        private static void DrawRoom(IUserInterface ui, Adventurer player)
        {
            if (player.CurrentRoom == null)
            {
                ui.WriteLine("[bold red]Error:[/] Current room is null.");
                return;
            }

            ui.WriteRule();
            ui.WriteLine(player.CurrentRoom.Description ?? "[dim]This room is an abyss.[/]");

            if (player.CurrentRoom.Contents.Count > 0)
            {
                ui.WriteLine();
                ui.WriteLine("You notice the following things in the room:");
                foreach (Entity entity in player.CurrentRoom.Contents)
                {
                    switch (entity)
                    {
                        case TreasureChest chest:
                            string chestState = chest.IsOpened ? "opened" : (chest.IsLocked ? "locked" : "unlocked");
                            ui.WriteLine($"[dim]-[/] [bold]{chest.Name}[/] ({chestState})");
                            break;

                        default:
                            string displayDesc = !string.IsNullOrWhiteSpace(entity.ShortDescription)
                                ? entity.ShortDescription
                                : entity.Description;
                            if (!string.IsNullOrWhiteSpace(displayDesc))
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]: {displayDesc}");
                            }
                            else
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]");
                            }

                            break;
                    }
                }
            }

            ui.WriteRule();
            ui.WriteLine();
        }

        /// <summary>
        /// Returns a list of available movement directions from the specified room,
        /// based on which exits are connected. Each direction is represented as a
        /// single uppercase letter: "N" (north), "E" (east), "S" (south), or "W" (west).
        /// </summary>
        /// <param name="room">The room to check for available exits.</param>
        /// <returns>A list of direction strings indicating which exits are available from the room.</returns>
        private static List<string> GetAvailableDirections(Room room)
        {
            List<string> directions = new();
            if (room.ConnectedNorth)
            {
                directions.Add("N");
            }

            if (room.ConnectedEast)
            {
                directions.Add("E");
            }

            if (room.ConnectedSouth)
            {
                directions.Add("S");
            }

            if (room.ConnectedWest)
            {
                directions.Add("W");
            }

            return directions;
        }

        /// <summary>
        /// Asynchronously prompts the user for a command and reads a key input, validating it against the available movement directions
        /// and other valid commands (look, inventory, exit). Displays the chosen command in the UI.
        /// </summary>
        /// <param name="ui">The user interface used for input and output.</param>
        /// <param name="directions">A list of valid movement directions (e.g., "N", "E", "S", "W").</param>
        /// <returns>A task that resolves to the character representing the user's chosen command, in lowercase.</returns>
        private static async Task<char> HandleInputAsync(IUserInterface ui, List<string> directions)
        {
            string directionsPrompt = directions.Count > 0 ? string.Join("[dim]/[/]", directions) : "";
            ui.Write($"Enter command (move [[[bold]{directionsPrompt}[/]]], [[[bold]L[/]]]ook, [[[bold]I[/]]]nventory, e[[[bold]X[/]]]it): ");
            char cmdChar;
            while (true)
            {
                string cmdKeyStr = await ui.ReadKeyAsync(intercept: true);
                if (string.IsNullOrEmpty(cmdKeyStr))
                {
                    continue;
                }

                char keyChar = char.ToLower(cmdKeyStr[0]);
                cmdChar = keyChar;

                if (cmdChar == 'x' || cmdChar == 'l' || cmdChar == 'i' ||
                    directions.Contains(cmdChar.ToString().ToUpper()))
                {
                    ui.Write("[bold]" + char.ToUpper(cmdChar).ToString() + "[/]");
                    ui.WriteLine();
                    break;
                }
            }
            ui.WriteLine();
            return cmdChar;
        }

        /// <summary>
        /// Asynchronously processes the player's command input, updating game state and handling actions such as movement,
        /// looking around, viewing inventory, or exiting the game. Also manages invalid commands and ensures
        /// the player's current room is valid before executing actions.
        /// </summary>
        /// <param name="cmdChar">The character representing the player's chosen command.</param>
        /// <param name="ui">The user interface for input and output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        /// <param name="llmClient">AI client for generating room descriptions as needed.</param>
        /// <param name="dungeon">The Dungeon instance containing rooms and state.</param>
        /// <param name="player">The Adventurer representing the player.</param>
        /// <param name="directions">A list of valid movement directions (e.g., "N", "E", "S", "W").</param>
        /// <returns>
        /// A task that resolves to true to continue the game loop; false to exit the game loop (e.g., when the player chooses to exit).
        /// </returns>
        private static async Task<bool> ProcessCommandAsync(
            char cmdChar,
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player,
            List<string> directions)
        {
            // Null check player current room (to avoid NullReferenceException)
            if (player.CurrentRoom == null)
            {
                ui.WriteLine("[bold red]Error:[/] Current room is null. Cannot process command.");
                return true; // Continue the loop to avoid breaking the game
            }

            if (cmdChar == 'x')
            {
                ui.WriteLine("Exiting the game. [bold]Goodbye![/]");
                return false;
            }
            else if (cmdChar == 'l')
            {
                ui.WriteLine(player.CurrentRoom.Description);
            }
            else if (cmdChar == 'i')
            {
                ui.WriteLine("Your inventory contains:");
                if (player.Inventory == null || player.Inventory.Count == 0)
                {
                    ui.WriteLine("  [dim](You are not carrying anything.)[/]");
                }
                else
                {
                    foreach (Entity item in player.Inventory)
                    {
                        ui.WriteLine($" - {item.Name}");
                    }
                }
            }
            else if (directions.Contains(cmdChar.ToString().ToUpper()))
            {
                RoomDirection moveDir = cmdChar switch
                {
                    'n' => RoomDirection.North,
                    'e' => RoomDirection.East,
                    's' => RoomDirection.South,
                    'w' => RoomDirection.West,
                    _ => throw new InvalidOperationException("Invalid direction")
                };

                Room? nextRoom = player.CurrentRoom.GetNeighbour(moveDir, dungeon.Grid);
                if (nextRoom != null)
                {
                    if (string.IsNullOrWhiteSpace(nextRoom.Description))
                    {
                        HashSet<Room> roomsToProcess = new HashSet<Room> { nextRoom };
                        List<Room> firstLevel = nextRoom.GetAccessibleNeighbours(dungeon.Grid);

                        foreach (Room neighbor in firstLevel)
                        {
                            roomsToProcess.Add(neighbor);
                        }

                        foreach (Room neighbor in firstLevel)
                        {
                            List<Room> secondLevel = neighbor.GetAccessibleNeighbours(dungeon.Grid);
                            foreach (Room secondNeighbor in secondLevel)
                            {
                                roomsToProcess.Add(secondNeighbor);
                            }
                        }

                        // Generate descriptions for the next room and its neighbors
                        await RoomDescriptionGenerator.GenerateRoomDescriptionsAsync(roomsToProcess.ToList(), dungeon.Theme, llmClient, logger);

                        // Extract rooms that have entities that are treasure chests
                        List<Room> roomsWithChests = roomsToProcess
                          .Where(r => r.Contents.Any(e => e.Type == EntityType.TreasureChest))
                          .ToList();

                        // If there are any rooms with treasure chests, generate descriptions for them
                        if (roomsWithChests.Count > 0)
                        {
                            await TreasureChestFactory.GenerateTreasureDescriptionsAsync(roomsWithChests, dungeon.Theme, llmClient, logger);
                        }
                    }
                    player.CurrentRoom = nextRoom;
                    player.VisitedRoomIds.Add(nextRoom.Id);
                    ui.Clear();
                }
                else
                {
                    ui.WriteLine("You can't go that way.");
                }
            }
            else
            {
                ui.WriteLine("[bold red]Invalid command.[/] Please try again.");
            }
            return true;
        }
    }
}