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
                    switch (entity)
                    {
                        case TreasureChest chest:
                            string chestState = chest.IsOpened ? "opened" : (chest.IsLocked ? "locked" : "unlocked");
                            string displayDesc = !string.IsNullOrWhiteSpace(chest.ShortDescription)
                                ? chest.ShortDescription
                                : "";

                            if (!string.IsNullOrWhiteSpace(displayDesc))
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{chest.Name}[/] ({chestState}): {displayDesc}");
                            }
                            else
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{chest.Name}[/] ({chestState})");
                            }
                            break;

                        default:
                            string entityDesc = !string.IsNullOrWhiteSpace(entity.ShortDescription)
                                ? entity.ShortDescription
                                : entity.Description;
                            if (!string.IsNullOrWhiteSpace(entityDesc))
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]: {entityDesc}");
                            }
                            else
                            {
                                ui.WriteLine($"[dim]-[/] [bold]{entity.Name}[/]");
                            }

                            break;
                    }
                }
            }

            ui.WriteLine();

            // Show direction room was entred from (if available)
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