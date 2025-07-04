using DynDungeonCrawler.Engine.Data;

namespace DynDungeonCrawler.Engine.Classes
{
    /// <summary>
    /// Factory class for creating Entity instances from EntityData.
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// Creates an Entity instance from the given EntityData.
        /// </summary>
        /// <param name="entityData">The EntityData to create the Entity from.</param>
        /// <returns>An Entity instance or null if the EntityData is invalid.</returns>
        public static Entity? FromEntityData(EntityData entityData)
        {
            if (entityData == null)
            {
                return null;
            }

            // Create the correct derived type based on entityData.Type
            switch (entityData.Type)
            {
                case EntityType.TreasureChest:
                    TreasureChest chest;

                    // If we have treasure data, use the constructor that sets it directly
                    if (entityData.TreasureType.HasValue && entityData.TreasureValue.HasValue)
                    {
                        chest = new TreasureChest(
                            entityData.Name ?? "Treasure Chest",
                            entityData.IsLocked ?? false,
                            entityData.TreasureType.Value,
                            entityData.TreasureValue.Value
                        );
                    }
                    else
                    {
                        // Otherwise use the standard constructor
                        chest = new TreasureChest(
                            entityData.Name ?? "Treasure Chest",
                            entityData.IsLocked ?? false
                        );
                    }

                    // Set the common entity properties
                    chest.Id = entityData.Id;
                    chest.Description = entityData.Description ?? "";
                    chest.ShortDescription = entityData.ShortDescription ?? "";

                    // Set IsOpened if provided
                    if (entityData.IsOpened.HasValue)
                    {
                        chest.IsOpened = entityData.IsOpened.Value;
                    }

                    return chest;

                case EntityType.Enemy:
                    int health = entityData.Health ?? 10;
                    int strength = entityData.Strength ?? 2;

                    // Use constructor that allows setting MoneyReward directly
                    Enemy enemy = new Enemy(
                        entityData.Name ?? "Monster",
                        entityData.Description ?? "",
                        entityData.ShortDescription ?? "",
                        health,
                        strength,
                        entityData.MoneyReward
                    );

                    // Set the ID from the data
                    enemy.Id = entityData.Id;

                    return enemy;

                // Handle other entity types (Key, NPC, Trap) here as they're implemented

                default:
                    // For any other or unknown types, use the base Entity class
                    return new Entity(entityData.Type, entityData.Name ?? "Unknown")
                    {
                        Id = entityData.Id,
                        Description = entityData.Description ?? "",
                        ShortDescription = entityData.ShortDescription ?? ""
                    };
            }
        }
    }
}