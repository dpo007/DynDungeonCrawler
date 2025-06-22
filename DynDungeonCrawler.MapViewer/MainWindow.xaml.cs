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
        int width = dungeon.Grid.GetLength(0);
        int height = dungeon.Grid.GetLength(1);
        int minX = width, minY = height, maxX = 0, maxY = 0;
        foreach (var room in dungeon.Rooms)
        {
            if (room.X < minX) minX = room.X;
            if (room.Y < minY) minY = room.Y;
            if (room.X > maxX) maxX = room.X;
            if (room.Y > maxY) maxY = room.Y;
        }
        var mainPathDirections = new System.Collections.Generic.Dictionary<(int, int), Engine.Classes.TravelDirection>();
        var entrance = dungeon.Rooms.FirstOrDefault(r => r.Type == Engine.Classes.RoomType.Entrance);
        if (entrance != null && !showEntities)
        {
            var method = typeof(Dungeon).GetMethod("FindMainPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(dungeon, new object[] { entrance, mainPathDirections });
        }
        for (int y = minY - 2; y <= maxY + 2; y++)
        {
            for (int x = minX - 2; x <= maxX + 2; x++)
            {
                var room = (x >= 0 && y >= 0 && x < width && y < height) ? dungeon.Grid[x, y] : null;
                char ch;
                SolidColorBrush color;
                if (room == null)
                {
                    ch = '.';
                    color = Brushes.DarkGray;
                }
                else if (room.Type == Engine.Classes.RoomType.Entrance)
                {
                    ch = 'E';
                    color = Brushes.Green;
                }
                else if (room.Type == Engine.Classes.RoomType.Exit)
                {
                    ch = 'X';
                    color = Brushes.Red;
                }
                else if (showEntities && room.Contents.Any(c => c.Type.ToString() == "TreasureChest"))
                {
                    ch = 'T';
                    color = Brushes.Cyan;
                }
                else if (showEntities && room.Contents.Any(c => c.Type.ToString() == "Enemy"))
                {
                    ch = '@';
                    color = Brushes.Magenta;
                }
                else if (!showEntities && mainPathDirections.ContainsKey((room.X, room.Y)))
                {
                    switch (mainPathDirections[(room.X, room.Y)])
                    {
                        case Engine.Classes.TravelDirection.North: ch = '^'; color = Brushes.Yellow; break;
                        case Engine.Classes.TravelDirection.East: ch = '>'; color = Brushes.Yellow; break;
                        case Engine.Classes.TravelDirection.South: ch = 'v'; color = Brushes.Yellow; break;
                        case Engine.Classes.TravelDirection.West: ch = '<'; color = Brushes.Yellow; break;
                        case Engine.Classes.TravelDirection.None: ch = '+'; color = Brushes.Yellow; break;
                        default: ch = '#'; color = Brushes.Gray; break;
                    }
                }
                else
                {
                    ch = '#';
                    color = Brushes.Gray;
                }
                para.Inlines.Add(new Run(ch.ToString()) { Foreground = color });
            }
            para.Inlines.Add(new Run("\n"));
        }
        // Legend
        para.Inlines.Add(new Run("\nLegend:\n") { Foreground = Brushes.White });
        para.Inlines.Add(new Run(" E = Entrance\n") { Foreground = Brushes.Green });
        para.Inlines.Add(new Run(" X = Exit\n") { Foreground = Brushes.Red });
        if (showEntities)
        {
            para.Inlines.Add(new Run(" T = Treasure Chest\n") { Foreground = Brushes.Cyan });
            para.Inlines.Add(new Run(" @ = Enemy\n") { Foreground = Brushes.Magenta });
        }
        else
        {
            para.Inlines.Add(new Run(" ^ > v < = Main Path Direction\n") { Foreground = Brushes.Yellow });
        }
        para.Inlines.Add(new Run(" # = Room\n") { Foreground = Brushes.Gray });
        para.Inlines.Add(new Run(" . = Empty Space\n") { Foreground = Brushes.DarkGray });
        doc.Blocks.Add(para);
        return doc;
    }

    // Dummy LLM client for map viewing (no LLM calls needed)
    private class DummyLLMClient : ILLMClient
    {
        public Task<string> GetResponseAsync(string userPrompt, string systemPrompt = "") => Task.FromResult("");
    }
}