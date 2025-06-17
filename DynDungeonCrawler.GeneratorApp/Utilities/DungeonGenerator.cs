using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Interfaces;
using DynDungeonCrawler.GeneratorApp.Factories;

namespace DynDungeonCrawler.GeneratorApp.Utilities
{
    public static class DungeonGenerator
    {
        public static Dungeon GenerateDungeon(
            int width,
            int height,
            string theme,
            ILLMClient llmClient,
            ILogger logger)
        {
            // Create a new Dungeon instance with the specified parameters
            var dungeon = new Dungeon(width, height, theme, llmClient, logger);

            int startX = width / 2;
            int startY = height / 2;

            // Create the entrance room at the center of the grid
            Room entrance = new Room(startX, startY)
            {
                Type = RoomType.Entrance
            };

            Room[,] grid = dungeon.Grid;

            // Get the internal list of rooms from the dungeon (cast from IReadOnlyList)
            if (dungeon.Rooms is not List<Room> rooms)
            {
                // Defensive: should not happen with current Dungeon implementation
                throw new InvalidOperationException("Dungeon.Rooms is not a List<Room>.");
            }

            // Place the entrance room in the grid and add it to the rooms list
            grid[startX, startY] = entrance;
            rooms.Add(entrance);

            // Stack for backtracking during main path generation
            Stack<Room> roomStack = new Stack<Room>();
            roomStack.Push(entrance);

            logger.Log($"Dungeon entrance created at ({startX}, {startY}).");

            // Set up path length constraints and randomization
            int minPathLength = DungeonDefaults.DefaultEscapePathLength;
            int roomsPlaced = 1;
            var random = Random.Shared;
            int targetPathLength = random.Next(minPathLength, minPathLength + 10);

            logger.Log($"Creating main path with target length: {targetPathLength} rooms.");

            // Helper delegates for stateless helpers
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections =
                room => GetAvailableDirections(room, grid, (x, y) => x >= 0 && y >= 0 && x < width && y < height);

            Action<Room> createBranchPathFrom = room =>
                CreateBranchPathFrom(room, grid, rooms, random, getAvailableDirections);

            Action<Room> tryCreateLoop = room =>
                TryCreateLoop(room, grid, random, (x, y) => x >= 0 && y >= 0 && x < width && y < height);

            // Generate the main path
            while (roomsPlaced < targetPathLength)
            {
                if (roomStack.Count == 0)
                {
                    logger.Log("Warning: Could not reach the desired path length. Dungeon may be smaller than expected.");
                    break;
                }

                Room currentRoom = roomStack.Peek();
                var availableDirections = getAvailableDirections(currentRoom);

                if (availableDirections.Count == 0)
                {
                    roomStack.Pop(); // Dead end, backtrack
                    continue;
                }

                var chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = currentRoom.X + chosen.dx;
                int newY = currentRoom.Y + chosen.dy;

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
                exitRoom.Type = RoomType.Exit;
                logger.Log($"Dungeon exit created at ({exitRoom.X}, {exitRoom.Y}).");
            }

            // Add side branches
            logger.Log($"Adding side branches...");
            int extraBranches = 30;
            for (int i = 0; i < extraBranches; i++)
            {
                CreateBranchPath(
                    grid,
                    rooms,
                    random,
                    logger.Log,
                    getAvailableDirections,
                    createBranchPathFrom,
                    tryCreateLoop
                );
            }

            // Generate a description for the entrance room using the LLM client
            Room.GenerateRoomDescriptionsAsync(
                new List<Room> { entrance },
                theme,
                llmClient,
                logger
            ).GetAwaiter().GetResult();

            return dungeon;
        }

        /// <summary>
        /// Creates a side branch of rooms starting from a random room in the dungeon.
        /// </summary>
        private static void CreateBranchPath(
            Room[,] grid,
            List<Room> rooms,
            Random random,
            Action<string> log,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections,
            Action<Room> createBranchPathFrom,
            Action<Room> tryCreateLoop)
        {
            if (rooms.Count == 0) return;

            Room fromRoom = rooms[random.Next(rooms.Count)];
            int branchLength = random.Next(2, 6); // Branch of 2–5 rooms
            Room current = fromRoom;

            for (int i = 0; i < branchLength; i++)
            {
                var availableDirections = getAvailableDirections(current);

                if (availableDirections.Count == 0)
                {
                    break; // Dead end
                }

                var chosen = availableDirections[random.Next(availableDirections.Count)];
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
                    createBranchPathFrom(current);
                }

                // 30% chance to loop at the end of branch
                if (i == branchLength - 1 && random.NextDouble() < 0.3)
                {
                    tryCreateLoop(current);
                }
            }
        }

        /// <summary>
        /// Creates a small sub-branch of rooms starting from the specified room.
        /// </summary>
        /// <param name="startRoom">The room to start the sub-branch from.</param>
        private static void CreateBranchPathFrom(
            Room startRoom,
            Room[,] grid,
            List<Room> rooms,
            Random random,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections)
        {
            int branchLength = random.Next(1, 4); // Small sub-branch (1–3 rooms)
            Room current = startRoom;

            for (int i = 0; i < branchLength; i++)
            {
                var availableDirections = getAvailableDirections(current);

                if (availableDirections.Count == 0)
                    break;

                var chosen = availableDirections[random.Next(availableDirections.Count)];
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
        private static void TryCreateLoop(
            Room room,
            Room[,] grid,
            Random random,
            Func<int, int, bool> isInBounds)
        {
            var directions = new List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>();

            if (isInBounds(room.X, room.Y - 1) && grid[room.X, room.Y - 1] != null)
                directions.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            if (isInBounds(room.X + 1, room.Y) && grid[room.X + 1, room.Y] != null)
                directions.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            if (isInBounds(room.X, room.Y + 1) && grid[room.X, room.Y + 1] != null)
                directions.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            if (isInBounds(room.X - 1, room.Y) && grid[room.X - 1, room.Y] != null)
                directions.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));

            if (directions.Count > 0)
            {
                var chosen = directions[random.Next(directions.Count)];
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
        /// <param name="grid">The grid of rooms.</param>
        /// <param name="isInBounds">Function to check if a position is within bounds.</param>
        /// <returns>A list of available directions and actions to set connections.</returns>
        private static List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> GetAvailableDirections(
            Room fromRoom,
            Room[,] grid,
            Func<int, int, bool> isInBounds)
        {
            var available = new List<(int dx, int dy, Action<Room>, Action<Room>)>();

            if (isInBounds(fromRoom.X, fromRoom.Y - 1) && grid[fromRoom.X, fromRoom.Y - 1] == null)
                available.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            if (isInBounds(fromRoom.X + 1, fromRoom.Y) && grid[fromRoom.X + 1, fromRoom.Y] == null)
                available.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            if (isInBounds(fromRoom.X, fromRoom.Y + 1) && grid[fromRoom.X, fromRoom.Y + 1] == null)
                available.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            if (isInBounds(fromRoom.X - 1, fromRoom.Y) && grid[fromRoom.X - 1, fromRoom.Y] == null)
                available.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));

            return available;
        }

        /// <summary>
        /// Populates the rooms with contents such as treasure chests and enemies.
        /// </summary>
        public static async Task PopulateRoomContentsAsync(
            List<Room> rooms,
            string theme,
            ILLMClient llmClient,
            ILogger logger,
            Random random)
        {
            // Generate a list of enemy types based on the dungeon theme
            var enemyTypes = await EnemyFactory.GenerateEnemyTypesAsync(theme, llmClient, logger);

            foreach (Room room in rooms)
            {
                // Only populate normal rooms (not entrance/exit)
                if (room.Type == RoomType.Normal)
                {
                    double roll = random.NextDouble();

                    // 10% chance to add a treasure chest (possibly locked)
                    if (roll < 0.1)
                    {
                        bool isLocked = random.NextDouble() < 0.3; // 30% of chests are locked
                        room.Contents.Add(TreasureChestFactory.CreateTreasureChest(isLocked: isLocked));

                        logger.Log($"Treasure chest added to room at ({room.X}, {room.Y}) -  {(isLocked ? "Locked" : "Unlocked")}.");
                    }
                    // Next 10% chance (i.e., 10% to 20%) to add an enemy
                    else if (roll < 0.2)
                    {
                        // Pick a random enemy type from the master list
                        var enemyType = enemyTypes[random.Next(enemyTypes.Count)];
                        var enemy = EnemyFactory.CreateEnemy(enemyType.Name, enemyType.Description, theme);
                        room.Contents.Add(enemy);

                        logger.Log($"Enemy '{enemy.Name}' added to room at ({room.X}, {room.Y}).");
                    }
                }
            }
        }
    }
}