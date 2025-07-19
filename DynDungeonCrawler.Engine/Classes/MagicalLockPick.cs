using DynDungeonCrawler.Engine.Data;

namespace DynDungeonCrawler.Engine.Classes
{
    /// <summary>
    /// Represents a magical lock pick that can unlock any treasure chest.
    /// </summary>
    public class MagicalLockPick : Entity
    {
        /// <summary>
        /// Creates a new magical lock pick.
        /// </summary>
        /// <param name="name">The name of the lock pick. Defaults to "Magical Lock Pick".</param>
        public MagicalLockPick(string name = "Magical Lock Pick")
            : base(EntityType.MagicalLockPick, name)
        {
            Description = "A slender, ornate pick with shimmering runes etched along its surface. It radiates a subtle magical aura.";
            ShortDescription = "A magical tool that can unlock any chest.";
        }

        /// <summary>
        /// Uses the lock pick on a treasure chest to unlock it.
        /// </summary>
        /// <param name="chest">The treasure chest to unlock.</param>
        /// <returns>True if the chest was successfully unlocked; otherwise, false.</returns>
        public bool UseOn(TreasureChest chest)
        {
            if (chest == null)
            {
                return false;
            }

            if (!chest.IsLocked)
            {
                return false; // Already unlocked
            }

            chest.IsLocked = false;
            return true;
        }

        /// <summary>
        /// Converts the magical lock pick to a data object for serialization.
        /// </summary>
        /// <returns>An EntityData object representing the magical lock pick.</returns>
        public override EntityData ToEntityData()
        {
            // Use the base EntityData conversion
            return base.ToEntityData();
        }
    }
}