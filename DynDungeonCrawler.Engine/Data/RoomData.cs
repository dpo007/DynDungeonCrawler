using System.Text.Json.Serialization;

namespace DynDungeonCrawler.Engine.Data
{
    public class RoomData
    {
        public Guid Id { get; set; }           // Unique ID for cross-system linking
        public int X { get; set; }              // Grid X coordinate
        public int Y { get; set; }              // Grid Y coordinate
        public string Type { get; set; } = "";  // Room type as string for serialization
        public string Description { get; set; } = ""; // Optional description

        /// <summary>
        /// List of entities in the room. Used for serialization/deserialization only.
        /// Do not use for runtime logic; use Room.Contents in the main model.
        /// </summary>
        public List<EntityData> Contents { get; set; } = new();

        public void AddEntity(EntityData entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            if (Contents.Any(e => e.Id == entity.Id))
                throw new InvalidOperationException("Entity with the same ID already exists in the room.");
            Contents.Add(entity);
        }

        public bool ConnectedNorth { get; set; }
        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }
    }
}