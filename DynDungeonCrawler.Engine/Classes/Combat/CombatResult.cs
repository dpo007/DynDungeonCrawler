namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Represents the result of a combat action, including information about damage dealt, absorbed, and target status.
    /// </summary>
    public class CombatResult
    {
        /// <summary>
        /// Gets the raw damage amount that was generated.
        /// </summary>
        public int RawDamage { get; }

        /// <summary>
        /// Gets the actual damage that was applied after defense and other mitigations.
        /// </summary>
        public int DamageDealt { get; }

        /// <summary>
        /// Gets the amount of damage that was absorbed by defense or other means.
        /// </summary>
        public int DamageAbsorbed { get; }

        /// <summary>
        /// Gets a value indicating whether the target is still alive after the combat action.
        /// </summary>
        public bool TargetIsAlive { get; }

        /// <summary>
        /// Gets a value indicating whether this was a critical hit.
        /// </summary>
        public bool IsCriticalHit { get; }

        /// <summary>
        /// Gets a value indicating whether the attack was dodged or evaded.
        /// </summary>
        public bool WasDodged { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatResult"/> class for a successful hit.
        /// </summary>
        /// <param name="rawDamage">The raw damage amount that was generated.</param>
        /// <param name="damageDealt">The actual damage that was applied after defense and other mitigations.</param>
        /// <param name="targetIsAlive">Whether the target is still alive after the combat action.</param>
        /// <param name="isCriticalHit">Whether this was a critical hit.</param>
        public CombatResult(int rawDamage, int damageDealt, bool targetIsAlive, bool isCriticalHit = false)
        {
            RawDamage = rawDamage;
            DamageDealt = damageDealt;
            DamageAbsorbed = rawDamage - damageDealt;
            TargetIsAlive = targetIsAlive;
            IsCriticalHit = isCriticalHit;
            WasDodged = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatResult"/> class for a dodged attack.
        /// </summary>
        /// <returns>A CombatResult representing a dodged attack.</returns>
        public static CombatResult Dodged()
        {
            CombatResult result = new CombatResult(0, 0, true);
            result.WasDodged = true;
            return result;
        }
    }
}