using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Data;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Classes
{
    public enum TravelDirection
    {
        None,
        North,
        East,
        South,
        West
    }

    public class Dungeon
    {
        private Room[,] grid;

        private int width;
        private int height;
        private string theme;
        private static readonly Random random = Random.Shared;
        private int minPathLength = DungeonDefaults.DefaultEscapePathLength; // Minimum rooms from Entrance to Exit
        private List<Room> rooms = new List<Room>();
        private List<EnemyTypeInfo>? enemyTypes;
        private readonly ILLMClient _llmClient;
        private readonly ILogger _logger;

        public const int MaxDungeonWidth = 1000;
        public const int MaxDungeonHeight = 1000;

        /// <summary>
        /// Initializes a new instance of the Dungeon class with the specified width, height, theme, and LLM client.
        /// </summary>
        /// <param name="width">The width of the dungeon grid.</param>
        /// <param name="height">The height of the dungeon grid.</param>
        /// <param name="theme">The theme of the dungeon.</param>
        /// <param name="llmClient">The LLM client used for generating content.</param>
        /// <param name="logger">The logger used for logging messages.</param>
        public Dungeon(int width, int height, string theme, ILLMClient llmClient, ILogger logger)
        {
            if (width < 1 || width > MaxDungeonWidth)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"Width must be between 1 and {MaxDungeonWidth} (was {width}).");

            if (height < 1 || height > MaxDungeonHeight)
                throw new ArgumentOutOfRangeException(nameof(height),
                    $"Height must be between 1 and {MaxDungeonHeight} (was {height}).");

            if (string.IsNullOrWhiteSpace(theme))
                throw new ArgumentException("Theme cannot be empty or whitespace.", nameof(theme));

            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);
            _llmClient = llmClient;
            _logger = logger;

            this.width = width;
            this.height = height;
            this.theme = theme.Trim();
            grid = new Room[width, height];

            _logger.Log($"Dungeon space initialized with maximum dimensions of {width}x{height}.");
        }

        /// <summary>
        /// Initializes a new instance of the Dungeon class with the specified width and height.
        /// The theme is set to the default value.
        /// </summary>
        /// <param name="width">The width of the dungeon grid.</param>
        /// <param name="height">The height of the dungeon grid.</param>
        /// <param name="llmClient">The LLM client used for generating content.</param>
        /// <param name="logger">The logger used for logging messages.</param>
        public Dungeon(int width, int height, ILLMClient llmClient, ILogger logger)
            : this(width, height, DungeonDefaults.DefaultDungeonDescription, llmClient, logger)
        {
        }

        /// <summary>
        /// Returns a list of rooms directly connected to the given room.
        /// </summary>
        /// <param name="room">The source room.</param>
        /// <returns>List of adjacent, connected rooms.</returns>
        public List<Room> GetConnectedRooms(Room room)
        {
            return GetConnectedRoomsWithDirections(room).Values.ToList();
        }

        /// <summary>
        /// Returns a dictionary of connected rooms and their directions relative to the given room.
        /// </summary>
        /// <param name="room">The source room.</param>
        /// <returns>Dictionary mapping TravelDirection to connected Room.</returns>
        public Dictionary<TravelDirection, Room> GetConnectedRoomsWithDirections(Room room)
        {
            Dictionary<TravelDirection, Room> connectedRooms = new Dictionary<TravelDirection, Room>();

            if (room.ConnectedNorth && IsInBounds(room.X, room.Y - 1))
            {
                Room? northRoom = grid[room.X, room.Y - 1];
                if (northRoom != null)
                    connectedRooms[TravelDirection.North] = northRoom;
            }

            if (room.ConnectedEast && IsInBounds(room.X + 1, room.Y))
            {
                Room? eastRoom = grid[room.X + 1, room.Y];
                if (eastRoom != null)
                    connectedRooms[TravelDirection.East] = eastRoom;
            }

            if (room.ConnectedSouth && IsInBounds(room.X, room.Y + 1))
            {
                Room? southRoom = grid[room.X, room.Y + 1];
                if (southRoom != null)
                    connectedRooms[TravelDirection.South] = southRoom;
            }

            if (room.ConnectedWest && IsInBounds(room.X - 1, room.Y))
            {
                Room? westRoom = grid[room.X - 1, room.Y];
                if (westRoom != null)
                    connectedRooms[TravelDirection.West] = westRoom;
            }

            return connectedRooms;
        }

        /// <summary>
        /// Gets the Theme of the dungeon.
        /// </summary>
        public string Theme => theme;

        /// <summary>
        /// Generates the dungeon layout, including a main path from the entrance to the exit
        /// and additional side branches.
        /// </summary>
        public void GenerateDungeon()
        {
            int startX = width / 2;
            int startY = height / 2;

            // Create the entrance room
            Room entrance = new Room(startX, startY)
            {
                Type = RoomType.Entrance // Explicitly set RoomType
            };

            grid[startX, startY] = entrance;
            rooms.Add(entrance);

            Stack<Room> roomStack = new Stack<Room>();
            roomStack.Push(entrance);

            _logger.Log($"Dungeon entrance created at ({startX}, {startY}).");

            int roomsPlaced = 1;
            int targetPathLength = random.Next(minPathLength, minPathLength + 10);

            _logger.Log($"Creating main path with target length: {targetPathLength} rooms.");

            // Generate the main path
            while (roomsPlaced < targetPathLength)
            {
                if (roomStack.Count == 0)
                {
                    _logger.Log("Warning: Could not reach the desired path length. Dungeon may be smaller than expected.");
                    break;
                }

                Room currentRoom = roomStack.Peek();
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = GetAvailableDirections(currentRoom);

                if (availableDirections.Count == 0)
                {
                    roomStack.Pop(); // Dead end, backtrack
                    continue;
                }

                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = currentRoom.X + chosen.dx;
                int newY = currentRoom.Y + chosen.dy;

                // Create a new room with default RoomType (Normal)
                Room newRoom = new Room(newX, newY);

                chosen.setExitFrom(currentRoom);
                chosen.setExitTo(newRoom);

                grid[newX, newY] = newRoom;
                rooms.Add(newRoom);
                roomStack.Push(newRoom);

                roomsPlaced++;
            }

            if (roomStack.Count > 0)
            {
                Room exitRoom = roomStack.Peek();
                exitRoom.Type = RoomType.Exit; // Explicitly set RoomType for the exit
                _logger.Log($"Dungeon exit created at ({exitRoom.X}, {exitRoom.Y}).");
            }

            // Add side branches
            _logger.Log($"Adding side branches...");
            int extraBranches = 30;
            for (int i = 0; i < extraBranches; i++)
            {
                CreateBranchPath();
            }
        }

        /// <summary>
        /// Creates a side branch of rooms starting from a random room in the dungeon.
        /// </summary>
        private void CreateBranchPath()
        {
            if (rooms.Count == 0) return;

            Room fromRoom = rooms[random.Next(rooms.Count)];
            int branchLength = random.Next(2, 6); // Branch of 2–5 rooms
            Room current = fromRoom;

            for (int i = 0; i < branchLength; i++)
            {
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = GetAvailableDirections(current);

                if (availableDirections.Count == 0)
                {
                    break; // Dead end
                }

                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;

                // Create a new room with default RoomType (Normal)
                Room newRoom = new Room(newX, newY);

                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);

                grid[newX, newY] = newRoom;
                rooms.Add(newRoom);

                current = newRoom;

                // 20% chance to spawn a mini sub-branch
                if (random.NextDouble() < 0.2)
                {
                    CreateBranchPathFrom(current);
                }

                // 30% chance to loop at the end of branch
                if (i == branchLength - 1 && random.NextDouble() < 0.3)
                {
                    TryCreateLoop(current);
                }
            }
        }

        /// <summary>
        /// Creates a small sub-branch of rooms starting from the specified room.
        /// </summary>
        /// <param name="startRoom">The room to start the sub-branch from.</param>
        private void CreateBranchPathFrom(Room startRoom)
        {
            int branchLength = random.Next(1, 4); // Small sub-branch (1–3 rooms)
            Room current = startRoom;

            for (int i = 0; i < branchLength; i++)
            {
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = GetAvailableDirections(current);

                if (availableDirections.Count == 0)
                    break;

                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;

                // Create a new room with default RoomType (Normal)
                Room newRoom = new Room(newX, newY);

                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);

                grid[newX, newY] = newRoom;
                rooms.Add(newRoom);

                current = newRoom;
            }
        }

        /// <summary>
        /// Attempts to create a loop by connecting the specified room to an existing room.
        /// </summary>
        /// <param name="room">The room to attempt to connect to an existing room.</param>
        private void TryCreateLoop(Room room)
        {
            List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> directions = new List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>();

            if (IsInBounds(room.X, room.Y - 1) && grid[room.X, room.Y - 1] != null)
                directions.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            if (IsInBounds(room.X + 1, room.Y) && grid[room.X + 1, room.Y] != null)
                directions.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            if (IsInBounds(room.X, room.Y + 1) && grid[room.X, room.Y + 1] != null)
                directions.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            if (IsInBounds(room.X - 1, room.Y) && grid[room.X - 1, room.Y] != null)
                directions.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));

            if (directions.Count > 0)
            {
                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = directions[random.Next(directions.Count)];
                int targetX = room.X + chosen.dx;
                int targetY = room.Y + chosen.dy;

                Room targetRoom = grid[targetX, targetY];

                if (targetRoom != null)
                {
                    chosen.setExitFrom(room);
                    chosen.setExitTo(targetRoom);
                }
            }
        }

        /// <summary>
        /// Determines the available directions for placing a new room from the specified room.
        /// </summary>
        /// <param name="fromRoom">The room to check for available directions.</param>
        /// <returns>A list of available directions and actions to set connections.</returns>
        private List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> GetAvailableDirections(Room fromRoom)
        {
            List<(int dx, int dy, Action<Room>, Action<Room>)> available = new List<(int dx, int dy, Action<Room>, Action<Room>)>();

            if (IsInBounds(fromRoom.X, fromRoom.Y - 1) && grid[fromRoom.X, fromRoom.Y - 1] == null)
                available.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            if (IsInBounds(fromRoom.X + 1, fromRoom.Y) && grid[fromRoom.X + 1, fromRoom.Y] == null)
                available.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            if (IsInBounds(fromRoom.X, fromRoom.Y + 1) && grid[fromRoom.X, fromRoom.Y + 1] == null)
                available.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            if (IsInBounds(fromRoom.X - 1, fromRoom.Y) && grid[fromRoom.X - 1, fromRoom.Y] == null)
                available.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));

            return available;
        }

        /// <summary>
        /// Checks if the specified coordinates are within the bounds of the dungeon grid.
        /// </summary>
        /// <param name="x">The X coordinate to check.</param>
        /// <param name="y">The Y coordinate to check.</param>
        /// <returns>True if the coordinates are within bounds; otherwise, false.</returns>
        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        /// <summary>
        /// Prints the dungeon map to the console, showing rooms and their connections.
        /// </summary>
        /// <param name="showEntities">Wether to show entities in the map.</param>
        public void PrintDungeonMap(bool showEntities = false)
        {
            int minX = width, minY = height, maxX = 0, maxY = 0;

            foreach (Room room in rooms)
            {
                if (room.X < minX) minX = room.X;
                if (room.Y < minY) minY = room.Y;
                if (room.X > maxX) maxX = room.X;
                if (room.Y > maxY) maxY = room.Y;
            }

            Dictionary<(int, int), TravelDirection> mainPathDirections = new Dictionary<(int, int), TravelDirection>();
            Room? entrance = rooms.Find(static r => r.Type == RoomType.Entrance);

            if (entrance != null && !showEntities)
            {
                FindMainPath(entrance, mainPathDirections);
            }

            for (int y = minY - 2; y <= maxY + 2; y++)
            {
                for (int x = minX - 2; x <= maxX + 2; x++)
                {
                    Room? room = IsInBounds(x, y) ? grid[x, y] : null;

                    if (room == null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(".");
                    }
                    else if (room.Type == RoomType.Entrance)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("E");
                    }
                    else if (room.Type == RoomType.Exit)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("X");
                    }
                    else if (showEntities && room.Contents.Any(c => c.Type == EntityType.TreasureChest))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("T");
                    }
                    else if (showEntities && room.Contents.Any(c => c.Type == EntityType.Enemy))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("@");
                    }
                    else if (!showEntities && mainPathDirections.ContainsKey((room.X, room.Y)))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        switch (mainPathDirections[(room.X, room.Y)])
                        {
                            case TravelDirection.North:
                                Console.Write("^");
                                break;

                            case TravelDirection.East:
                                Console.Write(">");
                                break;

                            case TravelDirection.South:
                                Console.Write("v");
                                break;

                            case TravelDirection.West:
                                Console.Write("<");
                                break;

                            case TravelDirection.None:
                                Console.Write("+");
                                break;
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("#");
                    }
                }
                Console.WriteLine();
            }

            Console.ResetColor();

            // 🗺️ Print Map Legend
            PrintLegend(showEntities);
        }

        /// <summary>
        /// Prints a legend for the dungeon map to the console.
        /// </summary>
        /// <param name="showEntities">Wether to show entities in the legend.</param>
        private static void PrintLegend(bool showEntities)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Legend:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" E = Entrance");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" X = Exit");

            if (showEntities)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(" T = Treasure Chest");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(" @ = Enemy");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" ^ > v < = Main Path Direction");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" # = Room");
            Console.WriteLine(" . = Empty Space");
            Console.ResetColor();
        }

        /// <summary>
        /// Finds the main path from the entrance to the exit and stores the directions in the provided dictionary.
        /// </summary>
        /// <param name="currentRoom">The starting room for the path search.</param>
        /// <param name="mainPathDirections">A dictionary to store the directions of the main path.</param>
        /// <returns>True if the path to the exit is found; otherwise, false.</returns>
        private bool FindMainPath(Room currentRoom, Dictionary<(int, int), TravelDirection> mainPathDirections)
        {
            Dictionary<(int, int), TravelDirection> tempPath = new Dictionary<(int, int), TravelDirection>();
            bool found = FindMainPathRecursive(currentRoom, tempPath);

            if (found)
            {
                foreach (KeyValuePair<(int, int), TravelDirection> kvp in tempPath)
                {
                    mainPathDirections[kvp.Key] = kvp.Value;
                }
            }

            return found;
        }

        /// <summary>
        /// Recursively searches for the main path from the current room to the exit.
        /// </summary>
        /// <param name="currentRoom">The current room being searched.</param>
        /// <param name="path">A dictionary to store the directions of the path.</param>
        /// <returns>True if the path to the exit is found; otherwise, false.</returns>
        private bool FindMainPathRecursive(Room currentRoom, Dictionary<(int, int), TravelDirection> path)
        {
            if (!path.ContainsKey((currentRoom.X, currentRoom.Y)))
                path[(currentRoom.X, currentRoom.Y)] = TravelDirection.None;

            if (currentRoom.Type == RoomType.Exit) // Updated to use Room.Type
            {
                return true;
            }

            // Try all exits
            if (currentRoom.ConnectedNorth && IsInBounds(currentRoom.X, currentRoom.Y - 1))
            {
                Room next = grid[currentRoom.X, currentRoom.Y - 1];
                if (next != null && !path.ContainsKey((next.X, next.Y)))
                {
                    if (FindMainPathRecursive(next, path))
                    {
                        path[(currentRoom.X, currentRoom.Y)] = TravelDirection.North;
                        return true;
                    }
                }
            }

            if (currentRoom.ConnectedEast && IsInBounds(currentRoom.X + 1, currentRoom.Y))
            {
                Room next = grid[currentRoom.X + 1, currentRoom.Y];
                if (next != null && !path.ContainsKey((next.X, next.Y)))
                {
                    if (FindMainPathRecursive(next, path))
                    {
                        path[(currentRoom.X, currentRoom.Y)] = TravelDirection.East;
                        return true;
                    }
                }
            }

            if (currentRoom.ConnectedSouth && IsInBounds(currentRoom.X, currentRoom.Y + 1))
            {
                Room next = grid[currentRoom.X, currentRoom.Y + 1];
                if (next != null && !path.ContainsKey((next.X, next.Y)))
                {
                    if (FindMainPathRecursive(next, path))
                    {
                        path[(currentRoom.X, currentRoom.Y)] = TravelDirection.South;
                        return true;
                    }
                }
            }

            if (currentRoom.ConnectedWest && IsInBounds(currentRoom.X - 1, currentRoom.Y))
            {
                Room next = grid[currentRoom.X - 1, currentRoom.Y];
                if (next != null && !path.ContainsKey((next.X, next.Y)))
                {
                    if (FindMainPathRecursive(next, path))
                    {
                        path[(currentRoom.X, currentRoom.Y)] = TravelDirection.West;
                        return true;
                    }
                }
            }

            // Dead end - backtrack
            path.Remove((currentRoom.X, currentRoom.Y));
            return false;
        }

        /// <summary>
        /// Serializes the dungeon to a JSON string, including rooms and their contents.
        /// </summary>
        /// <returns>A JSON string representing the dungeon.</returns>
        public string ToJson()
        {
            DungeonData dungeonData = new DungeonData
            {
                Width = width,
                Height = height,
                Theme = theme,
                Rooms = new List<RoomData>()
            };

            foreach (Room room in rooms)
            {
                RoomData roomData = new RoomData
                {
                    Id = room.Id,
                    X = room.X,
                    Y = room.Y,
                    Type = room.Type.ToString(),
                    Description = room.Description,
                    ConnectedNorth = room.ConnectedNorth,
                    ConnectedEast = room.ConnectedEast,
                    ConnectedSouth = room.ConnectedSouth,
                    ConnectedWest = room.ConnectedWest
                };

                // Add entities to RoomData using AddEntity method
                foreach (Entity entity in room.Contents)
                {
                    EntityData entityData = entity.ToEntityData();
                    roomData.AddEntity(entityData);
                }

                dungeonData.Rooms.Add(roomData);
            }

            return JsonSerializer.Serialize(dungeonData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Saves the dungeon to a JSON file at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to save the JSON file to.</param>
        public void SaveToJson(string filePath)
        {
            string json = ToJson();
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Populates the rooms with contents such as treasure chests and enemies.
        /// </summary>
        public async Task PopulateRoomContentsAsync()
        {
            // Generate a list of enemy types based on the dungeon theme
            enemyTypes = await EnemyFactory.GenerateEnemyTypesAsync(theme, _llmClient, _logger);

            // Iterate through all rooms in the dungeon
            foreach (Room room in rooms)
            {
                // Only populate normal rooms (not entrance/exit)
                if (room.Type == RoomType.Normal)
                {
                    double roll = random.NextDouble(); // Roll a random value between 0.0 and 1.0

                    // 10% chance to add a treasure chest (possibly locked)
                    if (roll < 0.1)
                    {
                        bool isLocked = random.NextDouble() < 0.3; // 30% of chests are locked
                        room.Contents.Add(TreasureChestFactory.CreateTreasureChest(isLocked: isLocked));

                        _logger.Log($"Treasure chest added to room at ({room.X}, {room.Y}) -  {(isLocked ? "Locked" : "Unlocked")}.");
                    }
                    // Next 10% chance (i.e., 10% to 20%) to add an enemy
                    else if (roll < 0.2)
                    {
                        // Pick a random enemy type from the master list
                        EnemyTypeInfo enemyType = enemyTypes[random.Next(enemyTypes.Count)];
                        Enemy enemy = EnemyFactory.CreateEnemy(enemyType.Name, theme);
                        enemy.Description = enemyType.Description; // Set enemy description
                        room.Contents.Add(enemy);

                        _logger.Log($"Enemy '{enemy.Name}' added to room at ({room.X}, {room.Y}).");
                    }
                }
            }
        }
    }
}