namespace DynDungeonCrawler.Engine.Constants
{
    public static class DungeonDefaults
    {
        public static int DefaultEscapePathLength { get; } = 15; // Minimum rooms from Entrance to Exit

        public static int MaxDungeonWidth { get; } = 100; // Maximum width of the dungeon
        public static int MaxDungeonHeight { get; } = 100; // Maximum height of the dungeon
    }
}