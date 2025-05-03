namespace DynDungeonCrawler.Engine.Classes
{
    public class Enemy : Entity
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int MoneyReward { get; private set; } = 0;

        private static readonly Random random = Random.Shared;

        public Enemy(string name = "Monster", int health = 10, int attack = 2)
            : base(EntityType.Enemy, name)
        {
            Health = health;
            Attack = attack;
            MoneyReward = GenerateMoneyReward();
        }

        private static int GenerateMoneyReward()
        {
            double roll = random.NextDouble();

            if (roll < 0.7)
                return 0; // 70% chance no money
            else if (roll < 0.9)
                return random.Next(1, 11); // 1-10 gold
            else
                return random.Next(10, 51); // 10-50 gold
        }
    }
}
