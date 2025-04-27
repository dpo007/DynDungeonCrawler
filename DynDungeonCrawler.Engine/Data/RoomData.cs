namespace DynDungeonCrawler.Data
{
    public class RoomData
    {
        public Guid Id { get; set; }           // Unique ID for cross-system linking
        public int X { get; set; }              // Grid X coordinate
        public int Y { get; set; }              // Grid Y coordinate
        public string Type { get; set; } = "";  // Room type as string for serialization
        public string Description { get; set; } = ""; // Optional description
        public List<EntityData> Contents { get; set; } = new List<EntityData>();

        public bool ConnectedNorth { get; set; }
        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }
    }
}