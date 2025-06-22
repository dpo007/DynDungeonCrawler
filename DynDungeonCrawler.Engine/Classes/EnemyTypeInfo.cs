namespace DynDungeonCrawler.Engine.Classes
{
    public class EnemyTypeInfo
    {
        public string Name { get; }
        public string Description { get; }
        public string ShortDescription { get; }

        public EnemyTypeInfo(string name, string description, string shortDescription = "")
        {
            Name = name;
            Description = description;
            ShortDescription = shortDescription;
        }
    }
}