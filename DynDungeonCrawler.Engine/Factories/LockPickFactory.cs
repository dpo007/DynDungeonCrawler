using DynDungeonCrawler.Engine.Classes;

namespace DynDungeonCrawler.Engine.Factories
{
    /// <summary>
    /// Factory for creating magical lock pick items.
    /// </summary>
    public static class LockPickFactory
    {
        /// <summary>
        /// Creates a new magical lock pick.
        /// </summary>
        /// <param name="name">Optional custom name for the lock pick.</param>
        /// <returns>A new MagicalLockPick instance.</returns>
        public static MagicalLockPick CreateMagicalLockPick(string name = "Magical Lock Pick")
        {
            return new MagicalLockPick(name);
        }
    }
}