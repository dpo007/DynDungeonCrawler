using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal class ConDungeon
    {
        private static void Main(string[] args)
        {
            // Initialize the user interface using Spectre.Console for enhanced console output and input.
            IUserInterface ui = new SpectreConsoleUserInterface();

            ui.WriteLine("*** [bold]Dynamic Dungeon Crawler![/] ***");

            // Load settings and check API key
            var settings = Settings.Load();
            if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                ui.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                ui.WriteLine("Press any key to exit.");
                ui.ReadKey();
                return;
            }

            // Setup logger and LLM client
            ILogger logger = new FileLogger(@"C:\temp\ConDungeon.log");
            ILLMClient llmClient = new OpenAIHelper(new HttpClient(), settings.OpenAIApiKey);

            // Load dungeon from JSON
            string filePath = "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);
            ui.WriteLine("[dim]Dungeon loaded successfully.[/]");

            // Display some information about the dungeon
            ui.WriteLine($"Dungeon Theme: \"[white]{dungeon.Theme}[/]\"");
            ui.WriteLine($"Total Rooms: [yellow]{dungeon.Rooms.Count}[/]");
            ui.WriteLine();

            // Ask user for their Name (optional)
            ui.Write("Enter your adventurer's name [gray](or press Enter to generate one)[/]: ");
            string playerName = ui.ReadLine().Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                // Ask user for their Gender
                AdventurerGender gender = AdventurerGender.Unspecified;
                ui.Write("Enter your adventurer's gender ([[[deepskyblue1]M[/]]]ale/[[[hotpink]F[/]]]emale, [gray]or press Enter for unspecified[/]): ");
                while (true)
                {
                    var key = ui.ReadKey(intercept: true);
                    if (key == ConsoleKey.Enter)
                    {
                        ui.WriteLine();
                        break;
                    }
                    else if (key == ConsoleKey.M)
                    {
                        ui.WriteLine("[deepskyblue1]M[/]");
                        gender = AdventurerGender.Male;
                        break;
                    }
                    else if (key == ConsoleKey.F)
                    {
                        ui.WriteLine("[hotpink]F[/]");
                        gender = AdventurerGender.Female;
                        break;
                    }
                    // Any other key: ignore and re-prompt
                }

                // Generate a name using the LLM, passing the theme and gender
                playerName = Adventurer.GenerateNameAsync(llmClient, dungeon.Theme, gender).GetAwaiter().GetResult();
            }

            ui.WriteLine();
            ui.WriteLine($"Welcome to the dungeon [bold underline]{playerName}[/]!");
            ui.WriteLine();

            // Find entrance and create adventurer
            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(playerName, entrance);

            // Main game loop
            while (true)
            {
                // Check for game-ending conditions
                if (player.Health <= 0)
                {
                    ui.WriteLine("You have perished in the dungeon. Game over!");
                    break;
                }
                if (player.CurrentRoom?.Type == RoomType.Exit)
                {
                    ui.WriteLine("Congratulations! You have found the exit and escaped the dungeon!");
                    break;
                }

                if (player.CurrentRoom == null)
                {
                    ui.WriteLine("You are lost in the void. Game over!");
                    break;
                }

                // Display room info.
                ui.WriteRule();
                ui.WriteLine(player.CurrentRoom.Description);

                // Show room contents if any
                if (player.CurrentRoom.Contents.Count > 0)
                {
                    ui.WriteLine();
                    ui.WriteLine("You notice the following things in the room:");
                    foreach (var entity in player.CurrentRoom.Contents)
                    {
                        switch (entity)
                        {
                            case TreasureChest chest:
                                // Show the chest's name and its state (opened, locked, or unlocked)
                                string chestState = chest.IsOpened ? "opened" : (chest.IsLocked ? "locked" : "unlocked");
                                ui.WriteLine($"[dim]-[/] [bold]{chest.Name}[/] ({chestState})");
                                break;

                            default:
                                // Prefer ShortDescription if available, otherwise use Description
                                string displayDesc = !string.IsNullOrWhiteSpace(entity.ShortDescription)
                                    ? entity.ShortDescription
                                    : entity.Description;
                                if (!string.IsNullOrWhiteSpace(displayDesc))
                                    // Show the entity's name and the chosen description
                                    ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]: {displayDesc}");
                                else
                                    // If no description is available, just show the name
                                    ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]");
                                break;
                        }
                    }
                }

                ui.WriteRule();
                ui.WriteLine();

                // Build available directions string
                List<string> directions = new();
                if (player.CurrentRoom.ConnectedNorth) directions.Add("N");
                if (player.CurrentRoom.ConnectedEast) directions.Add("E");
                if (player.CurrentRoom.ConnectedSouth) directions.Add("S");
                if (player.CurrentRoom.ConnectedWest) directions.Add("W");
                string directionsPrompt = directions.Count > 0 ? string.Join("[dim]/[/]", directions) : "";

                // Ask for player input (single letter only) in a loop until a valid command is entered
                ui.Write($"Enter command (move [[[bold]{directionsPrompt}[/]]], [[[bold]L[/]]]ook, [[[bold]I[/]]]nventory, e[[[bold]X[/]]]it): ");
                char cmdChar;
                while (true)
                {
                    var cmdKey = ui.ReadKey(intercept: true);
                    cmdChar = char.ToLower((char)cmdKey);

                    // Validate command
                    if (cmdChar == 'x' || cmdChar == 'l' || cmdChar == 'i' ||
                        directions.Contains(cmdChar.ToString().ToUpper()))
                    {
                        // Valid command entered
                        ui.Write("[bold]" + char.ToUpper(cmdChar).ToString() + "[/]");
                        ui.WriteLine();
                        break;
                    }
                    // Otherwise, ignore and re-prompt (do not display the character)
                }

                ui.WriteLine();

                // Handle player commands
                if (cmdChar == 'x')
                {
                    ui.WriteLine("Exiting the game. [bold]Goodbye![/]");
                    break;
                }
                else if (cmdChar == 'l')
                {
                    ui.WriteLine(player.CurrentRoom.Description);
                }
                else if (cmdChar == 'i')
                {
                    ui.WriteLine("Your inventory contains:");
                    foreach (var item in player.Inventory)
                    {
                        ui.WriteLine($" - {item.Name}");
                    }
                }
                else if (directions.Contains(cmdChar.ToString().ToUpper()))
                {
                    // Determine direction
                    RoomDirection moveDir = cmdChar switch
                    {
                        'n' => RoomDirection.North,
                        'e' => RoomDirection.East,
                        's' => RoomDirection.South,
                        'w' => RoomDirection.West,
                        _ => throw new InvalidOperationException("Invalid direction")
                    };

                    // Attempt to move
                    Room? nextRoom = player.CurrentRoom.GetNeighbour(moveDir, dungeon.Grid);
                    if (nextRoom != null)
                    {
                        // If the next room does not have a description, generate descriptions for it and its accessible neighbors (2 levels deep)
                        if (string.IsNullOrWhiteSpace(nextRoom.Description))
                        {
                            // Use a HashSet to avoid duplicate rooms
                            HashSet<Room> roomsToProcess = new HashSet<Room> { nextRoom };

                            // First level: direct neighbors
                            var firstLevel = nextRoom.GetAccessibleNeighbours(dungeon.Grid);
                            foreach (var neighbor in firstLevel)
                                roomsToProcess.Add(neighbor);

                            // Second level: neighbors of neighbors
                            foreach (var neighbor in firstLevel)
                            {
                                var secondLevel = neighbor.GetAccessibleNeighbours(dungeon.Grid);
                                foreach (var secondNeighbor in secondLevel)
                                    roomsToProcess.Add(secondNeighbor);
                            }

                            // Generate descriptions for all collected rooms
                            Room.GenerateRoomDescriptionsAsync(roomsToProcess.ToList(), dungeon.Theme, llmClient, logger).GetAwaiter().GetResult();
                        }

                        // Move the player to the next room and record the visit
                        player.CurrentRoom = nextRoom;
                        player.VisitedRoomIds.Add(nextRoom.Id);

                        // Clear the UI before displaying the new room
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
                    continue; // Re-prompt for command
                }
            }
        }
    }
}