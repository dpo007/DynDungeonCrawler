namespace DynDungeonCrawler.Engine.Classes
{
    public enum TreasureType
    {
        Money,
        Gold,
        Jewels
    }

    public class Treasure
    {
        public TreasureType Type { get; set; }
        public int Value { get; set; }

        public Treasure(TreasureType type, int value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Type} worth {Value} gold";
        }
    }
}