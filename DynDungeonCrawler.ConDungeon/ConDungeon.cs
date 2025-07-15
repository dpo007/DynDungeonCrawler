using DynDungeonCrawler.ConDungeon.GameLoop;
using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers.UI;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal class ConDungeon
    {
        /// <summary>
        /// Clears the UI and updates the player's status at the top of the console.
        /// </summary>
        /// <param name="ui">The user interface instance.</param>
        /// <param name="player">The current player.</param>
        private static void ClearAndShowStatus(IUserInterface ui, Adventurer player)
        {
            ui.Clear();
            ui.UpdateStatus(player.Health, player.Wealth, player.Name);
        }

        /// <summary>
        /// Entry point for the console dungeon crawler. Initializes the game and starts the main game loop asynchronously.
        /// </summary>
        private static async Task Main(string[] args)
        {
            // Set the console output encoding to UTF-8 to support special characters
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Gracefully handle LLMSettings errors
            LLMSettings llmSettings;
            try
            {
                llmSettings = LLMSettings.Load();
            }
            catch (InvalidOperationException ex)
            {
                // Use SpectreConsoleUserInterface for error output
                IUserInterface ui = new SpectreConsoleUserInterface();
                ui.WriteLine($"Error: {ex.Message}");
                ui.WriteLine("Press any key to exit.");
                await ui.ReadKeyAsync();
                return;
            }

            // Capture the nullable tuple
            (IUserInterface? ui, ILogger? logger, ILLMClient? llmClient, Dungeon? dungeon, Adventurer? player) init = await GameInitializer.InitializeGameAsync();

            // Bail out if any element is null
            if (init.ui == null ||
                init.logger == null ||
                init.llmClient == null ||
                init.dungeon == null ||
                init.player == null)
            {
                return;
            }

            // Safe to deconstruct into non-nullable locals
            (IUserInterface ui,
             ILogger logger,
             ILLMClient llmClient,
             Dungeon dungeon,
             Adventurer player) = init;

            // Show status after initial clear (if needed)
            ClearAndShowStatus(ui, player);

            // Start the game loop
            await GameLoopRunner.GameLoopAsync(ui, logger, llmClient, dungeon, player);
        }
    }
}