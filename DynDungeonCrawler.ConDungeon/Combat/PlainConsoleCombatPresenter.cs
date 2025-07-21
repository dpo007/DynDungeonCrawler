using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Classes.Combat;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.Combat
{
    /// <summary>
    /// Implements the ICombatPresenter interface for plain console UI,
    /// providing combat presentation without any styling or markup.
    /// </summary>
    public class PlainConsoleCombatPresenter : ICombatPresenter
    {
        private readonly IUserInterface _ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlainConsoleCombatPresenter"/> class.
        /// </summary>
        /// <param name="ui">The user interface for input/output operations.</param>
        public PlainConsoleCombatPresenter(IUserInterface ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        /// <summary>
        /// Displays the start of a combat encounter with initial information.
        /// </summary>
        /// <param name="player">The player combatant.</param>
        /// <param name="enemy">The enemy combatant.</param>
        public async Task DisplayCombatStartAsync(Adventurer player, Enemy enemy)
        {
            _ui.Clear();
            _ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
            _ui.WriteRule("COMBAT");
            _ui.WriteLine($"{enemy.Name} prepares to attack!");
            _ui.WriteLine();
            _ui.WriteLine($"{enemy.ShortDescription}");
            _ui.WriteLine();
            _ui.WriteLine($"Enemy stats: Health: {enemy.Health}, Strength: {enemy.Strength}, Defense: {enemy.Defense}");
            _ui.WriteLine();
            _ui.WriteLine("Press any key to begin combat...");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);
        }

        /// <summary>
        /// Displays the current combat state including enemy information.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        public Task DisplayCombatStateAsync(CombatState state)
        {
            _ui.Clear();

            // Update player status bar
            if (state.Player is Adventurer player)
            {
                _ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
            }

            _ui.WriteRule("COMBAT");

            // Display enemy stats without any styling
            _ui.WriteLine($"Fighting {state.Enemy.Name} - Health: {state.Enemy.Health}");
            _ui.WriteLine();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays a collection of combat messages from the current turn.
        /// </summary>
        /// <param name="messages">The combat messages to display.</param>
        public Task DisplayCombatMessagesAsync(IEnumerable<string> messages)
        {
            // Plain text implementation - just display messages without formatting
            foreach (string message in messages)
            {
                _ui.WriteLine(message);
            }

            _ui.WriteLine();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays the final combat outcome and summary.
        /// </summary>
        /// <param name="summary">The combat summary with results.</param>
        public async Task DisplayCombatSummaryAsync(CombatSummary summary)
        {
            _ui.Clear();

            // Assuming we still have access to the player through context
            if (summary.PlayerRemainingHealth >= 0)
            {
                _ui.UpdateStatus("Player", 0, 0, summary.PlayerRemainingHealth); // Simplified status
            }

            _ui.WriteRule("COMBAT OVER");
            _ui.WriteLine();

            switch (summary.Outcome)
            {
                case CombatOutcome.PlayerVictory:
                    _ui.WriteLine("Victory!", center: true);
                    _ui.WriteLine($"You have emerged victorious from the battle!", center: true);

                    if (summary.MoneyReward > 0)
                    {
                        _ui.WriteLine($"You found {summary.MoneyReward} coins on the defeated enemy!");
                    }
                    break;

                case CombatOutcome.PlayerDefeat:
                    _ui.WriteLine("Defeat!", center: true);
                    _ui.WriteLine("You have been defeated in combat.", center: true);
                    break;

                case CombatOutcome.Fled:
                    _ui.WriteLine("Escaped!", center: true);
                    _ui.WriteLine("You have escaped from the battle.", center: true);
                    break;

                case CombatOutcome.MutualDefeat:
                    _ui.WriteLine("Mutual Defeat!", center: true);
                    _ui.WriteLine("Both you and your enemy have fallen in battle.", center: true);
                    break;
            }

            _ui.WriteLine();
            _ui.WriteLine("Press any key to continue...");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);
        }

        /// <summary>
        /// Gets the player's chosen combat action for their turn.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        /// <returns>A task that resolves to the chosen combat action.</returns>
        public async Task<CombatAction> GetPlayerActionAsync(CombatState state)
        {
            _ui.WriteLine("Your turn! Choose an action:");
            _ui.WriteLine("  1 - Attack");
            _ui.WriteLine("  2 - Defend (reduce incoming damage next turn)");
            _ui.WriteLine("  3 - Attempt to flee");
            _ui.WriteLine();
            _ui.Write("Enter choice [1-3]: ");

            string? input = await _ui.ReadLineAsync();
            _ui.WriteLine();

            return input?.Trim() switch
            {
                "1" => CombatAction.Attack(),
                "2" => CombatAction.Defend(),
                "3" => CombatAction.Flee(),
                _ => CombatAction.Invalid() // Handle invalid input
            };
        }

        /// <summary>
        /// Waits for the player to indicate they're ready to continue to the next turn.
        /// </summary>
        public async Task WaitForContinueAsync()
        {
            _ui.WriteLine("Press any key for next turn...");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);
        }
    }
}