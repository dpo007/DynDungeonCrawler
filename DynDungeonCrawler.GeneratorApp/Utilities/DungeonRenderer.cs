using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Helpers.UI;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.GeneratorApp.Utilities
{
    /// <summary>
    /// Renders a dungeon map using the provided user interface.
    /// </summary>
    public class DungeonRenderer
    {
        private readonly IUserInterface _ui;
        private readonly bool _useConsoleColors;

        /// <summary>
        /// Initializes a new instance of the DungeonRenderer class.
        /// </summary>
        /// <param name="ui">The user interface to use for rendering.</param>
        public DungeonRenderer(IUserInterface ui)
        {
            _ui = ui;
            // Use console colors for both ConsoleUserInterface and SpectreConsoleUserInterface
            _useConsoleColors = ui is ConsoleUserInterface || ui is SpectreConsoleUserInterface;
        }

        /// <summary>
        /// Renders a dungeon map with optional entity display.
        /// </summary>
        /// <param name="dungeon">The dungeon to render.</param>
        /// <param name="showEntities">Whether to show entities on the map.</param>
        public void RenderDungeon(Dungeon dungeon, bool showEntities)
        {
            // Find the room containing the magical lock pick
            Room? lockPickRoom = dungeon.Rooms.FirstOrDefault(r => r.Contents.Any(e => e.Type == EntityType.MagicalLockPick));

            // Get map cells using dungeon's helper method
            MapCellInfo[,] cells = dungeon.GetMapCells(showEntities);
            int mapWidth = cells.GetLength(0);
            int mapHeight = cells.GetLength(1);

            // Render the map
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    MapCellInfo cell = cells[x, y];
                    bool isLockPickRoom = lockPickRoom != null && cell.X == lockPickRoom.X && cell.Y == lockPickRoom.Y;

                    if (_useConsoleColors)
                    {
                        // Set the background color if this is the lock pick room
                        if (isLockPickRoom)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                        }

                        // Set the foreground color and write the appropriate symbol
                        switch (cell.CellType)
                        {
                            case MapCellType.Entrance:
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("E");
                                break;

                            case MapCellType.Exit:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("X");
                                break;

                            case MapCellType.TreasureChest:
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write("T");
                                break;

                            case MapCellType.Enemy:
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.Write("@");
                                break;

                            case MapCellType.TreasureAndEnemy:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write("&");
                                break;

                            case MapCellType.MainPath:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                string dirChar = "+";
                                switch (cell.MainPathDirection)
                                {
                                    case TravelDirection.North: dirChar = "^"; break;
                                    case TravelDirection.East: dirChar = ">"; break;
                                    case TravelDirection.South: dirChar = "v"; break;
                                    case TravelDirection.West: dirChar = "<"; break;
                                }
                                Console.Write(dirChar);
                                break;

                            case MapCellType.Room:
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.Write("#");
                                break;

                            case MapCellType.Empty:
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(".");
                                break;

                            default:
                                Console.Write(" ");
                                break;
                        }

                        // Reset colors after writing the character
                        Console.ResetColor();
                    }
                    else
                    {
                        // For non-console UIs, use brackets to highlight the lock pick room
                        if (isLockPickRoom)
                        {
                            _ui.Write("[");
                        }

                        // Write the appropriate symbol based on cell type
                        switch (cell.CellType)
                        {
                            case MapCellType.Entrance: _ui.Write("E"); break;
                            case MapCellType.Exit: _ui.Write("X"); break;
                            case MapCellType.TreasureChest: _ui.Write("T"); break;
                            case MapCellType.Enemy: _ui.Write("@"); break;
                            case MapCellType.TreasureAndEnemy: _ui.Write("&"); break;
                            case MapCellType.MainPath:
                                string dirChar = "+";
                                switch (cell.MainPathDirection)
                                {
                                    case TravelDirection.North: dirChar = "^"; break;
                                    case TravelDirection.East: dirChar = ">"; break;
                                    case TravelDirection.South: dirChar = "v"; break;
                                    case TravelDirection.West: dirChar = "<"; break;
                                }
                                _ui.Write(dirChar);
                                break;

                            case MapCellType.Room: _ui.Write("#"); break;
                            case MapCellType.Empty: _ui.Write("."); break;
                            default: _ui.Write(" "); break;
                        }

                        // Close the bracket for lock pick room in non-console UIs
                        if (isLockPickRoom)
                        {
                            _ui.Write("]");
                        }
                    }
                }

                // End the line
                _ui.WriteLine();
            }

            // Add a blank line after the map
            _ui.WriteLine();

            // Render legend AFTER the map
            if (_useConsoleColors)
            {
                _ui.WriteLine("Map Legend:");
                Console.ForegroundColor = ConsoleColor.Green;
                _ui.WriteLine(" E - Entrance");
                Console.ForegroundColor = ConsoleColor.Red;
                _ui.WriteLine(" X - Exit");
                if (showEntities)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    _ui.WriteLine(" T - Treasure");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    _ui.WriteLine(" @ - Enemy");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    _ui.WriteLine(" & - Treasure & Enemy");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    _ui.WriteLine(" ^ > v < - Main Path Direction");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                _ui.WriteLine(" # - Room");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                _ui.WriteLine(" . - Empty Space");

                // Show example of highlighted room
                Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" ");
                Console.ResetColor();
                _ui.WriteLine(" - Room with Magical Lock Pick");
            }
            else
            {
                // Fallback: plain text legend
                _ui.WriteLine("Map Legend:");
                _ui.WriteLine(" E - Entrance");
                _ui.WriteLine(" X - Exit");
                if (showEntities)
                {
                    _ui.WriteLine(" T - Treasure");
                    _ui.WriteLine(" @ - Enemy");
                    _ui.WriteLine(" & - Treasure & Enemy");
                }
                else
                {
                    _ui.WriteLine(" ^ > v < - Main Path Direction");
                }
                _ui.WriteLine(" # - Room");
                _ui.WriteLine(" . - Empty Space");
                _ui.WriteLine(" [X] - Room with Magical Lock Pick (X varies by room type)");
            }
        }
    }
}