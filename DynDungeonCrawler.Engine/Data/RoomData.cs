namespace DynDungeonCrawler.Engine.Data
{
    public class RoomData
    {
        public Guid Id { get; set; }           // Unique ID for cross-system linking
        public int X { get; set; }              // Grid X coordinate
        public int Y { get; set; }              // Grid Y coordinate
        public string Type { get; set; } = "";  // Room type as string for serialization
        public string Description { get; set; } = ""; // Optional description

        private readonly List<EntityData> _contents = new List<EntityData>();
        public IReadOnlyList<EntityData> Contents => _contents;

        public void AddEntity(EntityData entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            if (_contents.Any(e => e.Id == entity.Id))
                throw new InvalidOperationException("Entity with the same ID already exists in the room.");
            _contents.Add(entity);
        }

        public bool ConnectedNorth { get; set; }
        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }
    }
}