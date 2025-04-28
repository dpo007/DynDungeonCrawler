using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Factories
{
    public static class EnemyFactory
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Asynchronously generates a list of enemy names based on the provided dungeon theme using an LLM client.
        /// </summary>
        /// <param name="theme">The dungeon theme to base the enemy types on.</param>
        /// <param name="llmClient">The LLM client instance to use for generation.</param>
        /// <returns>A Task representing the asynchronous operation, containing a list of enemy names.</returns>
        public static async Task<List<string>> GenerateEnemyNamesAsync(string theme, ILLMClient llmClient)
        {
            try
            {
                string userPrompt = $"Generate a simple JSON list (no explanations, no other text) of 10 fantasy-themed enemy types appropriate for the following dungeon theme: \"{theme}\".";

                string response = await llmClient.GetResponseAsync(userPrompt, "You are an enemy type name generator. You only respond with raw JSON list of names. Return only plain text, don't use markdown.");

                // Clean up response if it has ``` markers
                response = response.TrimStart();

                if (response.StartsWith("```"))
                {
                    int firstNewline = response.IndexOf('\n');
                    int lastBackticks = response.LastIndexOf("```");

                    if (firstNewline >= 0 && lastBackticks >= 0 && lastBackticks > firstNewline)
                    {
                        response = response.Substring(firstNewline + 1, lastBackticks - firstNewline - 1).Trim();
                    }
                }

                var parsed = JsonSerializer.Deserialize<List<string>>(response);

                if (parsed != null && parsed.Count > 0)
                {
                    return parsed;
                }
                else
                {
                    Console.WriteLine("Warning: LLM response parsing failed. Falling back to default names.");
                    return GetDefaultEnemyNames();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enemy names: {ex.Message}");
                return GetDefaultEnemyNames();
            }
        }

        /// <summary>
        /// Returns a list of default enemy names if LLM fails to provide them.
        /// </summary>
        /// <returns>A list of default enemy names.</returns>
        private static List<string> GetDefaultEnemyNames()
        {
            return new List<string> { "Goblin", "Orc", "Skeleton", "Zombie", "Spider", "Troll", "Bat", "Ghost", "Bandit", "Wraith" };
        }

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
        /// Asynchronously creates a random enemy using the LLM client and a specified theme.
        /// </summary>
        /// <param name="llmClient"></param>
        /// <param name="theme">The theme of the dungeon (required).</param>
        /// <returns>A Task representing the asynchronous operation, containing a new Enemy object.</returns>
        public static async Task<Enemy> CreateRandomEnemyAsync(ILLMClient llmClient, string theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));
            }

            // Load enemy names using the provided theme
            List<string> enemyNames = await GenerateEnemyNamesAsync(theme, llmClient);

            // Select a random enemy name and create the enemy
            string name = enemyNames[random.Next(enemyNames.Count)];
            return CreateEnemy(name, theme);
        }
    }
}