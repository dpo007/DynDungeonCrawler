using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Classes.Combat;

namespace DynDungeonCrawler.Engine.Interfaces
{
    /// <summary>
    /// Interface for presenting combat UI and gathering player input during combat encounters.
    /// Implementations can provide different UI experiences (console, HTML, etc.) while
    /// the core combat logic remains independent of presentation concerns.
    /// </summary>
    public interface ICombatPresenter
    {
        /// <summary>
        /// Displays the start of a combat encounter with initial information.
        /// </summary>
        /// <param name="player">The player combatant.</param>
        /// <param name="enemy">The enemy combatant.</param>
        /// <returns>A task that completes when the display is finished.</returns>
        Task DisplayCombatStartAsync(Adventurer player, Enemy enemy);

        /// <summary>
        /// Displays the current combat state including enemy information.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        /// <returns>A task that completes when the display is finished.</returns>
        Task DisplayCombatStateAsync(CombatState state);

        /// <summary>
        /// Displays a collection of combat messages from the current turn.
        /// </summary>
        /// <param name="messages">The combat messages to display.</param>
        /// <returns>A task that completes when all messages have been displayed.</returns>
        Task DisplayCombatMessagesAsync(IEnumerable<string> messages);

        /// <summary>
        /// Displays the final combat outcome and summary.
        /// </summary>
        /// <param name="summary">The combat summary with results.</param>
        /// <returns>A task that completes when the summary has been displayed.</returns>
        Task DisplayCombatSummaryAsync(CombatSummary summary);

        /// <summary>
        /// Gets the player's chosen combat action for their turn.
        /// </summary>
        /// <param name="state">The current combat state.</param>
        /// <returns>A task that resolves to the chosen combat action.</returns>
        Task<CombatAction> GetPlayerActionAsync(CombatState state);

        /// <summary>
        /// Waits for the player to indicate they're ready to continue to the next turn.
        /// </summary>
        /// <returns>A task that completes when the player is ready to continue.</returns>
        Task WaitForContinueAsync();
    }
}