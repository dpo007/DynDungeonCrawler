using DynDungeonCrawler.Engine.Classes;

namespace DynDungeonCrawler.Engine.Factories
{
    public static class EnemyFactory
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Creates an enemy based on the given name and optional parameters.
        /// </summary>
        /// <param name="name">The name of the enemy.</param>
        /// <param name="theme">The theme of the dungeon (optional).</param>
        /// <returns>A new Enemy object.</returns>
        public static Enemy CreateEnemy(string name, string? theme = null)
        {
            // Example: Adjust stats based on theme or name
            int health = random.Next(10, 21); // Random health between 10 and 20
            int attack = random.Next(2, 6);  // Random attack between 2 and 5

            if (theme != null)
            {
                if (theme.Contains("dark", StringComparison.OrdinalIgnoreCase))
                {
                    health += 5; // Dark-themed enemies are tougher
                }
                else if (theme.Contains("fire", StringComparison.OrdinalIgnoreCase))
                {
                    attack += 2; // Fire-themed enemies hit harder
                }
            }

            return new Enemy(name, health, attack);
        }

        /// <summary>
        /// Creates a random enemy from a list of names.
        /// </summary>
        /// <param name="enemyNames">A list of possible enemy names.</param>
        /// <param name="theme">The theme of the dungeon (optional).</param>
        /// <returns>A new Enemy object.</returns>
        public static Enemy CreateRandomEnemy(List<string> enemyNames, string? theme = null)
        {
            string name = enemyNames[random.Next(enemyNames.Count)];
            return CreateEnemy(name, theme);
        }
    }
}
