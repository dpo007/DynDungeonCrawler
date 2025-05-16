using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Factories
{
    public static class EnemyFactory
    {
        private static readonly Random random = Random.Shared;

        /// <summary>
        /// Asynchronously generates a list of enemy names based on the provided dungeon theme using an LLM client.
        /// </summary>
        /// <param name="theme">The dungeon theme to base the enemy types on.</param>
        /// <param name="llmClient">The LLM client instance to use for generation.</param>
        /// <param name="count">The number of enemy names to generate (must be 1 or more, defaults to 10).</param>
        /// <returns>A Task representing the asynchronous operation, containing a list of enemy names.</returns>
        public static async Task<List<string>> GenerateEnemyNamesAsync(string theme, ILLMClient llmClient, int count = 10)
        {
            ArgumentNullException.ThrowIfNull(llmClient);

            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be 1 or greater.");
            }

            try
            {
                string userPrompt = $"Generate a simple JSON list (no explanations, no other text) of {count} fantasy-themed enemy types appropriate for the following dungeon theme: \"{theme}\".";

                string response = await llmClient.GetResponseAsync(userPrompt, "You are an enemy type name generator. You only respond with raw JSON list of names. Return only plain text, don't use markdown.");

                // Clean up response if it has &&& markers
                response = response.TrimStart();

                if (response.StartsWith("&&&"))
                {
                    int firstNewline = response.IndexOf('\n');
                    int lastBackticks = response.LastIndexOf("&&&");

                    if (firstNewline >= 0 && lastBackticks >= 0 && lastBackticks > firstNewline)
                    {
                        response = response.Substring(firstNewline + 1, lastBackticks - firstNewline - 1).Trim();
                    }
                }

                var parsed = JsonSerializer.Deserialize<List<string>>(response);

                if (parsed != null && parsed.Count > 0)
                {
                    // If the LLM returns more names than requested, trim the list
                    if (parsed.Count > count)
                        parsed = parsed.Take(count).ToList();
                    return parsed;
                }
                else
                {
                    Console.WriteLine("Warning: LLM response parsing failed. Falling back to default names.");
                    return GetDefaultEnemyNames(count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enemy names: {ex.Message}");
                return GetDefaultEnemyNames(count);
            }
        }

        /// <summary>
        /// Returns a list of default enemy names if LLM fails to provide them.
        /// </summary>
        /// <param name="count">The number of names to return.</param>
        /// <returns>A list of default enemy names.</returns>
        private static List<string> GetDefaultEnemyNames(int count = 10)
        {
            List<string> defaults = new List<string>
            {
                "Bandit",
                "Bat",
                "Bone Scorpion",
                "Cave Serpent",
                "Cultist",
                "Dark Mage",
                "Dire Wolf",
                "Fire Elemental",
                "Flesh Construct",
                "Ghost",
                "Ghoul",
                "Giant Rat",
                "Goblin",
                "Hellhound",
                "Ice Wyrm",
                "Imp",
                "Lich",
                "Mimic",
                "Necromancer",
                "Orc",
                "Shadow Beast",
                "Skeleton",
                "Slime",
                "Spider",
                "Stone Golem",
                "Swamp Hag",
                "Troll",
                "Venom Drake",
                "Wraith",
                "Zombie"
            };

            // If fewer than defaults requested, return the available names
            if (count <= defaults.Count)
                return defaults.Take(count).ToList();

            // If more than defaults requested, repeat as needed
            var result = new List<string>();
            while (result.Count < count)
            {
                result.AddRange(defaults);
            }
            return result.Take(count).ToList();
        }

        /// <summary>
        /// Asynchronously generates a vivid description for a given enemy name and dungeon theme using the LLM client.
        /// </summary>
        /// <param name="enemyName">The name of the enemy to describe.</param>
        /// <param name="theme">The theme of the dungeon.</param>
        /// <param name="llmClient">The LLM client instance to use for generation.</param>
        /// <returns>A Task representing the asynchronous operation, containing the generated description as a string.</returns>
        public static async Task<string> GenerateEnemyDescriptionAsync(string enemyName, string theme, ILLMClient llmClient)
        {
            ArgumentNullException.ThrowIfNull(llmClient);

            if (string.IsNullOrWhiteSpace(enemyName))
                throw new ArgumentException("Enemy name is required and cannot be null or empty.", nameof(enemyName));
            if (string.IsNullOrWhiteSpace(theme))
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));

            string userPrompt = $@"
You are an expert fantasy game designer helping build immersive dungeon content.

Generate a vivid and engaging description of a creature or enemy for a dungeon crawler game. Your description should match the given creature name and fit the overall theme of the dungeon. The result should be flavorful, atmospheric, and game-appropriate (no more than 4-5 sentences). Avoid game stats or explicit mechanics — focus on personality, lore, appearance, and behavior.

Creature Name: {enemyName}  
Dungeon Theme: {theme}

Respond only with the description.";

            string response = await llmClient.GetResponseAsync(userPrompt);

            // Optionally trim and clean up the response
            return response.Trim();
        }

        /// <summary>
        /// Creates an enemy based on the given name, description, and optional theme.
        /// </summary>
        /// <param name="name">The name of the enemy.</param>
        /// <param name="description">A non-empty, vivid description of the enemy.</param>
        /// <param name="theme">The theme of the dungeon (optional).</param>
        /// <returns>A new Enemy object.</returns>
        public static Enemy CreateEnemy(string name, string description, string? theme = null)
        {
            int health = random.Next(10, 21); // Random health between 10 and 20
            int attack = random.Next(2, 6);   // Random attack between 2 and 5

            if (theme != null)
            {
                if (theme.Contains("dark", StringComparison.OrdinalIgnoreCase))
                {
                    health += 5;
                }
                else if (theme.Contains("fire", StringComparison.OrdinalIgnoreCase))
                {
                    attack += 2;
                }
            }

            return new Enemy(name, description, health, attack);
        }

        /// <summary>
        /// Asynchronously creates a random enemy using the LLM client and a specified theme.
        /// </summary>
        /// <param name="llmClient"></param>
        /// <param name="theme">The theme of the dungeon (required).</param>
        /// <returns>A Task representing the asynchronous operation, containing a new Enemy object.</returns>
        public static async Task<Enemy> CreateRandomEnemyAsync(ILLMClient llmClient, string theme)
        {
            ArgumentNullException.ThrowIfNull(llmClient);

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

        /// <summary>
        /// Asynchronously generates a list of enemy types, each with a name and a vivid description,
        /// based on the provided dungeon theme using an LLM client.
        /// </summary>
        /// <param name="theme">The dungeon theme to base the enemy types on.</param>
        /// <param name="llmClient">The LLM client instance to use for generation.</param>
        /// <param name="count">The number of enemy types to generate (must be 1 or more, defaults to 10).</param>
        /// <returns>A Task representing the asynchronous operation, containing a list of <see cref="EnemyTypeInfo"/> objects.</returns>
        public static async Task<List<EnemyTypeInfo>> GenerateEnemyTypesAsync(string theme, ILLMClient llmClient, int count = 10)
        {
            var names = await GenerateEnemyNamesAsync(theme, llmClient, count);
            var result = new List<EnemyTypeInfo>(names.Count);

            foreach (var name in names)
            {
                string description = await GenerateEnemyDescriptionAsync(name, theme, llmClient);
                result.Add(new EnemyTypeInfo(name, description));
            }

            return result;
        }

        /// <summary>
        /// Creates a random enemy by selecting a random name and description from a pre-populated list of enemy types.
        /// </summary>
        /// <param name="enemyTypes">A non-empty list of pre-generated <see cref="EnemyTypeInfo"/> objects.</param>
        /// <param name="theme">The theme of the dungeon (optional, used for stat adjustments).</param>
        /// <returns>A new Enemy object.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="enemyTypes"/> is null or empty.</exception>
        public static Enemy CreateRandomEnemyFromList(List<EnemyTypeInfo> enemyTypes, string? theme = null)
        {
            if (enemyTypes == null || enemyTypes.Count == 0)
                throw new ArgumentException("Enemy types list must not be null or empty.", nameof(enemyTypes));

            var chosen = enemyTypes[random.Next(enemyTypes.Count)];
            return CreateEnemy(chosen.Name, chosen.Description, theme);
        }

    }
}