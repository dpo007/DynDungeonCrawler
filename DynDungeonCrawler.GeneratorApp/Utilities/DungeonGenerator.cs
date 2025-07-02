using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.GeneratorApp.Utilities
{
    public static class DungeonGenerator
    {
        public static async Task<Dungeon> GenerateDungeon(
            int width,
            int height,
            string theme,
            ILLMClient llmClient,
            ILogger logger)
        {
            Dungeon dungeon = new Dungeon(width, height, theme, llmClient, logger);
            int startX = width / 2;
            int startY = height / 2;
            Room entrance = new Room(startX, startY) { Type = RoomType.Entrance };
            Room[,] grid = dungeon.Grid;
            grid[startX, startY] = entrance;
            dungeon.AddRoom(entrance);
            Stack<Room> roomStack = new Stack<Room>();
            roomStack.Push(entrance);
            logger.Log($"Dungeon entrance created at ({startX}, {startY}).");
            int minPathLength = DungeonDefaults.DefaultEscapePathLength;
            int roomsPlaced = 1;
            Random random = Random.Shared;
            int targetPathLength = random.Next(minPathLength, minPathLength + 10);
            logger.Log($"Creating main path with target length: {targetPathLength} rooms.");
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections =
                room => GetAvailableDirections(room, grid, (x, y) => x >= 0 && y >= 0 && x < width && y < height);
            Action<Room> createBranchPathFrom = room =>
                CreateBranchPathFrom(room, grid, dungeon, random, getAvailableDirections);
            Action<Room> tryCreateLoop = room =>
                TryCreateLoop(room, grid, random, (x, y) => x >= 0 && y >= 0 && x < width && y < height);
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
                    roomStack.Pop();
                    continue;
                }
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
            if (roomStack.Count > 0)
            {
                Room exitRoom = roomStack.Peek();
                exitRoom.Type = RoomType.Exit;
                logger.Log($"Dungeon exit created at ({exitRoom.X}, {exitRoom.Y}).");
            }
            logger.Log($"Adding side branches...");
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
            await Room.GenerateRoomDescriptionsAsync(
                new List<Room> { entrance },
                theme,
                llmClient,
                logger
            );
            return dungeon;
        }

        private static void CreateBranchPath(
            Room[,] grid,
            Dungeon dungeon,
            Random random,
            Action<string> log,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections,
            Action<Room> createBranchPathFrom,
            Action<Room> tryCreateLoop)
        {
            IReadOnlyList<Room> rooms = dungeon.Rooms;
            if (rooms.Count == 0)
            {
                return;
            }

            Room fromRoom = rooms[random.Next(rooms.Count)];
            int branchLength = random.Next(2, 6);
            Room current = fromRoom;
            for (int i = 0; i < branchLength; i++)
            {
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = getAvailableDirections(current);
                if (availableDirections.Count == 0)
                {
                    break;
                }
                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;
                Room newRoom = new Room(newX, newY);
                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);
                grid[newX, newY] = newRoom;
                dungeon.AddRoom(newRoom);
                current = newRoom;
                if (random.NextDouble() < 0.2)
                {
                    createBranchPathFrom(current);
                }
                if (i == branchLength - 1 && random.NextDouble() < 0.3)
                {
                    tryCreateLoop(current);
                }
            }
        }

        private static void CreateBranchPathFrom(
            Room startRoom,
            Room[,] grid,
            Dungeon dungeon,
            Random random,
            Func<Room, List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)>> getAvailableDirections)
        {
            int branchLength = random.Next(1, 4);
            Room current = startRoom;
            for (int i = 0; i < branchLength; i++)
            {
                List<(int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo)> availableDirections = getAvailableDirections(current);
                if (availableDirections.Count == 0)
                {
                    break;
                }

                (int dx, int dy, Action<Room> setExitFrom, Action<Room> setExitTo) chosen = availableDirections[random.Next(availableDirections.Count)];
                int newX = current.X + chosen.dx;
                int newY = current.Y + chosen.dy;
                Room newRoom = new Room(newX, newY);
                chosen.setExitFrom(current);
                chosen.setExitTo(newRoom);
                grid[newX, newY] = newRoom;
                dungeon.AddRoom(newRoom);
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