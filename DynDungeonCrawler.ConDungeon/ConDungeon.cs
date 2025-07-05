using DynDungeonCrawler.ConDungeon.GameLoop;
using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal class ConDungeon
    {
        /// <summary>
        /// Entry point for the console dungeon crawler. Initializes the game and starts the main game loop asynchronously.
        /// </summary>
        private static async Task Main(string[] args)
        {
            // Set the console output encoding to UTF-8 to support special characters
            Console.OutputEncoding = System.Text.Encoding.UTF8;

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

            // Start the game loop
            await GameLoopRunner.GameLoopAsync(ui, logger, llmClient, dungeon, player);
        }
    }
}