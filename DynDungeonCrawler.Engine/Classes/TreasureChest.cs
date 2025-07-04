namespace DynDungeonCrawler.Engine.Classes
{
    public class TreasureChest : Entity
    {
        public bool IsLocked { get; set; } = false;
        public bool IsOpened { get; set; } = false;

        public Treasure? ContainedTreasure { get; private set; } = null;

        private static readonly Random random = Random.Shared;

        /// <summary>
        /// Standard constructor that generates a random treasure.
        /// </summary>
        /// <param name="name">The name of the chest.</param>
        /// <param name="isLocked">Whether the chest is locked.</param>
        public TreasureChest(string name = "Treasure Chest", bool isLocked = false)
            : base(EntityType.TreasureChest, name)
        {
            IsLocked = isLocked;
            GenerateTreasure();
        }

        /// <summary>
        /// Deserialization constructor that allows setting a specific treasure.
        /// </summary>
        /// <param name="name">The name of the chest.</param>
        /// <param name="isLocked">Whether the chest is locked.</param>
        /// <param name="treasureType">The type of treasure.</param>
        /// <param name="treasureValue">The value of the treasure.</param>
        public TreasureChest(string name, bool isLocked, TreasureType treasureType, int treasureValue)
            : base(EntityType.TreasureChest, name)
        {
            IsLocked = isLocked;
            ContainedTreasure = new Treasure(treasureType, treasureValue);
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

        public void Open()
        {
            if (IsOpened)
            {
                throw new InvalidOperationException("Cannot open: chest is already opened.");
            }

            IsOpened = true;
        }
    }
}