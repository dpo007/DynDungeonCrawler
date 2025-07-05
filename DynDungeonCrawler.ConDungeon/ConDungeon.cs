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
            await GameLoopAsync(ui, logger, llmClient, dungeon, player);
        }

        /// <summary>
        /// Runs the main game loop, handling player input and game state updates
        /// until the player either exits, dies, or reaches the exit room.
        /// </summary>
        /// <param name="ui">The user interface for input/output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        /// <param name="llmClient">AI client for generating descriptions as needed.</param>
        /// <param name="dungeon">The Dungeon instance containing rooms and state.</param>
        /// <param name="player">The Adventurer representing the player.</param>
        private static async Task GameLoopAsync(
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player)
        {
            while (true)
            {
                // Game-ending conditions
                if (player.Health <= 0)
                {
                    ui.WriteLine("You have perished in the dungeon. Game over!");
                    break;
                }
                if (player.CurrentRoom?.Type == RoomType.Exit)
                {
                    RoomRenderer.DrawRoom(ui, player);

                    ui.ShowSpecialMessage("Congratulations! You have found the exit and escaped the dungeon!", center: true, writeLine: true);
                    break;
                }
                if (player.CurrentRoom == null)
                {
                    ui.WriteLine("You are lost in the void. Game over!");
                    break;
                }

                RoomRenderer.DrawRoom(ui, player);

                List<string> directions = GetAvailableDirections(player.CurrentRoom);
                char cmdChar = await InputHandler.HandleInputAsync(ui, directions, player);

                if (!await CommandProcessor.ProcessCommandAsync(cmdChar, ui, logger, llmClient, dungeon, player, directions))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns a list of available movement directions from the specified room,
        /// based on which exits are connected. Each direction is represented as a
        /// single uppercase letter: "N" (north), "E" (east), "S" (south), or "W" (west).
        /// </summary>
        /// <param name="room">The room to check for available exits.</param>
        /// <returns>A list of direction strings indicating which exits are available from the room.</returns>
        private static List<string> GetAvailableDirections(Room room)
        {
            List<string> directions = new();
            if (room.ConnectedNorth)
            {
                directions.Add("N");
            }

            if (room.ConnectedEast)
            {
                directions.Add("E");
            }

            if (room.ConnectedSouth)
            {
                directions.Add("S");
            }

            if (room.ConnectedWest)
            {
                directions.Add("W");
            }

            return directions;
        }
    }
}