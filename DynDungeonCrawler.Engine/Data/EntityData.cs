using DynDungeonCrawler.Engine.Classes;

namespace DynDungeonCrawler.Engine.Data
{
    public class EntityData
    {
        public Guid Id { get; set; }
        public EntityType Type { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";

        // TreasureChest specific properties
        public bool? IsLocked { get; set; }

        public bool? IsOpened { get; set; }
        public TreasureType? TreasureType { get; set; }
        public int? TreasureValue { get; set; }

        // Enemy and Adventurer shared properties
        public int? Health { get; set; }

        public int? Strength { get; set; }

        // Adventurer specific properties
        public int? Defense { get; set; }

        public int? Wealth { get; set; }

        // Enemy specific properties
        public int MoneyReward { get; set; } = 0;

        /// <summary>
        /// Converts the EntityData to the appropriate Entity type based on the Type property.
        /// </summary>
        /// <returns>An Entity object representing this data.</returns>
        public Entity ToEntity()
        {
            Entity entity;

            switch (Type)
            {
                case EntityType.Enemy:
                    entity = new Enemy(
                        Name,
                        Description ?? "A fearsome creature",
                        ShortDescription ?? "",
                        Health ?? 10,
                        Strength ?? 2,
                        MoneyReward);
                    break;

                case EntityType.TreasureChest:
                    entity = new TreasureChest(Name, IsLocked ?? false);

                    // Handle opened state
                    if (IsOpened == true)
                    {
                        ((TreasureChest)entity).Open();
                    }
                    break;

                case EntityType.MagicalLockPick:
                    entity = new MagicalLockPick(Name);
                    break;

                case EntityType.Adventurer:
                    entity = new Adventurer(Name);
                    if (Health.HasValue)
                    {
                        ((Adventurer)entity).Health = Health.Value;
                    }
                    if (Strength.HasValue)
                    {
                        ((Adventurer)entity).Strength = Strength.Value;
                    }
                    if (Defense.HasValue)
                    {
                        ((Adventurer)entity).Defense = Defense.Value;
                    }
                    if (Wealth.HasValue && Wealth.Value > 0)
                    {
                        ((Adventurer)entity).AddWealth(Wealth.Value);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Entity type {Type} is not supported.");
            }

            // Set the ID and descriptions
            entity.Id = Id;

            if (!string.IsNullOrWhiteSpace(Description))
            {
                entity.Description = Description;
            }

            if (!string.IsNullOrWhiteSpace(ShortDescription))
            {
                entity.ShortDescription = ShortDescription;
            }

            return entity;
        }
    }
}