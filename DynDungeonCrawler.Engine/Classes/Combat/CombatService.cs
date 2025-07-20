using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Provides combat functionality, managing encounters between combatable entities.
    /// </summary>
    public class CombatService
    {
        private readonly IUserInterface _ui;
        private readonly ILogger _logger;
        private static readonly Random _random = Random.Shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatService"/> class.
        /// </summary>
        /// <param name="ui">The user interface for input/output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        public CombatService(IUserInterface ui, ILogger logger)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a full combat encounter between a player and an enemy.
        /// </summary>
        /// <param name="player">The player combatant.</param>
        /// <param name="enemy">The enemy combatant.</param>
        /// <returns>A combat summary with the final outcome.</returns>
        public async Task<CombatSummary> ExecuteCombatAsync(Adventurer player, Enemy enemy)
        {
            _logger.Log($"Combat initiated: {player.Name} vs {enemy.Name}");

            CombatState state = new(player, enemy);

            // Display combat start message
            _ui.Clear();
            _ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
            _ui.WriteRule("COMBAT");
            _ui.WriteLine($"{enemy.Name} prepares to attack!");
            _ui.WriteLine();
            _ui.WriteLine($"{enemy.ShortDescription}");
            _ui.WriteLine();
            _ui.WriteLine($"Enemy stats: Health: {enemy.Health}, Strength: {enemy.Strength}");
            _ui.WriteLine();
            _ui.WriteLine("Press any key to begin combat...");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);

            // Main combat loop
            while (state.IsCombatActive)
            {
                _ui.Clear();
                _ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
                _ui.WriteRule("COMBAT");

                // Display enemy stats
                _ui.WriteLine($"Fighting {enemy.Name} - Health: {enemy.Health}");
                _ui.WriteLine();

                await ExecuteTurnAsync(state);

                // Show combat messages
                foreach (string message in state.CombatMessages)
                {
                    _ui.WriteLine(message);
                }
                _ui.WriteLine();

                // Break if combat is over
                if (!state.IsCombatActive)
                {
                    break;
                }

                // Wait for player to continue to next turn
                _ui.WriteLine("Press any key for next turn...");
                await _ui.ReadKeyAsync(intercept: true, hideCursor: true);

                // Advance to next turn
                state.NextTurn();
            }

            // Combat complete, display outcome
            _ui.Clear();
            _ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
            _ui.WriteRule("COMBAT OVER");
            _ui.WriteLine();

            CombatSummary summary = state.GetSummary();

            if (summary.Outcome == CombatOutcome.PlayerVictory)
            {
                // Use plain messages - let the UI implementation handle any formatting
                string victoryMessage = $"You have defeated the {enemy.Name}!";
                _ui.WriteLine(victoryMessage, center: true);

                // Award money if any
                if (summary.MoneyReward > 0)
                {
                    player.AddWealth(summary.MoneyReward);
                    _ui.WriteLine($"You found {summary.MoneyReward} coins on the defeated enemy!");
                }
            }
            else if (summary.Outcome == CombatOutcome.PlayerDefeat)
            {
                _ui.WriteLine($"You have been defeated by the {enemy.Name}!");
            }

            _ui.WriteLine();
            _ui.WriteLine("Press any key to continue...");
            await _ui.ReadKeyAsync(intercept: true, hideCursor: true);

            _logger.Log($"Combat completed: {summary.Outcome}, Player health: {summary.PlayerRemainingHealth}, Enemy health: {summary.EnemyRemainingHealth}");
            return summary;
        }

        /// <summary>
        /// Executes a single turn of combat.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        private async Task ExecuteTurnAsync(CombatState state)
        {
            if (state.IsPlayerTurn)
            {
                await ExecutePlayerTurnAsync(state);
            }
            else
            {
                ExecuteEnemyTurn(state);
            }
        }

        /// <summary>
        /// Executes the player's turn in combat, including action selection.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        private async Task ExecutePlayerTurnAsync(CombatState state)
        {
            _ui.WriteLine("Your turn! Choose an action:");
            _ui.WriteLine("  1 - Attack");
            _ui.WriteLine("  2 - Defend (reduce incoming damage next turn)");
            _ui.WriteLine("  3 - Attempt to flee");
            _ui.WriteLine();
            _ui.Write("Enter choice [1-3]: ");

            string? input = await _ui.ReadLineAsync();
            _ui.WriteLine();

            switch (input?.Trim())
            {
                case "1":
                    // Attack
                    CombatResult attackResult = AttackTarget(state.Player, state.Enemy);
                    state.RecordResult(attackResult);

                    if (attackResult.WasDodged)
                    {
                        state.AddMessage($"You attack the {state.Enemy.Name}, but they dodge your strike!");
                    }
                    else if (attackResult.IsCriticalHit)
                    {
                        state.AddMessage($"You land a CRITICAL HIT on the {state.Enemy.Name} for {attackResult.DamageDealt} damage!");
                    }
                    else
                    {
                        state.AddMessage($"You attack the {state.Enemy.Name} for {attackResult.DamageDealt} damage!");

                        if (attackResult.DamageAbsorbed > 0)
                        {
                            state.AddMessage($"  The enemy's defense absorbed {attackResult.DamageAbsorbed} damage.");
                        }
                    }

                    if (!state.Enemy.IsAlive)
                    {
                        state.AddMessage($"The {state.Enemy.Name} has been defeated!");
                    }
                    break;

                case "2":
                    // Defend (will be handled in enemy turn)
                    // For simplicity, we'll implement a temporary defense boost
                    if (state.Player is Adventurer player)
                    {
                        player.SetDefendingStatus(true);
                        state.AddMessage("You brace yourself defensively, preparing for the enemy's attack.");
                        state.AddMessage("  Your defense will be increased for the next incoming attack.");
                    }
                    break;

                case "3":
                    // Attempt to flee
                    double fleeChance = 0.4; // 40% base chance

                    // Higher chance to flee if player health is low
                    if (state.Player.Health < 20)
                    {
                        fleeChance += 0.2;
                    }

                    if (_random.NextDouble() < fleeChance)
                    {
                        state.AddMessage("You successfully flee from combat!");

                        // Set combat as no longer active, with fled outcome
                        if (state.Enemy is Enemy enemy)
                        {
                            enemy.Health = 0; // A hack to make combat end
                        }

                        // CombatOutcome will be handled in GetSummary
                    }
                    else
                    {
                        state.AddMessage("You failed to flee from combat!");
                    }
                    break;

                default:
                    // Invalid choice, skip turn
                    state.AddMessage("Invalid action! Your turn is wasted.");
                    break;
            }
        }

        /// <summary>
        /// Executes the enemy's turn in combat.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        private void ExecuteEnemyTurn(CombatState state)
        {
            // Enemy AI logic - for now, always attack
            CombatResult attackResult = AttackTarget(state.Enemy, state.Player);
            state.RecordResult(attackResult);

            if (attackResult.WasDodged)
            {
                state.AddMessage($"The {state.Enemy.Name} attacks, but you dodge the strike!");
            }
            else if (attackResult.IsCriticalHit)
            {
                state.AddMessage($"The {state.Enemy.Name} lands a CRITICAL HIT on you for {attackResult.DamageDealt} damage!");
            }
            else
            {
                state.AddMessage($"The {state.Enemy.Name} attacks you for {attackResult.DamageDealt} damage!");

                if (attackResult.DamageAbsorbed > 0)
                {
                    state.AddMessage($"  Your defense absorbed {attackResult.DamageAbsorbed} damage.");
                }
            }

            if (!state.Player.IsAlive)
            {
                state.AddMessage($"You have been defeated!");
            }

            // Reset defending status after enemy attack
            if (state.Player is Adventurer player)
            {
                player.SetDefendingStatus(false);
            }
        }

        /// <summary>
        /// Calculates and applies damage from an attacker to a target.
        /// </summary>
        /// <param name="attacker">The combatant making the attack.</param>
        /// <param name="target">The combatant receiving the attack.</param>
        /// <returns>The result of the attack.</returns>
        private CombatResult AttackTarget(ICombatable attacker, ICombatable target)
        {
            // Check for dodge (10% base chance, higher if target is defending)
            bool isTargetDefending = (target is Adventurer player) && player.IsDefending;
            double dodgeChance = 0.1 + (isTargetDefending ? 0.15 : 0);

            if (_random.NextDouble() < dodgeChance)
            {
                return CombatResult.Dodged();
            }

            // Check for critical hit (15% chance)
            bool isCriticalHit = _random.NextDouble() < 0.15;

            // Calculate raw damage
            double baseDamage = attacker.Strength;
            double randomFactor = 0.8 + (_random.NextDouble() * 0.4); // 0.8 to 1.2 range
            int rawDamage = (int)Math.Round(baseDamage * randomFactor);

            // Apply critical hit bonus
            if (isCriticalHit)
            {
                rawDamage = (int)(rawDamage * 1.5);
            }

            // Calculate defense reduction
            double defenseModifier = isTargetDefending ? 1.5 : 1.0; // 50% more effective when defending
            double defenseValue = target.Defense * defenseModifier;

            // Apply diminishing returns formula: reduction = defense / (defense + 50)
            double damageReduction = defenseValue / (defenseValue + 50);
            int reducedDamage = (int)(rawDamage * (1 - damageReduction));

            // Ensure minimum damage of 1 if not dodged
            int finalDamage = Math.Max(1, reducedDamage);

            // Apply damage to target's health
            target.Health -= finalDamage;

            // Ensure health doesn't go below 0
            if (target.Health < 0)
            {
                target.Health = 0;
            }

            return new CombatResult(rawDamage, finalDamage, target.IsAlive, isCriticalHit);
        }
    }
}