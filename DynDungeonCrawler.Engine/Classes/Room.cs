namespace DynDungeonCrawler.Engine.Classes
{
    public enum RoomType
    {
        Normal,
        Entrance,
        Exit
    }

    public class Room
    {
        public Guid Id { get; internal set; } // Unique ID for external linking
        public int X { get; set; }            // Grid X coordinate
        public int Y { get; set; }            // Grid Y coordinate
        public RoomType Type { get; set; }    // Type of room
        public string Name { get; set; } = string.Empty; // LLM-generated room name
        public string Description { get; set; } = string.Empty;
        private readonly object _contentsLock = new();
        public List<Entity> Contents { get; } = new();

        // Room connection flags
        // These can be used to determine if a room is connected to another room in a specific direction
        public bool ConnectedNorth { get; set; }

        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }

        // Constructor with RoomType
        public Room(int x, int y, RoomType type)
        {
            // Validate coordinates
            if (x < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "X must be ≥ 0.");
            }

            if (y < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Y must be ≥ 0.");
            }

            // Validate RoomType
            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Invalid RoomType value.");
            }

            Id = Guid.NewGuid();
            X = x;
            Y = y;
            Type = type;
        }

        // Constructor without RoomType (defaults to Normal)
        public Room(int x, int y) : this(x, y, RoomType.Normal)
        {
        }

        /// <summary>
        /// Adds an Entity to the room, enforcing validation rules.
        /// Automatically updates treasure chest guarded status after adding the entity.
        /// </summary>
        /// <param name="entity">The Entity to add to the room.</param>
        /// <exception cref="ArgumentNullException">Thrown if the entity is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an entity with the same ID already exists in the room.</exception>
        public void AddEntity(Entity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            lock (_contentsLock)
            {
                if (Contents.Any(e => e.Id == entity.Id))
                {
                    throw new InvalidOperationException("Entity with the same ID already exists in the room.");
                }

                Contents.Add(entity);

                // Update treasure chest guarded status after adding entity
                UpdateTreasureChestGuardedStatus();
            }
        }

        /// <summary>
        /// Removes an Entity from the room by its ID.
        /// Automatically updates treasure chest guarded status after removing the entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the Entity to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntityById(Guid entityId)
        {
            lock (_contentsLock)
            {
                Entity? entity = Contents.FirstOrDefault(e => e.Id == entityId);
                if (entity != null)
                {
                    bool removed = Contents.Remove(entity);
                    if (removed)
                    {
                        // Update treasure chest guarded status after removing entity
                        UpdateTreasureChestGuardedStatus();
                    }
                    return removed;
                }
                return false;
            }
        }

        /// <summary>
        /// Removes an Entity from the room by its object reference.
        /// Automatically updates treasure chest guarded status after removing the entity.
        /// </summary>
        /// <param name="entity">The Entity object to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null)
            {
                return false;
            }

            lock (_contentsLock)
            {
                bool removed = Contents.Remove(entity);
                if (removed)
                {
                    // Update treasure chest guarded status after removing entity
                    UpdateTreasureChestGuardedStatus();
                }
                return removed;
            }
        }

        /// <summary>
        /// Updates the guarded status of all treasure chests in the room based on the presence of enemies.
        /// Should be called whenever entities are added or removed from the room.
        /// </summary>
        private void UpdateTreasureChestGuardedStatus()
        {
            // Check if there are any enemies in the room
            bool hasEnemies = Contents.Any(entity => entity.Type == EntityType.Enemy);

            // Update all treasure chests in the room
            List<TreasureChest> treasureChests = Contents.OfType<TreasureChest>().ToList();
            foreach (TreasureChest chest in treasureChests)
            {
                chest.UpdateGuardedStatus(hasEnemies);
            }
        }

        /// <summary>
        /// Gets all treasure chests in the room that are currently being guarded by enemies.
        /// </summary>
        /// <returns>A list of guarded treasure chests.</returns>
        public List<TreasureChest> GetGuardedTreasureChests()
        {
            lock (_contentsLock)
            {
                return Contents.OfType<TreasureChest>()
                              .Where(chest => chest.IsGuarded)
                              .ToList();
            }
        }

        /// <summary>
        /// Gets all enemies currently in the room.
        /// </summary>
        /// <returns>A list of enemies in the room.</returns>
        public List<Enemy> GetEnemies()
        {
            lock (_contentsLock)
            {
                return Contents.OfType<Enemy>().ToList();
            }
        }

        /// <summary>
        /// Checks if the room has any living enemies present.
        /// </summary>
        /// <returns>True if enemies are present; otherwise, false.</returns>
        public bool HasEnemies()
        {
            lock (_contentsLock)
            {
                return Contents.Any(entity => entity.Type == EntityType.Enemy);
            }
        }

        /// <summary>
        /// Returns a list of rooms that are directly accessible from this room.
        /// A room is considered directly accessible if it is adjacent in the grid
        /// and there is an exit from this room to that room (i.e., the corresponding
        /// Connected* property is true).
        /// </summary>
        /// <param name="grid">The 2D array of rooms representing the dungeon layout.</param>
        /// <returns>A list of directly accessible neighbouring rooms.</returns>
        public List<Room> GetAccessibleNeighbours(Room[,] grid)
        {
            List<Room> neighbours = new List<Room>();
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            // North
            if (ConnectedNorth && Y > 0)
            {
                Room north = grid[X, Y - 1];
                if (north != null)
                {
                    neighbours.Add(north);
                }
            }
            // East
            if (ConnectedEast && X < width - 1)
            {
                Room east = grid[X + 1, Y];
                if (east != null)
                {
                    neighbours.Add(east);
                }
            }
            // South
            if (ConnectedSouth && Y < height - 1)
            {
                Room south = grid[X, Y + 1];
                if (south != null)
                {
                    neighbours.Add(south);
                }
            }
            // West
            if (ConnectedWest && X > 0)
            {
                Room west = grid[X - 1, Y];
                if (west != null)
                {
                    neighbours.Add(west);
                }
            }

            return neighbours;
        }

        public Room? GetNeighbour(RoomDirection direction, Room[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            return direction switch
            {
                RoomDirection.North when ConnectedNorth && Y > 0 => grid[X, Y - 1],
                RoomDirection.East when ConnectedEast && X < width - 1 => grid[X + 1, Y],
                RoomDirection.South when ConnectedSouth && Y < height - 1 => grid[X, Y + 1],
                RoomDirection.West when ConnectedWest && X > 0 => grid[X - 1, Y],
                _ => null
            };
        }

        /// <summary>
        /// Returns a string representation of the Room.
        /// </summary>
        /// <returns>A string describing the Room's type, ID, and coordinates.</returns>
        public override string ToString()
        {
            return $"{Type} Room [{Id}] at ({X}, {Y})";
        }
    }
}