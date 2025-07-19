using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Helpers.ContentGeneration;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.GameLoop
{
    internal static class CommandProcessor
    {
        /// <summary>
        /// Asynchronously processes the player's command input, updating game state and handling actions such as movement,
        /// looking around, viewing inventory, or exiting the game. Also manages invalid commands and ensures
        /// the player's current room is valid before executing actions.
        /// </summary>
        /// <param name="cmdChar">The character representing the player's chosen command.</param>
        /// <param name="ui">The user interface for input and output interactions.</param>
        /// <param name="logger">Logger for diagnostic and debug messages.</param>
        /// <param name="llmClient">AI client for generating room descriptions as needed.</param>
        /// <param name="dungeon">The Dungeon instance containing rooms and state.</param>
        /// <param name="player">The Adventurer representing the player.</param>
        /// <param name="directions">A list of valid movement directions (e.g., "N", "E", "S", "W").</param>
        /// <returns>
        /// A task that resolves to true to continue the game loop; false to exit the game loop (e.g., when the player chooses to exit).
        /// </returns>
        public static async Task<bool> ProcessCommandAsync(
            char cmdChar,
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player,
            List<string> directions)
        {
            // Null check player current room (to avoid NullReferenceException)
            if (player.CurrentRoom == null)
            {
                ui.WriteLine("[bold red]Error:[/] Current room is null. Cannot process command.");
                return true; // Continue the loop to avoid breaking the game
            }

            if (cmdChar == 'x')
            {
                ui.WriteLine("Exiting the game. [bold]Goodbye![/]");
                return false;
            }
            else if (cmdChar == 'l')
            {
                // Show menu of things to look at
                await HandleLookCommandAsync(ui, player);
            }
            else if (cmdChar == 'i')
            {
                ui.WriteLine("Your inventory contains:");
                if (player.Inventory == null || player.Inventory.Count == 0)
                {
                    ui.WriteLine("  [dim](You are not carrying anything.)[/]");
                }
                else
                {
                    foreach (Entity item in player.Inventory)
                    {
                        ui.WriteLine($" - {item.Name}");
                    }
                }

                ui.WriteLine();
                ui.WriteLine("[dim italic]Press any key to continue...[/]");
                await ui.ReadKeyAsync(intercept: true, hideCursor: true);
                ui.Clear();
                ui.UpdateStatus(player.Health, player.Wealth, player.Name);
                return true; // Continue the loop to allow further commands
            }
            else if (directions.Contains(cmdChar.ToString().ToUpper()))
            {
                RoomDirection moveDir = cmdChar switch
                {
                    'n' => RoomDirection.North,
                    'e' => RoomDirection.East,
                    's' => RoomDirection.South,
                    'w' => RoomDirection.West,
                    _ => throw new InvalidOperationException("Invalid direction")
                };

                Room? nextRoom = player.CurrentRoom.GetNeighbour(moveDir, dungeon.Grid);
                if (nextRoom != null)
                {
                    if (string.IsNullOrWhiteSpace(nextRoom.Description))
                    {
                        HashSet<Room> roomsToProcess = new HashSet<Room> { nextRoom };
                        List<Room> firstLevel = nextRoom.GetAccessibleNeighbours(dungeon.Grid);

                        foreach (Room neighbor in firstLevel)
                        {
                            roomsToProcess.Add(neighbor);
                        }

                        foreach (Room neighbor in firstLevel)
                        {
                            List<Room> secondLevel = neighbor.GetAccessibleNeighbours(dungeon.Grid);
                            foreach (Room secondNeighbor in secondLevel)
                            {
                                roomsToProcess.Add(secondNeighbor);
                            }
                        }

                        await ui.ShowSpinnerAsync("[italic]Generating rooms...[/]", async () =>
                        {
                            await RoomDescriptionGenerator.GenerateRoomDescriptionsAsync(roomsToProcess.ToList(), dungeon.Theme, llmClient, logger);

                            // Extract rooms that have entities that are treasure chests
                            List<Room> roomsWithChests = roomsToProcess
                              .Where(r => r.Contents.Any(e => e.Type == EntityType.TreasureChest))
                              .ToList();

                            // If there are any rooms with treasure chests, generate descriptions for them
                            if (roomsWithChests.Count > 0)
                            {
                                await TreasureChestFactory.GenerateTreasureDescriptionsAsync(roomsWithChests, dungeon.Theme, llmClient, logger);
                            }
                            return true;
                        });
                    }
                    player.PreviousRoom = player.CurrentRoom; // Track previous room before moving
                    player.CurrentRoom = nextRoom;
                    player.VisitedRoomIds.Add(nextRoom.Id);
                    ui.Clear();
                    ui.UpdateStatus(player.Health, player.Wealth, player.Name);
                }
                else
                {
                    ui.WriteLine("You can't go that way.");
                }
            }
            else
            {
                ui.WriteLine("[bold red]Invalid command.[/] Please try again.");
            }
            return true;
        }

        /// <summary>
        /// Handles the Look command by presenting a menu of things to look at in detail,
        /// including each entity in the room (but not the room itself).
        /// </summary>
        /// <param name="ui">The user interface for I/O operations.</param>
        /// <param name="player">The current player.</param>
        /// <returns>A task that completes when the look operation is finished.</returns>
        private static async Task HandleLookCommandAsync(IUserInterface ui, Adventurer player)
        {
            if (player.CurrentRoom == null)
            {
                ui.WriteLine("[bold red]Error:[/] Current room is null.");
                ui.WriteLine();
                ui.Write("[dim]Press any key to continue...[/]");
                await ui.ReadKeyAsync();
                ui.Clear();
                return;
            }

            // Build a list of lookable things
            List<(string name, string description, string color, Entity entity)> lookables = new();

            foreach (Entity entity in player.CurrentRoom.Contents)
            {
                string name;
                string description;
                string color;

                switch (entity)
                {
                    case TreasureChest chest:
                        name = chest.Name;
                        description = !string.IsNullOrWhiteSpace(chest.Description)
                            ? chest.Description
                            : "A mysterious chest with no further details.";
                        color = "gold3_1";
                        break;

                    case Enemy enemy:
                        name = enemy.Name;
                        description = !string.IsNullOrWhiteSpace(enemy.Description)
                            ? enemy.Description
                            : "A fearsome creature lurking in the shadows.";
                        color = "red";
                        break;

                    default:
                        name = entity.Name;
                        description = !string.IsNullOrWhiteSpace(entity.Description)
                            ? entity.Description
                            : "I can't tell what this is.";
                        color = "cyan1";
                        break;
                }

                lookables.Add((name, description, color, entity));
            }

            // If there are no entities to look at, inform the user
            if (lookables.Count == 0)
            {
                ui.WriteLine("[dim]There is nothing to look at in detail in this room.[/]");
                ui.WriteLine();
                ui.Write("[dim]Press any key to continue...[/]");
                await ui.ReadKeyAsync();
                ui.Clear();
                ui.UpdateStatus(player.Health, player.Wealth, player.Name);
                return;
            }

            // Use the new ShowPickListAsync method to get the player's choice
            int selectedIndex = await ui.ShowPickListAsync(
                prompt: "What would you like to examine more closely?",
                items: lookables,
                displaySelector: item => item.name,
                colorSelector: item => item.color,
                cancelPrompt: "Cancel");

            // If the user cancelled (selectedIndex == -1), return
            if (selectedIndex == -1)
            {
                ui.Clear();
                ui.UpdateStatus(player.Health, player.Wealth, player.Name);
                return;
            }

            // Clear screen and show status bar before showing entity details
            ui.Clear();
            ui.UpdateStatus(player.Health, player.Wealth, player.Name);

            // Otherwise, show the selected item's details
            (string name, string description, string color, Entity entity) selected = lookables[selectedIndex];
            ui.WriteLine();
            ui.WriteLine($"[italic]You take a closer look at the [bold][{selected.color}]{selected.name}[/][/]...[/]");
            ui.WriteLine();

            // For TreasureChest, append the status after the description
            if (selected.entity is TreasureChest chestEntity)
            {
                string chestState = chestEntity.IsOpened ? "Opened" : (chestEntity.IsLocked ? "Locked" : "Unlocked");
                ui.WriteLine($"{selected.description}\n\nStatus: [violet]{chestState}[/]");
            }
            else if (selected.entity is Enemy enemyEntity)
            {
                ui.WriteLine(selected.description);
                ui.WriteLine();
                ui.WriteLine($"Stats: [red]Health[/]: {enemyEntity.Health}, [red]Strength[/]: {enemyEntity.Strength}");
            }
            else
            {
                ui.WriteLine(selected.description);
            }
            ui.WriteLine();
            ui.Write("[dim]Press any key to continue...[/]");
            await ui.ReadKeyAsync();
            ui.Clear();
            ui.UpdateStatus(player.Health, player.Wealth, player.Name);
        }
    }
}