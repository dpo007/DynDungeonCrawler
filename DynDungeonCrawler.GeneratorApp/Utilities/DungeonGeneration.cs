using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.GeneratorApp.Utilities
{
    public static class DungeonGeneration
    {
        /// <summary>
        /// Asynchronously generates a new dungeon with a main path, side branches, and AI-generated room descriptions.
        /// </summary>
        /// <param name="width">The width of the dungeon grid.</param>
        /// <param name="height">The height of the dungeon grid.</param>
        /// <param name="theme">The theme for styling and description generation.</param>
        /// <param name="llmClient">An AI client used to generate room descriptions.</param>
        /// <param name="logger">Logger instance for diagnostic and progress messages.</param>
        /// <returns>
        /// A Task that, when completed, returns the fully generated Dungeon instance.
        /// </returns>
        public static async Task<Dungeon> GenerateDungeon(
            int width,
            int height,
            string theme,
            ILLMClient llmClient,
            ILogger logger)
        {
            // Create the dungeon instance and initialize the grid
            Dungeon dungeon = new Dungeon(width, height, theme, llmClient, logger);
            int startX = width / 2;
            int startY = height / 2;

            // Create the entrance room at the center
            Room entrance = new Room(startX, startY) { Type = RoomType.Entrance };
            Room[,] grid = dungeon.Grid;
            grid[startX, startY] = entrance;
            dungeon.AddRoom(entrance);

            // Stack for backtracking during main path generation
            Stack<Room> roomStack = new Stack<Room>();
            roomStack.Push(entrance);
            logger.Log($"Dungeon entrance created at ({startX}, {startY}).");

            // Determine minimum and target path lengths
            int minPathLength = DungeonDefaults.DefaultEscapePathLength;
            int roomsPlaced = 1;
            Random random = Random.Shared;
            int targetPathLength = random.Next(minPathLength, minPathLength + 10);
            logger.Log($"Creating main path with target length: {targetPathLength} rooms.");

            // Helper functions for direction and branching
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections =
                room => GetAvailableDirections(room, grid, (x, y) => x >= 0 && y >= 0 && x < width && y < height);
            Action<Room> createBranchPathFrom = room =>
                CreateBranchPathFrom(room, grid, dungeon, random, getAvailableDirections);
            Action<Room> tryCreateLoop = room =>
                TryCreateLoop(room, grid, random, (x, y) => x >= 0 && y >= 0 && x < width && y < height);

            // Main path generation loop
            while (roomsPlaced < targetPathLength)
            {
                if (roomStack.Count == 0)
                {
                    logger.Log("Warning: Could not reach the desired path length. Dungeon may be smaller than expected.");
                    break;
                }

                Room currentRoom = roomStack.Peek();
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = getAvailableDirections(currentRoom);

                if (availableDirections.Count == 0)
                {
                    // Dead end, backtrack
                    roomStack.Pop();
                    continue;
                }

                // Choose a random available direction and create a new room
                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = currentRoom.X + chosen.dx;
                int newY = currentRoom.Y + chosen.dy;
                Room newRoom = new Room(newX, newY);
                chosen.setExitFrom(currentRoom);
                chosen.setExitTo(newRoom);
                grid[newX, newY] = newRoom;
                dungeon.AddRoom(newRoom);
                roomStack.Push(newRoom);
                roomsPlaced++;
            }

            // Mark the last room on the main path as the exit
            if (roomStack.Count > 0)
            {
                Room exitRoom = roomStack.Peek();
                exitRoom.Type = RoomType.Exit;
                logger.Log($"Dungeon exit created at ({exitRoom.X}, {exitRoom.Y}).");
            }

            logger.Log("Adding side branches...");

            // Add extra branches to the dungeon for complexity
            int extraBranches = 30;
            for (int i = 0; i < extraBranches; i++)
            {
                CreateBranchPath(
                    grid,
                    dungeon,
                    random,
                    logger.Log,
                    getAvailableDirections,
                    createBranchPathFrom,
                    tryCreateLoop
                );
            }

            // Generate a description for the entrance room (async)
            await Room.GenerateRoomDescriptionsAsync(
                new List<Room> { entrance },
                theme,
                llmClient,
                logger
            );

            // Return the generated dungeon
            return dungeon;
        }

        /// <summary>
        /// Creates a primary branching path in the dungeon.
        /// </summary>
        /// <param name="grid">2D grid representing the dungeon layout.</param>
        /// <param name="dungeon">The Dungeon instance holding all rooms.</param>
        /// <param name="random">Random generator for decision making.</param>
        /// <param name="log">Action for logging messages (unused in current logic).</param>
        /// <param name="getAvailableDirections">
        /// Function that, given a room, returns a list of possible directions
        /// along with the corresponding exit setters.
        /// </param>
        /// <param name="createBranchPathFrom">
        /// Action to recursively create a sub-branch from the current room
        /// with a certain probability.
        /// </param>
        /// <param name="tryCreateLoop">
        /// Action to attempt creating a loop back into an existing path
        /// at the end of the branch with a certain probability.
        /// </param>
        private static void CreateBranchPath(
            Room[,] grid,
            Dungeon dungeon,
            Random random,
            Action<string> log,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections,
            Action<Room> createBranchPathFrom,
            Action<Room> tryCreateLoop)
        {
            // Grab the list of existing rooms
            IReadOnlyList<Room> rooms = dungeon.Rooms;

            // If there are no rooms, nothing to branch from
            if (rooms.Count == 0)
            {
                return;
            }

            // Pick a random starting room
            Room fromRoom = rooms[random.Next(rooms.Count)];

            // Decide how long this branch will be
            int branchLength = random.Next(2, 6);

            // Begin at the chosen starting room
            Room current = fromRoom;

            // Carve the branch step by step
            for (int i = 0; i < branchLength; i++)
            {
                // Find all possible directions we can extend into
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = getAvailableDirections(current);
                if (availableDirections.Count == 0)
                {
                    // Dead end reached
                    break;
                }

                // Choose one direction at random
                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];

                // Calculate the new room’s coordinates
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;

                // Create and wire up the new room
                Room newRoom = new Room(newX, newY);
                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);

                // Place the new room into the grid and dungeon
                grid[newX, newY] = newRoom;
                dungeon.AddRoom(newRoom);

                // Advance the “current” pointer
                current = newRoom;

                // Occasionally spawn a side-branch
                if (random.NextDouble() < 0.2)
                {
                    createBranchPathFrom(current);
                }

                // At the end of the branch, maybe loop back somewhere
                if (i == branchLength - 1 && random.NextDouble() < 0.3)
                {
                    tryCreateLoop(current);
                }
            }
        }

        /// <summary>
        /// Creates a sub-branch path starting from the specified room.
        /// </summary>
        /// <param name="startRoom">The room from which to start the sub-branch.</param>
        /// <param name="grid">2D grid representing the dungeon layout.</param>
        /// <param name="dungeon">The Dungeon instance holding all rooms.</param>
        /// <param name="random">Random generator for decision making.</param>
        /// <param name="getAvailableDirections">
        /// Function that, given a room, returns a list of possible directions
        /// along with the corresponding exit setters.
        /// </param>
        private static void CreateBranchPathFrom(
            Room startRoom,
            Room[,] grid,
            Dungeon dungeon,
            Random random,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections)
        {
            // Determine the length of this sub-branch
            int branchLength = random.Next(1, 4);

            // Start from the provided room
            Room current = startRoom;

            // Carve out rooms along the branch
            for (int i = 0; i < branchLength; i++)
            {
                // Look for directions we can go
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = getAvailableDirections(current);
                if (availableDirections.Count == 0)
                {
                    // No further extension possible
                    break;
                }

                // Pick a random direction
                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];

                // Compute new coordinates
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;

                // Instantiate the new room and connect exits
                Room newRoom = new Room(newX, newY);
                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);

                // Add the room to both grid and dungeon
                grid[newX, newY] = newRoom;
                dungeon.AddRoom(newRoom);

                // Move forward
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
            List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> directions = new List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>();

            if (isInBounds(room.X, room.Y - 1) && grid[room.X, room.Y - 1] != null)
            {
                directions.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            }

            if (isInBounds(room.X + 1, room.Y) && grid[room.X + 1, room.Y] != null)
            {
                directions.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            }

            if (isInBounds(room.X, room.Y + 1) && grid[room.X, room.Y + 1] != null)
            {
                directions.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            }

            if (isInBounds(room.X - 1, room.Y) && grid[room.X - 1, room.Y] != null)
            {
                directions.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));
            }

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
        /// <param name="grid">The grid of rooms.</param>
        /// <param name="isInBounds">Function to check if a position is within bounds.</param>
        /// <returns>A list of available directions and actions to set connections.</returns>
        private static List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> GetAvailableDirections(
            Room fromRoom,
            Room[,] grid,
            Func<int, int, bool> isInBounds)
        {
            List<(int dx, int dy, Action<Room>, Action<Room>)> available = new List<(int dx, int dy, Action<Room>, Action<Room>)>();

            if (isInBounds(fromRoom.X, fromRoom.Y - 1) && grid[fromRoom.X, fromRoom.Y - 1] == null)
            {
                available.Add((0, -1, r => r.ConnectedNorth = true, r => r.ConnectedSouth = true));
            }

            if (isInBounds(fromRoom.X + 1, fromRoom.Y) && grid[fromRoom.X + 1, fromRoom.Y] == null)
            {
                available.Add((1, 0, r => r.ConnectedEast = true, r => r.ConnectedWest = true));
            }

            if (isInBounds(fromRoom.X, fromRoom.Y + 1) && grid[fromRoom.X, fromRoom.Y + 1] == null)
            {
                available.Add((0, 1, r => r.ConnectedSouth = true, r => r.ConnectedNorth = true));
            }

            if (isInBounds(fromRoom.X - 1, fromRoom.Y) && grid[fromRoom.X - 1, fromRoom.Y] == null)
            {
                available.Add((-1, 0, r => r.ConnectedWest = true, r => r.ConnectedEast = true));
            }

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
            List<EnemyTypeInfo> enemyTypes = await EnemyFactory.GenerateEnemyTypesAsync(theme, llmClient, logger);

            // Use a thread-local Random instance to avoid contention in parallel processing
            ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

            Parallel.ForEach(rooms, room =>
            {
                Random localRandom = threadLocalRandom.Value!;

                // Only populate normal rooms (not entrance/exit)
                if (room.Type == RoomType.Normal)
                {
                    double roll = localRandom.NextDouble();

                    // 10% chance to add a treasure chest (possibly locked)
                    if (roll < 0.1)
                    {
                        bool isLocked = localRandom.NextDouble() < 0.3; // 30% of chests are locked
                        room.AddEntity(TreasureChestFactory.CreateTreasureChest(isLocked: isLocked));

                        logger.Log($"Treasure chest added to room at ({room.X}, {room.Y}) -  {(isLocked ? "Locked" : "Unlocked")}.");
                    }
                    // Next 10% chance (i.e., 10% to 20%) to add an enemy
                    else if (roll < 0.2)
                    {
                        // Pick a random enemy type from the master list
                        EnemyTypeInfo enemyType = enemyTypes[localRandom.Next(enemyTypes.Count)];
                        Enemy enemy = EnemyFactory.CreateEnemy(enemyType.Name, enemyType.Description, enemyType.ShortDescription, theme);
                        room.AddEntity(enemy);

                        logger.Log($"Enemy '{enemy.Name}' added to room at ({room.X}, {room.Y}).");
                    }
                }
            });
        }
    }
}