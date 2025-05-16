namespace DynDungeonCrawler.Engine.Classes
{
    public class EnemyTypeInfo
    {
        public string Name { get; }
        public string Description { get; }

        public EnemyTypeInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}