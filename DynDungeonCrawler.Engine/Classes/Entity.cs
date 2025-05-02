using DynDungeonCrawler.Engine.Data;

namespace DynDungeonCrawler.Engine.Classes
{
    public enum EntityType
    {
        Enemy,
        TreasureChest,
        Key,
        NPC,
        Trap,
        Adventurer // Aka Players
    }

    public class Entity
    {
        public Guid Id { get; set; }
        public EntityType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = "";

        public Entity(EntityType type, string name)
        {
            Id = Guid.NewGuid();
            Type = type;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Entity name cannot be empty.", nameof(name));
            Name = name.Trim();
        }

        /// <summary>
        /// Converts the entity to a data object for serialization.
        /// </summary>
        /// <returns>An EntityData object representing the entity.</returns>
        public virtual EntityData ToEntityData()
        {
            var data = new EntityData
            {
                Id = this.Id,
                Type = this.Type,
                Name = this.Name,
                Description = this.Description
            };

            if (this is TreasureChest chest)
            {
                data.IsLocked = chest.IsLocked;
                data.IsOpened = chest.IsOpened;
                data.TreasureType = chest.ContainedTreasure?.Type;
                data.TreasureValue = chest.ContainedTreasure?.Value;
            }
            else if (this is Enemy enemy)
            {
                data.MoneyReward = enemy.MoneyReward;
            }

            return data;
        }
    }
}
