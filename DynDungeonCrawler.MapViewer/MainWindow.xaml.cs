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
    private Dungeon? _dungeon;
    private ILogger _logger = new ConsoleLogger();
    private ILLMClient _llmClient = new DummyLLMClient();
    private string? _currentFilePath;

    private ScrollViewer? _scrollViewerPaths;
    private ScrollViewer? _scrollViewerEntities;
    private bool _syncingScroll = false;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewerPaths = GetScrollViewer(RtbMapDisplayPaths);
        _scrollViewerEntities = GetScrollViewer(RtbMapDisplayEntities);
        if (_scrollViewerPaths != null)
            _scrollViewerPaths.ScrollChanged += ScrollViewer_ScrollChanged;
        if (_scrollViewerEntities != null)
            _scrollViewerEntities.ScrollChanged += ScrollViewer_ScrollChanged;
    }

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
                _dungeon = Dungeon.LoadFromJson(_currentFilePath, _llmClient, _logger);
                ShowMaps();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dungeon: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ShowMaps()
    {
        if (RtbMapDisplayPaths == null || RtbMapDisplayEntities == null)
            return;
        if (_dungeon == null)
        {
            RtbMapDisplayPaths.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            RtbMapDisplayEntities.Document = new FlowDocument(new Paragraph(new Run("No dungeon loaded.")));
            return;
        }
        var docPaths = BuildColoredMapDocument(_dungeon, false);
        docPaths.PageWidth = 200; // Use a smaller static value for max map width
        RtbMapDisplayPaths.Document = docPaths;

        var docEntities = BuildColoredMapDocument(_dungeon, true);
        docEntities.PageWidth = 200;
        RtbMapDisplayEntities.Document = docEntities;
    }

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
            para.Inlines.Add(new Run("\n"));
        }
        // Legend
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