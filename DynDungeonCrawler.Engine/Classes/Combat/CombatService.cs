using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Classes.Combat
{
    /// <summary>
    /// Provides combat functionality, managing encounters between combatable entities.
    /// </summary>
    public class CombatService
    {
        private readonly ILogger _logger;
        private static readonly Random _random = Random.Shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatService"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        public CombatService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a full combat encounter between a player and an enemy.
        /// </summary>
        /// <param name="player">The player combatant.</param>
        /// <param name="enemy">The enemy combatant.</param>
        /// <param name="presenter">The combat presenter that handles UI interactions.</param>
        /// <returns>A combat summary with the final outcome.</returns>
        public async Task<CombatSummary> ExecuteCombatAsync(
            Adventurer player,
            Enemy enemy,
            ICombatPresenter presenter)
        {
            _logger.Log($"Combat initiated: {player.Name} vs {enemy.Name}");

            CombatState state = new(player, enemy);

            // Display combat start message through the presenter
            await presenter.DisplayCombatStartAsync(player, enemy);

            // Main combat loop
            while (state.IsCombatActive)
            {
                await presenter.DisplayCombatStateAsync(state);

                if (state.IsPlayerTurn)
                {
                    await ExecutePlayerTurnAsync(state, presenter);
                }
                else
                {
                    ExecuteEnemyTurn(state);
                }

                // Show combat messages through presenter
                await presenter.DisplayCombatMessagesAsync(state.CombatMessages);

                // Break if combat is over
                if (!state.IsCombatActive)
                {
                    break;
                }

                // Wait for player to continue to next turn
                await presenter.WaitForContinueAsync();

                // Advance to next turn
                state.NextTurn();
            }

            // Get combat summary
            CombatSummary summary = state.GetSummary();

            // Handle rewards for victory
            if (summary.Outcome == CombatOutcome.PlayerVictory && summary.MoneyReward > 0)
            {
                player.AddWealth(summary.MoneyReward);
            }

            // Display combat summary through presenter
            await presenter.DisplayCombatSummaryAsync(summary);

            _logger.Log($"Combat completed: {summary.Outcome}, Player health: {summary.PlayerRemainingHealth}, Enemy health: {summary.EnemyRemainingHealth}");
            return summary;
        }

        /// <summary>
        /// Executes the player's turn in combat, getting action choice from the presenter.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        /// <param name="presenter">The combat presenter that handles UI interactions.</param>
        private async Task ExecutePlayerTurnAsync(CombatState state, ICombatPresenter presenter)
        {
            // Get the player's action choice from the presenter
            CombatAction action = await presenter.GetPlayerActionAsync(state);

            // Process the player's action
            switch (action.Type)
            {
                case CombatActionType.Attack:
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

                case CombatActionType.Defend:
                    // Defend (will be handled in enemy turn)
                    if (state.Player is Adventurer player)
                    {
                        player.SetDefendingStatus(true);
                        state.AddMessage("You brace yourself defensively, preparing for the enemy's attack.");
                        state.AddMessage("  Your defense will be increased for the next incoming attack.");
                    }
                    break;

                case CombatActionType.Flee:
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

                        // Set combat as no longer active, with escaped outcome
                        if (state.Enemy is Enemy enemy)
                        {
                            enemy.Health = 0; // A hack to make combat end
                        }
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