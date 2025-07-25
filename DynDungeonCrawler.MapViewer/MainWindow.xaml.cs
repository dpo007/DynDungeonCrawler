﻿using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Helpers.LLM;
using DynDungeonCrawler.Engine.Helpers.Logging;
using DynDungeonCrawler.Engine.Interfaces;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DynDungeonCrawler.MapViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // The currently loaded dungeon instance
    private Dungeon? _dungeon;

    // Logger for diagnostic output (file logger in viewer)
    private ILogger _logger;

    // Dummy LLM client (no LLM calls needed for map viewing)
    private ILLMClient _llmClient = new DummyLLMClient();

    // Path to the currently loaded dungeon file
    private string? _currentFilePath;

    // Internal ScrollViewers for scroll syncing
    private ScrollViewer? _scrollViewerPaths;

    private ScrollViewer? _scrollViewerEntities;
    private bool _syncingScroll = false; // Prevents recursive scroll events

    public MainWindow()
    {
        // Remove MapViewerSettings usage, use default log path
        string logFilePath = @"C:\temp\MapViewer.log";
        _logger = new FileLogger(logFilePath);
        InitializeComponent();
        Loaded += MainWindow_Loaded; // Attach loaded event handler
    }

    // On window loaded, hook up scroll sync events for FlowDocumentScrollViewer's internal ScrollViewer
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewerPaths = GetDescendantScrollViewer(MapDisplayPaths);
        _scrollViewerEntities = GetDescendantScrollViewer(MapDisplayEntities);
        if (_scrollViewerPaths != null)
        {
            _scrollViewerPaths.ScrollChanged += ScrollViewer_ScrollChanged;
        }

        if (_scrollViewerEntities != null)
        {
            _scrollViewerEntities.ScrollChanged += ScrollViewer_ScrollChanged;
        }
    }

    // Helper to get the internal ScrollViewer of a FlowDocumentScrollViewer
    private static ScrollViewer? GetDescendantScrollViewer(DependencyObject d)
    {
        if (d is ScrollViewer sv)
        {
            return sv;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(d, i);
            ScrollViewer? result = GetDescendantScrollViewer(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // Synchronize vertical/horizontal scrolling between the two map views
    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_syncingScroll)
        {
            return;
        }

        if (_scrollViewerPaths == null || _scrollViewerEntities == null)
        {
            return;
        }

        _syncingScroll = true;
        if (sender == _scrollViewerPaths)
        {
            _scrollViewerEntities.ScrollToVerticalOffset(_scrollViewerPaths.VerticalOffset);
            _scrollViewerEntities.ScrollToHorizontalOffset(_scrollViewerPaths.HorizontalOffset);
        }
        else if (sender == _scrollViewerEntities)
        {
            _scrollViewerPaths.ScrollToVerticalOffset(_scrollViewerEntities.VerticalOffset);
            _scrollViewerPaths.ScrollToHorizontalOffset(_scrollViewerEntities.HorizontalOffset);
        }
        _syncingScroll = false;
    }

    // Handle the Load Dungeon button click
    private void BtnLoadDungeon_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Filter = "Dungeon JSON (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open Dungeon JSON File"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _currentFilePath = dlg.FileName;
                _llmClient = new DummyLLMClient(); // No LLM needed for map display
                // Load the dungeon from JSON file
                _dungeon = Dungeon.LoadFromJson(_currentFilePath, _llmClient, _logger);
                UpdateDungeonInfoUI(); // Update theme/room count
                ShowMaps(); // Render maps
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dungeon: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Update the theme and room count UI elements
    private void UpdateDungeonInfoUI()
    {
        if (_dungeon != null)
        {
            TxtDungeonTheme.Text = _dungeon.Theme; // Show dungeon theme
            TxtRoomCount.Text = $"Rooms: {_dungeon.Rooms.Count}"; // Show room count
        }
        else
        {
            TxtDungeonTheme.Text = "Dungeon Theme: (none)";
            TxtRoomCount.Text = "Rooms: 0";
        }
    }

    // Render the map displays in both modes (paths only, with entities)
    private void ShowMaps()
    {
        if (MapDisplayPaths == null || MapDisplayEntities == null)
        {
            return;
        }

        if (_dungeon == null)
        {
            // Show placeholder if no dungeon loaded
            MapDisplayPaths.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            MapDisplayEntities.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            return;
        }
        // Always highlight the magical lock pick room
        FlowDocument docPaths = BuildColoredMapDocument(_dungeon, false, true);
        docPaths.PageWidth = 420; // Use a smaller static value for max map width
        MapDisplayPaths.Document = docPaths;

        FlowDocument docEntities = BuildColoredMapDocument(_dungeon, true, true);
        docEntities.PageWidth = 420;
        MapDisplayEntities.Document = docEntities;
    }

    // Build a FlowDocument for the map, coloring each cell appropriately
    private static FlowDocument BuildColoredMapDocument(Dungeon dungeon, bool showEntities, bool highlightLockPick = false)
    {
        FlowDocument doc = new FlowDocument();
        doc.Background = Brushes.Black;
        doc.FontFamily = new FontFamily("Consolas"); // Ensure monospaced font
        Paragraph para = new Paragraph { Margin = new Thickness(0) };
        para.FontFamily = new FontFamily("Consolas"); // Ensure monospaced font

        MapCellInfo[,] cells = dungeon.GetMapCells(showEntities);
        int mapWidth = cells.GetLength(0);
        int mapHeight = cells.GetLength(1);

        Room[,] roomGrid = dungeon.Grid;

        // Find the room containing the magical lock pick
        Room? lockPickRoom = null;
        if (highlightLockPick)
        {
            lockPickRoom = dungeon.Rooms.FirstOrDefault(r => r.Contents.Any(e => e.Type == EntityType.MagicalLockPick));
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                MapCellInfo cell = cells[x, y];
                char ch = cell.Symbol;

                SolidColorBrush color = cell.CellType switch
                {
                    MapCellType.Entrance => Brushes.Green,
                    MapCellType.Exit => Brushes.Red,
                    MapCellType.TreasureChest => Brushes.Cyan,
                    MapCellType.Enemy => Brushes.Magenta,
                    MapCellType.TreasureAndEnemy => Brushes.Yellow,
                    MapCellType.MainPath => Brushes.Yellow,
                    MapCellType.Room => Brushes.Gray,
                    MapCellType.Empty => Brushes.DarkGray,
                    _ => Brushes.Gray
                };

                Room? room = null;
                int rx = cell.X;
                int ry = cell.Y;
                if (rx >= 0 && rx < roomGrid.GetLength(0) && ry >= 0 && ry < roomGrid.GetLength(1))
                {
                    room = roomGrid[rx, ry];
                }

                Run run = new Run(ch.ToString()) { Foreground = color };

                // Highlight the magical lock pick room with a gold background
                if (highlightLockPick && room != null && lockPickRoom != null && room.X == lockPickRoom.X && room.Y == lockPickRoom.Y)
                {
                    run.Background = Brushes.Gold;
                }

                if (room != null)
                {
                    // Entities section
                    string entityList;
                    if (room.Contents != null && room.Contents.Count > 0)
                    {
                        entityList = string.Join("\n", room.Contents.Select(e =>
                        {
                            string type = e.GetType().Name;
                            if (type == "TreasureChest" && e is TreasureChest chest)
                            {
                                return chest.IsLocked
                                    ? "- Treasure Chest: Locked"
                                    : "- Treasure Chest: Unlocked";
                            }
                            else if (type == "Enemy")
                            {
                                return $"- Enemy: {e.Name}";
                            }
                            else if (!string.IsNullOrWhiteSpace(e.Name))
                            {
                                return $"- {type}: {e.Name}";
                            }
                            else
                            {
                                return $"- {type}";
                            }
                        }));
                    }
                    else
                    {
                        entityList = "- None";
                    }

                    // Doors section
                    string?[] doors =
                    [
                        room.ConnectedNorth ? "N" : null,
                        room.ConnectedEast  ? "E" : null,
                        room.ConnectedSouth ? "S" : null,
                        room.ConnectedWest  ? "W" : null
                    ];
                    string doorsList = string.Join(", ", doors.Where(d => d != null));
                    if (string.IsNullOrEmpty(doorsList))
                    {
                        doorsList = "None";
                    }

                    run.ToolTip = $"Coords: ({room.X},{room.Y})\nDoors: {doorsList}\nEntities:\n{entityList}";
                }
                else
                {
                    run.ToolTip = $"Coords: (n/a)\nDoors: None\nEntities:\n- None";
                }

                para.Inlines.Add(run);
            }
            para.Inlines.Add(new Run("\n"));
        }

        // Add legend at the bottom
        para.Inlines.Add(new Run("\nLegend:\n") { Foreground = Brushes.White });
        foreach (MapLegendEntry entry in MapLegend.GetLegend(showEntities))
        {
            SolidColorBrush color = entry.CellType switch
            {
                MapCellType.Entrance => Brushes.Green,
                MapCellType.Exit => Brushes.Red,
                MapCellType.TreasureChest => Brushes.Cyan,
                MapCellType.Enemy => Brushes.Magenta,
                MapCellType.TreasureAndEnemy => Brushes.Yellow,
                MapCellType.MainPath => Brushes.Yellow,
                MapCellType.Room => Brushes.Gray,
                MapCellType.Empty => Brushes.DarkGray,
                _ => Brushes.Gray
            };
            para.Inlines.Add(new Run($" {entry.Symbol} = {entry.Description}\n") { Foreground = color });
        }
        if (highlightLockPick)
        {
            para.Inlines.Add(new Run(" █ = Room with Magical Lock Pick\n") { Foreground = Brushes.Gold });
        }
        doc.Blocks.Add(para);
        return doc;
    }
}