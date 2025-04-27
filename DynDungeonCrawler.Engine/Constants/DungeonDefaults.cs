namespace DynDungeonCrawler.Engine.Constants
{
    public static class DungeonDefaults
    {
        public static string DefaultDungeonDescription { get; } = "A D&D style labyrinth filled with ancient secrets.";
        public static int DefaultEscapePathLength { get; } = 10; // Minimum rooms from Entrance to Exit
        public static int MaxDungeonWidth { get; } = 100; // Maximum width of the dungeon
        public static int MaxDungeonHeight { get; } = 100; // Maximum height of the dungeon
    }
}
