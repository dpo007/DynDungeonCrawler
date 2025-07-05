namespace DynDungeonCrawler.Engine.Classes
{
    public enum MapCellType
    {
        Empty,
        Room,
        Entrance,
        Exit,
        MainPath,
        TreasureChest,
        Enemy,
        TreasureAndEnemy // Added for rooms with both treasure and enemy
    }

    public class MapCellInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; set; } = '.';
        public MapCellType CellType { get; set; } = MapCellType.Empty;
        public DynDungeonCrawler.Engine.Classes.TravelDirection? MainPathDirection { get; set; } = null;
    }

    public class MapLegendEntry
    {
        public char Symbol { get; set; }
        public MapCellType CellType { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public static class MapLegend
    {
        public static List<MapLegendEntry> GetLegend(bool showEntities)
        {
            List<MapLegendEntry> legend = new List<MapLegendEntry>
            {
                new MapLegendEntry { Symbol = 'E', CellType = MapCellType.Entrance, Description = "Entrance" },
                new MapLegendEntry { Symbol = 'X', CellType = MapCellType.Exit, Description = "Exit" },
            };
            if (showEntities)
            {
                legend.Add(new MapLegendEntry { Symbol = 'T', CellType = MapCellType.TreasureChest, Description = "Treasure Chest" });
                legend.Add(new MapLegendEntry { Symbol = '@', CellType = MapCellType.Enemy, Description = "Enemy" });
                legend.Add(new MapLegendEntry { Symbol = '&', CellType = MapCellType.TreasureAndEnemy, Description = "Treasure & Enemy" }); // Add after T and @
            }
            else
            {
                legend.Add(new MapLegendEntry { Symbol = '^', CellType = MapCellType.MainPath, Description = "Main Path Direction" });
                legend.Add(new MapLegendEntry { Symbol = '>', CellType = MapCellType.MainPath, Description = "Main Path Direction" });
                legend.Add(new MapLegendEntry { Symbol = 'v', CellType = MapCellType.MainPath, Description = "Main Path Direction" });
                legend.Add(new MapLegendEntry { Symbol = '<', CellType = MapCellType.MainPath, Description = "Main Path Direction" });
            }
            legend.Add(new MapLegendEntry { Symbol = '#', CellType = MapCellType.Room, Description = "Room" });
            legend.Add(new MapLegendEntry { Symbol = '.', CellType = MapCellType.Empty, Description = "Empty Space" });
            return legend;
        }
    }
}