namespace DynDungeonCrawler.Engine.Interfaces
{
    /// <summary>
    /// Interface for entities that can participate in combat, defining core combat attributes and states.
    /// </summary>
    public interface ICombatable
    {
        /// <summary>
        /// Current health of the combatant. Combat ends for a combatant when health reaches 0.
        /// </summary>
        int Health { get; set; }

        /// <summary>
        /// Offensive capability of the combatant, influencing base damage output.
        /// </summary>
        int Strength { get; }

        /// <summary>
        /// Defensive capability of the combatant, reducing damage taken.
        /// </summary>
        int Defense { get; }

        /// <summary>
        /// The name of the combatant, used in combat messages.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Indicates whether the combatant is still alive and able to fight.
        /// </summary>
        bool IsAlive => Health > 0;
    }
}