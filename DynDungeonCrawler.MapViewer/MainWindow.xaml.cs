using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers;
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
    // Logger for diagnostic output (console only in viewer)
    private ILogger _logger = new ConsoleLogger();
    // Dummy LLM client (no LLM calls needed for map viewing)
    private ILLMClient _llmClient = new DummyLLMClient();
    // Path to the currently loaded dungeon file
    private string? _currentFilePath;

    // ScrollViewers for map display RichTextBoxes (for synchronized scrolling)
    private ScrollViewer? _scrollViewerPaths;
    private ScrollViewer? _scrollViewerEntities;
    private bool _syncingScroll = false; // Prevents recursive scroll events

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded; // Attach loaded event handler
    }

    // On window loaded, find ScrollViewers and hook up scroll sync events
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewerPaths = GetScrollViewer(RtbMapDisplayPaths);
        _scrollViewerEntities = GetScrollViewer(RtbMapDisplayEntities);
        if (_scrollViewerPaths != null)
            _scrollViewerPaths.ScrollChanged += ScrollViewer_ScrollChanged;
        if (_scrollViewerEntities != null)
            _scrollViewerEntities.ScrollChanged += ScrollViewer_ScrollChanged;
    }

    // Recursively search for a ScrollViewer in the visual tree
    private ScrollViewer? GetScrollViewer(DependencyObject o)
    {
        if (o is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
        {
            var child = VisualTreeHelper.GetChild(o, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    // Synchronize vertical/horizontal scrolling between the two map views
    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_syncingScroll) return;
        _syncingScroll = true;
        if (sender == _scrollViewerPaths && _scrollViewerEntities != null)
        {
            _scrollViewerEntities.ScrollToVerticalOffset(_scrollViewerPaths.VerticalOffset);
            _scrollViewerEntities.ScrollToHorizontalOffset(_scrollViewerPaths.HorizontalOffset);
        }
        else if (sender == _scrollViewerEntities && _scrollViewerPaths != null)
        {
            _scrollViewerPaths.ScrollToVerticalOffset(_scrollViewerEntities.VerticalOffset);
            _scrollViewerPaths.ScrollToHorizontalOffset(_scrollViewerEntities.HorizontalOffset);
        }
        _syncingScroll = false;
    }

    // Handle the Load Dungeon button click
    private void BtnLoadDungeon_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Dungeon JSON (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open Dungeon JSON File"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _currentFilePath = dlg.FileName;
                var settings = Settings.Load();
                _llmClient = new DummyLLMClient(); // No LLM needed for map display
                _logger = new ConsoleLogger();
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
        if (RtbMapDisplayPaths == null || RtbMapDisplayEntities == null)
            return;
        if (_dungeon == null)
        {
            // Show placeholder if no dungeon loaded
            RtbMapDisplayPaths.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            RtbMapDisplayEntities.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            return;
        }
        // Render map (paths only)
        var docPaths = BuildColoredMapDocument(_dungeon, false);
        docPaths.PageWidth = 420; // Use a smaller static value for max map width
        RtbMapDisplayPaths.Document = docPaths;

        // Render map (with entities)
        var docEntities = BuildColoredMapDocument(_dungeon, true);
        docEntities.PageWidth = 420;
        RtbMapDisplayEntities.Document = docEntities;
    }

    // Build a FlowDocument for the map, coloring each cell appropriately
    private static FlowDocument BuildColoredMapDocument(Dungeon dungeon, bool showEntities)
    {
        var doc = new FlowDocument();
        doc.Background = Brushes.Black;
        var para = new Paragraph { Margin = new Thickness(0) };
        var cells = dungeon.GetMapCells(showEntities);
        int mapWidth = cells.GetLength(0);
        int mapHeight = cells.GetLength(1);
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                var cell = cells[x, y];
                char ch = cell.Symbol;
                // Choose color based on cell type
                SolidColorBrush color = cell.CellType switch
                {
                    MapCellType.Entrance => Brushes.Green,
                    MapCellType.Exit => Brushes.Red,
                    MapCellType.TreasureChest => Brushes.Cyan,
                    MapCellType.Enemy => Brushes.Magenta,
                    MapCellType.MainPath => Brushes.Yellow,
                    MapCellType.Room => Brushes.Gray,
                    MapCellType.Empty => Brushes.DarkGray,
                    _ => Brushes.Gray
                };
                para.Inlines.Add(new Run(ch.ToString()) { Foreground = color });
            }
            para.Inlines.Add(new Run("\n")); // Newline after each row
        }
        // Add legend at the bottom
        para.Inlines.Add(new Run("\nLegend:\n") { Foreground = Brushes.White });
        foreach (var entry in MapLegend.GetLegend(showEntities))
        {
            SolidColorBrush color = entry.CellType switch
            {
                MapCellType.Entrance => Brushes.Green,
                MapCellType.Exit => Brushes.Red,
                MapCellType.TreasureChest => Brushes.Cyan,
                MapCellType.Enemy => Brushes.Magenta,
                MapCellType.MainPath => Brushes.Yellow,
                MapCellType.Room => Brushes.Gray,
                MapCellType.Empty => Brushes.DarkGray,
                _ => Brushes.Gray
            };
            para.Inlines.Add(new Run($" {entry.Symbol} = {entry.Description}\n") { Foreground = color });
        }
        doc.Blocks.Add(para);
        return doc;
    }

    // Dummy LLM client for map viewing (no LLM calls needed)
    private class DummyLLMClient : ILLMClient
    {
        public Task<string> GetResponseAsync(string userPrompt, string systemPrompt = "") => Task.FromResult("");
    }
}