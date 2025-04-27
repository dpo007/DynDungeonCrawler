namespace DynDungeonCrawler.Engine.Classes
{
    public class TreasureChest : Entity
    {
        public bool IsLocked { get; set; } = false;
        public bool IsOpened { get; set; } = false;

        public Treasure? ContainedTreasure { get; private set; } = null;

        private static Random random = new Random();

        public TreasureChest(string name = "Treasure Chest", bool isLocked = false)
            : base(EntityType.TreasureChest, name)
        {
            IsLocked = isLocked;
            GenerateTreasure();
        }

        private void GenerateTreasure()
        {
            double roll = random.NextDouble();

            if (roll < 0.6)
            {
                ContainedTreasure = new Treasure(TreasureType.Money, random.Next(10, 501));
            }
            else if (roll < 0.9)
            {
                ContainedTreasure = new Treasure(TreasureType.Gold, random.Next(500, 5001));
            }
            else
            {
                ContainedTreasure = new Treasure(TreasureType.Jewels, random.Next(1000, 10001));
            }
        }
    }
}