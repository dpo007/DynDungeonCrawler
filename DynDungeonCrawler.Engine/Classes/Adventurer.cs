namespace DynDungeonCrawler.Engine.Classes
{
    public class Adventurer : Entity
    {
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Wealth { get; private set; }
        public List<Entity> Inventory { get; private set; }
        public Room? CurrentRoom { get; set; }

        public Adventurer(string name, int health = 100, int attack = 10, int defense = 5)
            : base(EntityType.Adventurer, name)
        {
            Health = health;
            Attack = attack;
            Defense = defense;
            Wealth = 0;
            Inventory = new List<Entity>();
            CurrentRoom = null;
        }

        public void AddWealth(int amount)
        {
            if (amount > 0)
            {
                Wealth += amount;
                Console.WriteLine($"You gained {amount} gold! Total Wealth: {Wealth}");
            }
        }
    }
}