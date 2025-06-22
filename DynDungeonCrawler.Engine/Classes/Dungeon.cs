using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Data;
using DynDungeonCrawler.Engine.Interfaces;
using DynDungeonCrawler.Engine.Classes; // For MapCellInfo, MapCellType, MapLegend
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
        private readonly Room[,] grid;
        private readonly List<Room> rooms = new List<Room>();

        private int width;
        private int height;
        private string theme;
        private static readonly Random random = Random.Shared;
        private int minPathLength = DungeonDefaults.DefaultEscapePathLength; // Minimum rooms from Entrance to Exit
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
        /// Gets the grid of rooms in the dungeon.
        /// </summary>
        public Room[,] Grid => grid;

        /// <summary>
        /// Gets the list of rooms in the dungeon.
        /// </summary>
        public IReadOnlyList<Room> Rooms => rooms;

        /// <summary>
        /// Encapsulated method to add a room to the dungeon.
        /// </summary>
        /// <param name="room">The room to add.</param>
        public void AddRoom(Room room)
        {
            rooms.Add(room);
        }

        /// <summary>
        /// Encapsulated method to remove a room from the dungeon.
        /// </summary>
        /// <param name="room">The room to remove.</param>
        public void RemoveRoom(Room room)
        {
            rooms.Remove(room);
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
        /// Loads a dungeon from a JSON file at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to load the JSON file from.</param>
        /// <param name="llmClient">The LLM client used for generating content.</param>
        /// <param name="logger">The logger used for logging messages.</param>
        /// <returns>A Dungeon instance deserialized from the JSON file.</returns>
        public static Dungeon LoadFromJson(string filePath, ILLMClient llmClient, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dungeon JSON file not found.", filePath);

            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);

            var json = File.ReadAllText(filePath);
            var dungeonData = JsonSerializer.Deserialize<DungeonData>(json)
                              ?? throw new InvalidOperationException("Failed to deserialize dungeon data.");

            // Create the dungeon instance
            var dungeon = new Dungeon(dungeonData.Width, dungeonData.Height, dungeonData.Theme, llmClient, logger);

            // Prepare grid and room list
            var grid = dungeon.Grid;
            var rooms = dungeon.Rooms as List<Room>;
            if (rooms == null)
                throw new InvalidOperationException("Dungeon.Rooms is not a List<Room>.");

            // Reconstruct rooms
            foreach (var roomData in dungeonData.Rooms)
            {
                var room = new Room(roomData.X, roomData.Y)
                {
                    Id = roomData.Id,
                    Type = Enum.TryParse<RoomType>(roomData.Type, out var type) ? type : RoomType.Normal,
                    Description = roomData.Description ?? string.Empty,
                    ConnectedNorth = roomData.ConnectedNorth,
                    ConnectedEast = roomData.ConnectedEast,
                    ConnectedSouth = roomData.ConnectedSouth,
                    ConnectedWest = roomData.ConnectedWest,
                };

                // Reconstruct entities. Use AddEntity for validation and encapsulation
                if (roomData.Contents is not null && roomData.Contents.Count > 0)
                {
                    foreach (var entityData in roomData.Contents)
                    {
                        var entity = EntityFactory.FromEntityData(entityData);
                        if (entity != null)
                            room.AddEntity(entity);
                    }
                }

                grid[room.X, room.Y] = room;
                dungeon.AddRoom(room);
            }

            return dungeon;
        }

        /// <summary>
        /// Returns a 2D array of MapCellInfo representing the dungeon map for UI rendering.
        /// </summary>
        /// <param name="showEntities">Whether to show entities in the map.</param>
        /// <returns>2D array of MapCellInfo for the map display.</returns>
        public MapCellInfo[,] GetMapCells(bool showEntities = false)
        {
            int minX = width, minY = height, maxX = 0, maxY = 0;
            foreach (Room room in rooms)
            {
                if (room.X < minX) minX = room.X;
                if (room.Y < minY) minY = room.Y;
                if (room.X > maxX) maxX = room.X;
                if (room.Y > maxY) maxY = room.Y;
            }
            int mapWidth = maxX - minX + 5; // +4 for border, +1 for inclusive
            int mapHeight = maxY - minY + 5;
            var cells = new MapCellInfo[mapWidth, mapHeight];
            var mainPathDirections = new Dictionary<(int, int), TravelDirection>();
            Room? entrance = rooms.Find(r => r.Type == RoomType.Entrance);
            if (entrance != null && !showEntities)
            {
                FindMainPath(entrance, mainPathDirections);
            }
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int gridX = minX - 2 + x;
                    int gridY = minY - 2 + y;
                    Room? room = (IsInBounds(gridX, gridY)) ? grid[gridX, gridY] : null;
                    var cell = new MapCellInfo { X = gridX, Y = gridY };
                    if (room == null)
                    {
                        cell.Symbol = '.';
                        cell.CellType = MapCellType.Empty;
                    }
                    else if (room.Type == RoomType.Entrance)
                    {
                        cell.Symbol = 'E';
                        cell.CellType = MapCellType.Entrance;
                    }
                    else if (room.Type == RoomType.Exit)
                    {
                        cell.Symbol = 'X';
                        cell.CellType = MapCellType.Exit;
                    }
                    else if (showEntities && room.Contents.Any(c => c.Type == EntityType.TreasureChest))
                    {
                        cell.Symbol = 'T';
                        cell.CellType = MapCellType.TreasureChest;
                    }
                    else if (showEntities && room.Contents.Any(c => c.Type == EntityType.Enemy))
                    {
                        cell.Symbol = '@';
                        cell.CellType = MapCellType.Enemy;
                    }
                    else if (!showEntities && mainPathDirections.ContainsKey((room.X, room.Y)))
                    {
                        cell.CellType = MapCellType.MainPath;
                        cell.MainPathDirection = mainPathDirections[(room.X, room.Y)];
                        switch (mainPathDirections[(room.X, room.Y)])
                        {
                            case TravelDirection.North: cell.Symbol = '^'; break;
                            case TravelDirection.East: cell.Symbol = '>'; break;
                            case TravelDirection.South: cell.Symbol = 'v'; break;
                            case TravelDirection.West: cell.Symbol = '<'; break;
                            case TravelDirection.None: cell.Symbol = '+'; break;
                            default: cell.Symbol = '#'; break;
                        }
                    }
                    else
                    {
                        cell.Symbol = '#';
                        cell.CellType = MapCellType.Room;
                    }
                    cells[x, y] = cell;
                }
            }
            return cells;
        }
    }
}