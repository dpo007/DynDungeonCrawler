using DynDungeonCrawler.ConDungeon.Combat;
using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Classes.Combat;
using DynDungeonCrawler.Engine.Factories;
using DynDungeonCrawler.Engine.Helpers.ContentGeneration;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon.GameLoop
{
    internal static class CommandProcessor
    {
        private static CombatService? _combatService;

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
            // Initialize combat service if needed
            _combatService ??= new CombatService(logger);

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
                await HandleLookCommandAsync(ui, logger, llmClient, dungeon, player);
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
                ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
                return true; // Continue the loop to allow further commands
            }
            else if (cmdChar == 'a')
            {
                // Attack/interact with enemy if present in the room
                await HandleAttackCommandAsync(ui, logger, llmClient, dungeon, player);
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
                    ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
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
        /// Handles the Attack command by initiating combat with an enemy in the current room.
        /// </summary>
        /// <param name="ui">The user interface for I/O operations.</param>
        /// <param name="logger">Logger for diagnostic and error messages.</param>
        /// <param name="llmClient">The LLM client for generating content.</param>
        /// <param name="dungeon">The dungeon instance containing the game state.</param>
        /// <param name="player">The current player.</param>
        /// <returns>A task that completes when the combat operation is finished.</returns>
        private static async Task HandleAttackCommandAsync(
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player)
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

            // Find enemies in the current room
            List<Enemy> enemies = player.CurrentRoom.Contents.OfType<Enemy>().ToList();

            if (enemies.Count == 0)
            {
                ui.WriteLine("[dim]There are no enemies to attack in this room.[/]");
                ui.WriteLine();
                ui.Write("[dim]Press any key to continue...[/]");
                await ui.ReadKeyAsync();
                ui.Clear();
                ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
                return;
            }

            // If only one enemy, engage directly
            if (enemies.Count == 1)
            {
                Enemy enemy = enemies[0];
                await InitiateCombatAsync(ui, logger, player, enemy);
            }
            else
            {
                // Multiple enemies, let player choose
                List<(string name, string description, string color, Enemy enemy)> combatables =
                    enemies.Select(e => (e.Name, e.Description, "red", e)).ToList();

                int selectedIndex = await ui.ShowPickListAsync(
                    prompt: "Which enemy do you want to attack?",
                    items: combatables,
                    displaySelector: item => item.name,
                    colorSelector: item => item.color,
                    cancelPrompt: "Cancel");

                if (selectedIndex == -1)
                {
                    ui.Clear();
                    ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
                    return;
                }

                Enemy selectedEnemy = combatables[selectedIndex].enemy;
                await InitiateCombatAsync(ui, logger, player, selectedEnemy);
            }
        }

        /// <summary>
        /// Initiates combat between the player and an enemy.
        /// </summary>
        /// <param name="ui">The user interface for I/O operations.</param>
        /// <param name="logger">Logger for diagnostic and error messages.</param>
        /// <param name="player">The player adventurer.</param>
        /// <param name="enemy">The enemy to fight.</param>
        /// <returns>A task that completes when combat is finished.</returns>
        private static async Task InitiateCombatAsync(
            IUserInterface ui,
            ILogger logger,
            Adventurer player,
            Enemy enemy)
        {
            if (_combatService == null)
            {
                logger.Log("Combat service not initialized");
                return;
            }

            // Create the appropriate combat presenter based on UI implementation
            ICombatPresenter presenter = ui.GetType().Name.Contains("Spectre")
                ? new SpectreConsoleCombatPresenter(ui)
                : new PlainConsoleCombatPresenter(ui);

            // Execute combat with the presenter
            CombatSummary result = await _combatService.ExecuteCombatAsync(player, enemy, presenter);

            // Process combat results
            if (result.Outcome == CombatOutcome.PlayerVictory)
            {
                // Remove defeated enemy from room
                if (player.CurrentRoom != null)
                {
                    player.CurrentRoom.Contents.Remove(enemy);
                }
            }

            // Update UI after combat
            ui.Clear();
            ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
        }

        /// <summary>
        /// Handles the Look command by presenting a menu of things to look at in detail,
        /// including each entity in the room (but not the room itself).
        /// </summary>
        /// <param name="ui">The user interface for I/O operations.</param>
        /// <param name="logger">Logger for diagnostic and error messages.</param>
        /// <param name="llmClient">The LLM client for generating content.</param>
        /// <param name="dungeon">The dungeon instance containing the game state.</param>
        /// <param name="player">The current player.</param>
        /// <returns>A task that completes when the look operation is finished.</returns>
        private static async Task HandleLookCommandAsync(
            IUserInterface ui,
            ILogger logger,
            ILLMClient llmClient,
            Dungeon dungeon,
            Adventurer player)
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

                    case MagicalLockPick lockPick:
                        name = lockPick.Name;
                        description = !string.IsNullOrWhiteSpace(lockPick.Description)
                            ? lockPick.Description
                            : "A magical tool that can unlock any chest.";
                        color = "purple";
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
                ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
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
                ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
                return;
            }

            // Clear screen and show status bar before showing entity details
            ui.Clear();
            ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);

            // Otherwise, show the selected item's details
            (string name, string description, string color, Entity entity) selected = lookables[selectedIndex];
            ui.WriteLine();
            ui.WriteLine($"[italic]You take a closer look at the [bold][{selected.color}]{selected.name}[/][/]...[/]");
            ui.WriteLine();

            // For TreasureChest, prompt to open
            if (selected.entity is TreasureChest chestEntity)
            {
                // Update the chest's guarded status based on current room enemies
                List<Enemy> enemies = player.CurrentRoom.GetEnemies();
                chestEntity.UpdateGuardedStatus(enemies.Count > 0);

                string chestStatusMessage = chestEntity.GetStatusMessage(enemies);
                ui.WriteLine($"{selected.description}");
                ui.WriteLine();
                ui.WriteLine(chestStatusMessage);

                // Prompt to open chest if not already opened and not guarded
                if (!chestEntity.IsOpened)
                {
                    // Check if chest is guarded by enemies
                    if (chestEntity.IsGuarded)
                    {
                        // Don't show additional redundant messages - the status message above already explains the situation
                    }
                    else
                    {
                        // Check if chest is locked and player has a magical lock pick
                        bool hasLockPick = false;
                        MagicalLockPick? lockPick = null;

                        if (chestEntity.IsLocked)
                        {
                            lockPick = player.Inventory.OfType<MagicalLockPick>().FirstOrDefault();
                            hasLockPick = lockPick != null;

                            if (hasLockPick)
                            {
                                ui.WriteLine($"You have a [purple]{lockPick!.Name}[/] in your inventory that could unlock this chest.");
                                ui.WriteLine();
                            }
                        }

                        // Present open options
                        if (chestEntity.IsLocked && hasLockPick)
                        {
                            ui.Write($"[bold]Use [purple]{lockPick!.Name}[/] to unlock chest?[/] [[[green]Y[/]]]es / [[[red]N[/]]]o [gray](default: N)[/]: ");
                            string keyStr = await ui.ReadKeyAsync(intercept: true);

                            if (keyStr.Equals("Y", StringComparison.OrdinalIgnoreCase))
                            {
                                ui.WriteLine("[green]Y[/]");
                                ui.WriteLine();

                                // Use the lock pick to unlock the chest
                                bool success = lockPick.UseOn(chestEntity);

                                if (success)
                                {
                                    string unlockMessage = "[italic]You insert the magical pick into the lock. The runes along its surface glow with arcane energy, and with a satisfying click, the chest unlocks.[/]";
                                    await ui.WriteSlowlyBySentenceAsync(unlockMessage, pauseMs: 1500);
                                    ui.WriteLine();

                                    // Ask if they want to open the now unlocked chest
                                    ui.Write("[bold]Open the unlocked chest?[/] [[[green]Y[/]]]es / [[[red]N[/]]]o [gray](default: N)[/]: ");
                                    keyStr = await ui.ReadKeyAsync(intercept: true);

                                    if (keyStr.Equals("Y", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ui.WriteLine("[green]Y[/]");
                                        ui.WriteLine();

                                        try
                                        {
                                            // Display a thematic chest opening story
                                            string openingStory = dungeon.GetRandomChestOpeningStory();
                                            await ui.WriteSlowlyBySentenceAsync($"[italic]{openingStory}[/]");

                                            // Open the chest and award treasure
                                            chestEntity.Open();
                                            int value = chestEntity.ContainedTreasure?.Value ?? 0;
                                            player.AddWealth(value);
                                            ui.WriteLine($"[bold green]You find treasure worth [gold1]{value}[/] coins![/]");
                                        }
                                        catch (InvalidOperationException ex)
                                        {
                                            ui.WriteLine($"[bold red]{ex.Message}[/]");
                                        }
                                    }
                                    else
                                    {
                                        ui.WriteLine("[red]N[/]");
                                        ui.WriteLine("You decide not to open the chest yet.");
                                    }
                                }
                                else
                                {
                                    ui.WriteLine("[red]For some reason, the magical lock pick doesn't work on this chest.[/]");
                                }
                            }
                            else
                            {
                                ui.WriteLine("[red]N[/]");
                                ui.WriteLine("You decide not to use the magical lock pick.");
                            }
                        }
                        else
                        {
                            // Standard open prompt for unlocked chests
                            ui.Write("[bold]Open chest?[/] [[[green]Y[/]]]es / [[[red]N[/]]]o [gray](default: N)[/]: ");
                            string keyStr = await ui.ReadKeyAsync(intercept: true);

                            if (string.IsNullOrEmpty(keyStr) || keyStr == "\r" || keyStr == "\n" || keyStr.Equals("N", StringComparison.OrdinalIgnoreCase))
                            {
                                ui.WriteLine("[red]N[/]");
                                ui.WriteLine("You decide not to open the chest.");
                            }
                            else if (keyStr.Equals("Y", StringComparison.OrdinalIgnoreCase))
                            {
                                ui.WriteLine("[green]Y[/]");
                                ui.WriteLine();

                                if (chestEntity.IsLocked)
                                {
                                    ui.WriteLine("[bold red]The chest is locked. You cannot open it.[/]");
                                }
                                else
                                {
                                    try
                                    {
                                        // Display a thematic chest opening story with the new dramatic sentence-by-sentence display
                                        string openingStory = dungeon.GetRandomChestOpeningStory();
                                        await ui.WriteSlowlyBySentenceAsync($"[italic]{openingStory}[/]");

                                        // Actually open the chest and award treasure
                                        chestEntity.Open();
                                        int value = chestEntity.ContainedTreasure?.Value ?? 0;
                                        player.AddWealth(value);
                                        ui.WriteLine($"[bold green]You find treasure worth [gold1]{value}[/] coins![/]");
                                    }
                                    catch (InvalidOperationException ex)
                                    {
                                        ui.WriteLine($"[bold red]{ex.Message}[/]");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ui.WriteLine("[dim]This chest is already opened.[/]");
                }
            }
            else if (selected.entity is Enemy enemyEntity)
            {
                ui.WriteLine(selected.description);
                ui.WriteLine();
                ui.WriteLine($"Stats: [red]Health[/]: {enemyEntity.Health}, [red]Strength[/]: {enemyEntity.Strength}, [blue]Defense[/]: {enemyEntity.Defense}");
                ui.WriteLine();

                // Option to attack the enemy
                ui.Write("[bold]Attack this enemy?[/] [[[green]Y[/]]]es / [[[red]N[/]]]o [gray](default: N)[/]: ");
                string keyStr = await ui.ReadKeyAsync(intercept: true);

                if (keyStr.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    ui.WriteLine("[green]Y[/]");
                    ui.WriteLine();

                    // Initiate combat with this enemy
                    if (_combatService != null)
                    {
                        await InitiateCombatAsync(ui, logger, player, enemyEntity);
                    }
                    else
                    {
                        ui.WriteLine("[red]Combat system not initialized.[/]");
                        ui.WriteLine();
                        ui.Write("[dim]Press any key to continue...[/]");
                        await ui.ReadKeyAsync();
                    }
                }
                else
                {
                    ui.WriteLine("[red]N[/]");
                    ui.WriteLine("You decide not to attack the enemy.");
                }
            }
            else if (selected.entity is MagicalLockPick lockPickEntity)
            {
                ui.WriteLine(selected.description);
                ui.WriteLine();
                ui.Write("[bold]Pick up the Magical Lock Pick?[/] [[[green]Y[/]]]es / [[[red]N[/]]]o [gray](default: N)[/]: ");

                string keyStr = await ui.ReadKeyAsync(intercept: true);
                if (keyStr.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    ui.WriteLine("[green]Y[/]");
                    ui.WriteLine();

                    if (player.PickUpEntity(lockPickEntity))
                    {
                        ui.WriteLine($"[bold green]You add the [purple]{lockPickEntity.Name}[/] to your inventory.[/]");
                    }
                    else
                    {
                        ui.WriteLine("[red]You were unable to pick up the item.[/]");
                    }
                }
                else
                {
                    ui.WriteLine("[red]N[/]");
                    ui.WriteLine("You leave it where it is.");
                }
            }
            else
            {
                ui.WriteLine(selected.description);
            }
            ui.WriteLine();
            ui.Write("[dim]Press any key to continue...[/]");
            await ui.ReadKeyAsync();
            ui.Clear();
            ui.UpdateStatus(player.Name, player.Strength, player.Defense, player.Health);
        }
    }
}