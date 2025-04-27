namespace DynDungeonCrawler.Engine.Classes
{
    public class Enemy : Entity
    {
        public int Health { get; set; }
        public int Attack { get; set; }

        public Enemy(string name = "Monster", int health = 10, int attack = 2)
            : base(EntityType.Enemy, name)
        {
            Health = health;
            Attack = attack;
        }
    }
}