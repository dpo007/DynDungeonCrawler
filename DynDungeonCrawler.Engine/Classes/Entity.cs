namespace DynDungeonCrawler.Classes
{
    public enum EntityType
    {
        Enemy,
        TreasureChest,
        Key,
        NPC,
        Trap
        // etc. Easily expandable later
    }

    public abstract class Entity
    {
        public Guid Id { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public EntityType Type { get; set; }

        public Entity(EntityType type, string name)
        {
            Id = Guid.NewGuid();
            Type = type;
            Name = name;
        }
    }
}