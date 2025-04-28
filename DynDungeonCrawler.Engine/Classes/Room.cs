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
        public Guid Id { get; private set; } // Unique ID for external linking
        public int X { get; set; }            // Grid X coordinate
        public int Y { get; set; }            // Grid Y coordinate
        public RoomType Type { get; set; }    // Type of room
        public string Description { get; set; } = string.Empty;
        public List<Entity> Contents { get; set; } = new List<Entity>();

        // Room connection flags
        // These can be used to determine if a room is connected to another room in a specific direction
        public bool ConnectedNorth { get; set; }
        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }

        // Optimized constructors
        public Room(int x, int y, RoomType type = RoomType.Normal)
        {
            Id = Guid.NewGuid(); // Automatically assign unique ID
            X = x;
            Y = y;
            Type = type;
        }

        // Constructor without RoomType (defaults to Normal)
        public Room(int x, int y)
        {
            Id = Guid.NewGuid(); // Automatically assign unique ID
            X = x;
            Y = y;
            Type = RoomType.Normal; // Default to Normal
        }

        /// <summary>
        /// Removes an Entity from the room by its ID.
        /// </summary>
        /// <param name="entityId">The unique identifier of the Entity to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntityById(Guid entityId)
        {
            var entity = Contents.FirstOrDefault(e => e.Id == entityId);
            if (entity != null)
            {
                Contents.Remove(entity);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an Entity from the room by its object reference.
        /// </summary>
        /// <param name="entity">The Entity object to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null) return false;
            return Contents.Remove(entity);
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