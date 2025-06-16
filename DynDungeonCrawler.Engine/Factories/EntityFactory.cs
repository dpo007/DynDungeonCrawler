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
                return null;

            return new Entity(entityData.Type, entityData.Name)
            {
                Id = entityData.Id,
                Description = entityData.Description
            };
        }
    }
}