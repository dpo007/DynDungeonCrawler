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

        // Remove an Entity by its ID
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

        // Remove by Entity object
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null) return false;
            return Contents.Remove(entity);
        }

        public override string ToString()
        {
            return $"{Type} Room [{Id}] at ({X}, {Y})";
        }
    }
}