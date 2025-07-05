using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.GameLoop
{
    internal static class GameLoopRunner
    {
        /// <summary>
        /// Runs the main game loop, handling player input and game state updates
        /// until the player either exits, dies, or reaches the exit room.
        /// </summary>
        /// <param name="ui">The user interface for input/output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        /// <param name="llmClient">AI client for generating descriptions as needed.</param>
        /// <param name="dungeon">The Dungeon instance containing rooms and state.</param>
        /// <param name="player">The Adventurer representing the player.</param>
        public static async Task GameLoopAsync(
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

        // Helper method for available directions (copied from ConDungeon)
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