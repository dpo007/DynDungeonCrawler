namespace DynDungeonCrawler.Engine.Data
{
    public class DungeonData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Theme { get; set; }
        public List<RoomData> Rooms { get; set; }

        public DungeonData()
        {
            Theme = string.Empty;
            Rooms = new List<RoomData>();
        }
    }
}