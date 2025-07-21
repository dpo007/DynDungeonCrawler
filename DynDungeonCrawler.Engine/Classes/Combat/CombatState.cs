using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Tracks the state of an ongoing combat encounter between a player and an enemy.
    /// </summary>
    public class CombatState
    {
        /// <summary>
        /// Gets the player combatant in this encounter.
        /// </summary>
        public ICombatable Player { get; }

        /// <summary>
        /// Gets the enemy combatant in this encounter.
        /// </summary>
        public ICombatable Enemy { get; }

        /// <summary>
        /// Gets the total number of turns that have elapsed in this combat.
        /// </summary>
        public int TurnCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the combat is still active (both combatants alive).
        /// </summary>
        public bool IsCombatActive => Player.IsAlive && Enemy.IsAlive;

        /// <summary>
        /// Gets a value indicating whether the player won the combat.
        /// </summary>
        public bool PlayerWon => Player.IsAlive && !Enemy.IsAlive;

        /// <summary>
        /// Gets a value indicating whether the player was defeated.
        /// </summary>
        public bool PlayerDefeated => !Player.IsAlive && Enemy.IsAlive;

        /// <summary>
        /// Gets a value indicating whether both combatants have been defeated (rare, but possible).
        /// </summary>
        public bool MutualDefeat => !Player.IsAlive && !Enemy.IsAlive;

        /// <summary>
        /// Gets a collection of combat messages generated during the current turn.
        /// </summary>
        public List<string> CombatMessages { get; } = new List<string>();

        /// <summary>
        /// Gets a history of combat results from all previous turns.
        /// </summary>
        public List<CombatResult> CombatHistory { get; } = new List<CombatResult>();

        /// <summary>
        /// Gets a value indicating whether it's currently the player's turn.
        /// </summary>
        public bool IsPlayerTurn => TurnCount % 2 == 0; // Player goes first (turn 0)

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatState"/> class.
        /// </summary>
        /// <param name="player">The player combatant.</param>
        /// <param name="enemy">The enemy combatant.</param>
        public CombatState(ICombatable player, ICombatable enemy)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            TurnCount = 0;
        }

        /// <summary>
        /// Advances the combat to the next turn.
        /// </summary>
        public void NextTurn()
        {
            TurnCount++;
            CombatMessages.Clear();
        }

        /// <summary>
        /// Adds a message to the current turn's combat log.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void AddMessage(string message)
        {
            CombatMessages.Add(message);
        }

        /// <summary>
        /// Records a combat result in the history.
        /// </summary>
        /// <param name="result">The combat result to record.</param>
        public void RecordResult(CombatResult result)
        {
            CombatHistory.Add(result);
        }

        /// <summary>
        /// Creates a summary of the combat outcome.
        /// </summary>
        /// <returns>A final combat summary.</returns>
        public CombatSummary GetSummary()
        {
            CombatOutcome outcome;

            if (PlayerWon)
            {
                outcome = CombatOutcome.PlayerVictory;
            }
            else if (PlayerDefeated)
            {
                outcome = CombatOutcome.PlayerDefeat;
            }
            else if (MutualDefeat)
            {
                outcome = CombatOutcome.MutualDefeat;
            }
            else
            {
                outcome = CombatOutcome.Ongoing;
            }

            Adventurer? playerEntity = Player as Adventurer;

            return new CombatSummary(
                outcome,
                TurnCount,
                Player.Health,
                Enemy.Health,
                Enemy is Enemy enemyEntity ? enemyEntity.MoneyReward : 0,
                playerEntity
            );
        }
    }

    /// <summary>
    /// Possible outcomes of a combat encounter.
    /// </summary>
    public enum CombatOutcome
    {
        /// <summary>
        /// Combat is still in progress.
        /// </summary>
        Ongoing,

        /// <summary>
        /// Player has defeated the enemy.
        /// </summary>
        PlayerVictory,

        /// <summary>
        /// Player has been defeated by the enemy.
        /// </summary>
        PlayerDefeat,

        /// <summary>
        /// Both player and enemy have been defeated (rare).
        /// </summary>
        MutualDefeat,

        /// <summary>
        /// Combat was fled or abandoned.
        /// </summary>
        Fled
    }
}