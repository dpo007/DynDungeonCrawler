namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Represents a summary of a completed combat encounter, including outcome and rewards.
    /// </summary>
    public class CombatSummary
    {
        /// <summary>
        /// Gets the final outcome of the combat encounter.
        /// </summary>
        public CombatOutcome Outcome { get; }

        /// <summary>
        /// Gets the total number of turns the combat lasted.
        /// </summary>
        public int TurnCount { get; }

        /// <summary>
        /// Gets the player's remaining health after combat.
        /// </summary>
        public int PlayerRemainingHealth { get; }

        /// <summary>
        /// Gets the enemy's remaining health after combat.
        /// </summary>
        public int EnemyRemainingHealth { get; }

        /// <summary>
        /// Gets the money reward from defeating the enemy.
        /// </summary>
        public int MoneyReward { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatSummary"/> class.
        /// </summary>
        /// <param name="outcome">The outcome of the combat.</param>
        /// <param name="turnCount">The number of turns the combat lasted.</param>
        /// <param name="playerRemainingHealth">The player's remaining health.</param>
        /// <param name="enemyRemainingHealth">The enemy's remaining health.</param>
        /// <param name="moneyReward">The money reward for defeating the enemy.</param>
        public CombatSummary(
            CombatOutcome outcome,
            int turnCount,
            int playerRemainingHealth,
            int enemyRemainingHealth,
            int moneyReward)
        {
            Outcome = outcome;
            TurnCount = turnCount;
            PlayerRemainingHealth = playerRemainingHealth;
            EnemyRemainingHealth = enemyRemainingHealth;
            MoneyReward = moneyReward;
        }

        /// <summary>
        /// Gets a descriptive message about the combat outcome.
        /// </summary>
        /// <returns>A string describing the combat outcome.</returns>
        public string GetOutcomeMessage()
        {
            return Outcome switch
            {
                CombatOutcome.PlayerVictory => "You have defeated the enemy!",
                CombatOutcome.PlayerDefeat => "You have been defeated by the enemy.",
                CombatOutcome.MutualDefeat => "Both you and the enemy have fallen.",
                CombatOutcome.Fled => "You have fled from combat.",
                _ => "Combat is still ongoing."
            };
        }
    }
}