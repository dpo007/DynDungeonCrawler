using DynDungeonCrawler.Engine.Classes;

namespace DynDungeonCrawler.Engine.Data
{
    public class EntityData
    {
        public Guid Id { get; set; }
        public EntityType Type { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        // TreasureChest specific properties
        public bool? IsLocked { get; set; }
        public bool? IsOpened { get; set; }
        public TreasureType? TreasureType { get; set; }
        public int? TreasureValue { get; set; }
    }
}