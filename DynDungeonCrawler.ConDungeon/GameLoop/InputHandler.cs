using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.GameLoop
{
    internal static class InputHandler
    {
        /// <summary>
        /// Asynchronously prompts the user for a command and reads a key input, validating it against the available movement directions
        /// and other valid commands (look, inventory, exit). Displays the chosen command in the UI.
        /// </summary>
        /// <param name="ui">The user interface used for input and output.</param>
        /// <param name="directions">A list of valid movement directions (e.g., "N", "E", "S", "W").</param>
        /// <param name="player">The current player (to check for entities in the room).</param>
        /// <returns>A task that resolves to the character representing the user's chosen command, in lowercase.</returns>
        public static async Task<char> HandleInputAsync(IUserInterface ui, List<string> directions, Adventurer player)
        {
            bool hasEntities = player.CurrentRoom != null && player.CurrentRoom.Contents.Count > 0;
            string directionsPrompt = directions.Count > 0 ? string.Join("[dim]/[/]", directions) : "";
            string prompt = $"Enter command (move [[[bold]{directionsPrompt}[/]]]";
            if (hasEntities)
            {
                prompt += ", [[[bold]L[/]]]ook";
            }

            prompt += ", [[[bold]I[/]]]nventory, e[[[bold]X[/]]]it): ";
            ui.Write(prompt);

            char cmdChar;
            while (true)
            {
                string cmdKeyStr = await ui.ReadKeyAsync(intercept: true);
                if (string.IsNullOrEmpty(cmdKeyStr))
                {
                    continue;
                }

                char keyChar = char.ToLower(cmdKeyStr[0]);
                cmdChar = keyChar;

                bool isValid =
                    cmdChar == 'x' ||
                    cmdChar == 'i' ||
                    directions.Contains(cmdChar.ToString().ToUpper()) ||
                    (hasEntities && cmdChar == 'l');

                if (isValid)
                {
                    ui.Write("[bold]" + char.ToUpper(cmdChar).ToString() + "[/]");
                    ui.WriteLine();
                    break;
                }
            }
            ui.WriteLine();
            return cmdChar;
        }
    }
}