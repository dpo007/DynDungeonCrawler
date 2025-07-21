using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.GameLoop
{
    internal static class RoomRenderer
    {
        /// <summary>
        /// Displays the current room's description and contents to the user interface.
        /// If the player's current room is null, an error message is shown instead.
        /// </summary>
        /// <param name="ui">The user interface used for output.</param>
        /// <param name="player">The adventurer whose current room will be displayed.</param>
        public static void DrawRoom(IUserInterface ui, Adventurer player)
        {
            if (player.CurrentRoom == null)
            {
                ui.WriteLine("[bold red]Error:[/] Current room is null.");
                return;
            }

            // Show the room name (if available), otherwise a rule
            if (!string.IsNullOrWhiteSpace(player.CurrentRoom.Name))
            {
                ui.WriteRule($"[bold white]{player.CurrentRoom.Name}[/]");
            }
            else
            {
                ui.WriteRule();
            }

            ui.WriteLine();

            // Ensure the room description is not null before passing it to WriteLine
            string roomDescription = player.CurrentRoom.Description ?? "[dim]This room is an abyss.[/]";
            ui.WriteLine(roomDescription);

            if (player.CurrentRoom.Contents.Count > 0)
            {
                ui.WriteLine();
                ui.WriteLine("You notice the following things in the room:");

                foreach (Entity entity in player.CurrentRoom.Contents)
                {
                    string formattedEntityText = CreateEntityText(entity);
                    ui.WriteLine(formattedEntityText);
                }
            }

            ui.WriteLine();

            // Show direction room was entered from (if available)
            if (player.PreviousRoom == null)
            {
                ui.WriteRule();
            }
            else
            {
                string enteredFrom = GetEntranceDirection(player.PreviousRoom, player.CurrentRoom);
                if (!string.IsNullOrEmpty(enteredFrom))
                {
                    ui.WriteRule($"You entered from the {enteredFrom}");
                }
            }
            ui.WriteLine();
        }

        /// <summary>
        /// Creates appropriately formatted text for displaying an entity in the room with Spectre.Console markup.
        /// </summary>
        /// <param name="entity">The entity to create formatted text for.</param>
        /// <returns>A formatted string with Spectre.Console markup for the entity.</returns>
        private static string CreateEntityText(Entity entity)
        {
            string entityDesc;
            string markupColor;
            string nameMarkup;

            switch (entity)
            {
                case TreasureChest chest:
                    entityDesc = !string.IsNullOrWhiteSpace(chest.ShortDescription)
                        ? chest.ShortDescription
                        : "A chest of treasures.";
                    markupColor = "gold3_1";
                    nameMarkup = $"[bold {markupColor}]{chest.Name}[/]";
                    break;

                case Enemy enemy:
                    entityDesc = !string.IsNullOrWhiteSpace(enemy.ShortDescription)
                        ? enemy.ShortDescription
                        : enemy.Description;
                    markupColor = "red";
                    nameMarkup = $"[bold {markupColor}]{enemy.Name}[/]";
                    break;

                case MagicalLockPick lockPick:
                    entityDesc = !string.IsNullOrWhiteSpace(lockPick.ShortDescription)
                        ? lockPick.ShortDescription
                        : lockPick.Description;
                    markupColor = "purple";
                    nameMarkup = $"[bold {markupColor}]{lockPick.Name}[/]";
                    break;

                default:
                    entityDesc = !string.IsNullOrWhiteSpace(entity.ShortDescription)
                        ? entity.ShortDescription
                        : entity.Description;
                    markupColor = "cyan1";
                    nameMarkup = $"[bold {markupColor}]{entity.Name}[/]";
                    break;
            }

            // Format the final output with Spectre.Console markup
            string content = !string.IsNullOrWhiteSpace(entityDesc)
                ? $"[dim]-[/] {nameMarkup}: {entityDesc}"
                : $"[dim]-[/] {nameMarkup}";

            return content;
        }

        /// <summary>
        /// Determines the entrance direction based on the previous and current room coordinates.
        /// Returns a string such as "North", "South", "East", or "West".
        /// </summary>
        public static string GetEntranceDirection(Room previous, Room current)
        {
            if (previous == null || current == null)
            {
                return string.Empty;
            }

            int dx = current.X - previous.X;
            int dy = current.Y - previous.Y;
            if (dx == 1 && dy == 0)
            {
                return "West";
            }

            if (dx == -1 && dy == 0)
            {
                return "East";
            }

            if (dx == 0 && dy == 1)
            {
                return "North";
            }

            if (dx == 0 && dy == -1)
            {
                return "South";
            }

            return string.Empty;
        }
    }
}