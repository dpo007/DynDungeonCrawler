using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Classes.Combat;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.Combat
{
    /// <summary>
    /// Implements the ICombatPresenter interface for Spectre Console UI,
    /// providing rich, styled combat presentation with markup.
    /// </summary>
    public class SpectreConsoleCombatPresenter : ICombatPresenter
    {
        private readonly IUserInterface _ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectreConsoleCombatPresenter"/> class.
        /// </summary>
        /// <param name="ui">The user interface for input/output operations.</param>
        public SpectreConsoleCombatPresenter(IUserInterface ui)
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
            _ui.UpdateStatus(player);
            _ui.WriteRule("[bold]COMBAT[/]");
            _ui.WriteLine($"[bold]{enemy.Name}[/] prepares to attack!");
            _ui.WriteLine();
            _ui.WriteLine($"[italic]{enemy.ShortDescription}[/]");
            _ui.WriteLine();
            _ui.WriteLine($"Enemy stats: [red]Health:[/] {enemy.Health}, [yellow]Strength:[/] {enemy.Strength}, [blue]Defense:[/] {enemy.Defense}");
            _ui.WriteLine();
            _ui.WriteLine("[dim]Press any key to begin combat...[/]");
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
                _ui.UpdateStatus(player);
            }

            _ui.WriteRule("[bold]COMBAT[/]");

            // Display enemy stats with colored formatting
            _ui.WriteLine($"Fighting [bold red]{state.Enemy.Name}[/] - [red]Health:[/] {state.Enemy.Health}");
            _ui.WriteLine();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays a collection of combat messages from the current turn.
        /// </summary>
        /// <param name="messages">The combat messages to display.</param>
        public Task DisplayCombatMessagesAsync(IEnumerable<string> messages)
        {
            foreach (string message in messages)
            {
                // Apply Spectre Console formatting to different message types
                if (message.Contains("CRITICAL HIT"))
                {
                    _ui.WriteLine($"[bold yellow]{message}[/]");
                }
                else if (message.Contains("dodge"))
                {
                    _ui.WriteLine($"[cyan]{message}[/]");
                }
                else if (message.Contains("defeated"))
                {
                    _ui.WriteLine($"[bold green]{message}[/]");
                }
                else if (message.Contains("flee"))
                {
                    _ui.WriteLine($"[magenta]{message}[/]");
                }
                else
                {
                    _ui.WriteLine(message);
                }
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
            if (summary.PlayerRemainingHealth >= 0 && summary.Player != null)
            {
                _ui.UpdateStatus(summary.Player);
            }
            else if (summary.PlayerRemainingHealth >= 0)
            {
                Adventurer fallback = new Adventurer("Player");
                fallback.Health = summary.PlayerRemainingHealth;
                fallback.Strength = 0;
                fallback.Defense = 0;
                // Wealth is read-only, so skip assignment
                _ui.UpdateStatus(fallback);
            }

            _ui.WriteRule("[bold]COMBAT OVER[/]");
            _ui.WriteLine();

            switch (summary.Outcome)
            {
                case CombatOutcome.PlayerVictory:
                    _ui.WriteLine("[bold green]Victory![/]", center: true);
                    _ui.WriteLine($"You have emerged victorious from the battle!", center: true);

                    if (summary.MoneyReward > 0)
                    {
                        _ui.WriteLine($"You found [gold1]{summary.MoneyReward}[/] coins on the defeated enemy!");
                    }
                    break;

                case CombatOutcome.PlayerDefeat:
                    _ui.WriteLine("[bold red]Defeat![/]", center: true);
                    _ui.WriteLine("You have been defeated in combat.", center: true);
                    break;

                case CombatOutcome.Fled:
                    _ui.WriteLine("[bold yellow]Escaped![/]", center: true);
                    _ui.WriteLine("You have escaped from the battle.", center: true);
                    break;

                case CombatOutcome.MutualDefeat:
                    _ui.WriteLine("[bold magenta]Mutual Defeat![/]", center: true);
                    _ui.WriteLine("Both you and your enemy have fallen in battle.", center: true);
                    break;
            }

            _ui.WriteLine();
            _ui.WriteLine("[dim]Press any key to continue...[/]");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);
        }

        /// <summary>
        /// Gets the player's chosen combat action for their turn.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        /// <returns>A task that resolves to the chosen combat action.</returns>
        public async Task<CombatAction> GetPlayerActionAsync(CombatState state)
        {
            _ui.WriteLine("[bold white]Your turn![/] Choose an action:");
            _ui.WriteLine("  [bold red]1[/] - [cyan]Attack[/]");
            _ui.WriteLine("  [bold blue]2[/] - [green]Defend[/] (reduce incoming damage next turn)");
            _ui.WriteLine("  [bold yellow]3[/] - [magenta]Attempt to flee[/]");
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
            _ui.WriteLine("[dim]Press any key for next turn...[/]");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);
        }
    }
}