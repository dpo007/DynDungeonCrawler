namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Defines the available action types that can be performed during combat.
    /// </summary>
    public enum CombatActionType
    {
        /// <summary>
        /// Attack the enemy, dealing damage based on strength.
        /// </summary>
        Attack,

        /// <summary>
        /// Defend against incoming attacks, increasing defense for the next turn.
        /// </summary>
        Defend,

        /// <summary>
        /// Attempt to flee from combat with a chance of success.
        /// </summary>
        Flee,

        /// <summary>
        /// Invalid or unrecognized action.
        /// </summary>
        Invalid
    }

    /// <summary>
    /// Represents a combat action that can be performed by a combatant.
    /// </summary>
    public class CombatAction
    {
        /// <summary>
        /// Gets the type of combat action.
        /// </summary>
        public CombatActionType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatAction"/> class.
        /// </summary>
        /// <param name="type">The type of combat action.</param>
        public CombatAction(CombatActionType type)
        {
            Type = type;
        }

        /// <summary>
        /// Creates an attack action.
        /// </summary>
        /// <returns>A combat action of type Attack.</returns>
        public static CombatAction Attack() => new CombatAction(CombatActionType.Attack);

        /// <summary>
        /// Creates a defend action.
        /// </summary>
        /// <returns>A combat action of type Defend.</returns>
        public static CombatAction Defend() => new CombatAction(CombatActionType.Defend);

        /// <summary>
        /// Creates a flee action.
        /// </summary>
        /// <returns>A combat action of type Flee.</returns>
        public static CombatAction Flee() => new CombatAction(CombatActionType.Flee);

        /// <summary>
        /// Creates an invalid action.
        /// </summary>
        /// <returns>A combat action of type Invalid.</returns>
        public static CombatAction Invalid() => new CombatAction(CombatActionType.Invalid);
    }
}