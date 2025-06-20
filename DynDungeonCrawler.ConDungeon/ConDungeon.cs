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
            Console.WriteLine("Welcome to the Dungeon Crawler!");

            // Load settings and check API key
            var settings = Settings.Load();
            if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                Console.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Setup logger and LLM client
            ILogger logger = new FileLogger(@"C:\temp\ConDungeon.log");
            ILLMClient llmClient = new OpenAIHelper(new HttpClient(), settings.OpenAIApiKey);

            // Load dungeon from JSON
            string filePath = "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);
            Console.WriteLine("Dungeon loaded successfully.");

            // Display the dungeon theme
            Console.WriteLine($"Dungeon Theme: \"{dungeon.Theme}\"");

            // Ask user for their Name (optional)
            Console.Write("Enter your adventurer's name (or press Enter to generate one): ");
            string playerName = Console.ReadLine()?.Trim() ?? string.Empty;

            // Ask user for their Gender (M/F, or press Enter for unspecified)
            AdventurerGender gender = AdventurerGender.Unspecified;
            Console.Write("Enter your adventurer's gender ([M]ale/[F]emale, or press Enter for unspecified): ");
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.M)
                {
                    Console.WriteLine("M");
                    gender = AdventurerGender.Male;
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.F)
                {
                    Console.WriteLine("F");
                    gender = AdventurerGender.Female;
                    break;
                }
                // Any other key: ignore and re-prompt
            }

            if (string.IsNullOrEmpty(playerName))
            {
                // Generate a name using the LLM, passing the theme and gender
                playerName = Adventurer.GenerateNameAsync(llmClient, dungeon.Theme, gender).GetAwaiter().GetResult();
                Console.WriteLine($"Generated adventurer name: {playerName}");
            }

            // Find entrance and create adventurer
            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(playerName, entrance);

            // Main game loop
            while (true)
            {
                // Check for game-ending conditions
                if (player.Health <= 0)
                {
                    Console.WriteLine("You have perished in the dungeon. Game over!");
                    break;
                }
                if (player.CurrentRoom?.Type == RoomType.Exit)
                {
                    Console.WriteLine("Congratulations! You have found the exit and escaped the dungeon!");
                    break;
                }

                if (player.CurrentRoom == null)
                {
                    Console.WriteLine("You are lost in the void. Game over!");
                    break;
                }

                // Display room info, including exits
                Console.WriteLine(player.CurrentRoom.Description);

                // Show room contents if any
                if (player.CurrentRoom.Contents.Count > 0)
                {
                    Console.WriteLine("You notice the following in the room:");
                    foreach (var entity in player.CurrentRoom.Contents)
                    {
                        switch (entity)
                        {
                            case TreasureChest chest:
                                string chestState = chest.IsOpened ? "opened" : (chest.IsLocked ? "locked" : "unlocked");
                                Console.WriteLine($" - {chest.Name} ({chestState})");
                                break;

                            default:
                                // Use Name and Description if available
                                if (!string.IsNullOrWhiteSpace(entity.Description))
                                    Console.WriteLine($" - {entity.Name}: {entity.Description}");
                                else
                                    Console.WriteLine($" - {entity.Name}");
                                break;
                        }
                    }
                }

                Console.WriteLine("Exits:");
                if (player.CurrentRoom.ConnectedNorth)
                    Console.WriteLine(" - North");
                if (player.CurrentRoom.ConnectedEast)
                    Console.WriteLine(" - East");
                if (player.CurrentRoom.ConnectedSouth)
                    Console.WriteLine(" - South");
                if (player.CurrentRoom.ConnectedWest)
                    Console.WriteLine(" - West");

                // Build available directions string
                List<string> directions = new();
                if (player.CurrentRoom.ConnectedNorth) directions.Add("N");
                if (player.CurrentRoom.ConnectedEast) directions.Add("E");
                if (player.CurrentRoom.ConnectedSouth) directions.Add("S");
                if (player.CurrentRoom.ConnectedWest) directions.Add("W");
                string directionsPrompt = directions.Count > 0 ? string.Join("/", directions) : "";

                // Ask for player input (single letter only) in a loop until a valid command is entered
                Console.Write($"Enter command (move [{directionsPrompt}], [L]ook, [I]nventory, e[X]it): ");
                char cmdChar;
                while (true)
                {
                    ConsoleKeyInfo cmdKeyInfo = Console.ReadKey(intercept: true);
                    cmdChar = char.ToLower(cmdKeyInfo.KeyChar);

                    // Validate command
                    if (cmdChar == 'x' || cmdChar == 'l' || cmdChar == 'i' ||
                        directions.Contains(cmdChar.ToString().ToUpper()))
                    {
                        // Valid command entered
                        // Show the accepted character
                        Console.Write(char.ToUpper(cmdKeyInfo.KeyChar));
                        Console.WriteLine();
                        break;
                    }
                    // Otherwise, ignore and re-prompt (do not display the character)
                }

                // Handle player commands
                if (cmdChar == 'x')
                {
                    Console.WriteLine("Exiting the game. Goodbye!");
                    break;
                }
                else if (cmdChar == 'l')
                {
                    Console.WriteLine(player.CurrentRoom.Description);
                }
                else if (cmdChar == 'i')
                {
                    Console.WriteLine("Your inventory contains:");
                    foreach (var item in player.Inventory)
                    {
                        Console.WriteLine($" - {item.Name}");
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
                        // If the next room doesn't have a description, create a list of it and its accessible neighbours
                        if (string.IsNullOrWhiteSpace(nextRoom.Description))
                        {
                            List<Room> roomsToProcess = new List<Room> { nextRoom };
                            roomsToProcess.AddRange(nextRoom.GetAccessibleNeighbours(dungeon.Grid));

                            // Generate descriptons for that list of rooms
                            Room.GenerateRoomDescriptionsAsync(roomsToProcess, dungeon.Theme, llmClient, logger).GetAwaiter().GetResult();
                        }

                        // Update player's current room and visited rooms
                        player.CurrentRoom = nextRoom;
                        player.VisitedRoomIds.Add(nextRoom.Id);
                    }
                    else
                    {
                        // Invalid move, room not connected in that direction
                        Console.WriteLine("You can't go that way.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command. Please try again.");
                    continue; // Re-prompt for command
                }
            }
        }
    }
}