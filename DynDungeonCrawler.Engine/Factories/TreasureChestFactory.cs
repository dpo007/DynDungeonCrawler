using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Factories
{
    public static class TreasureChestFactory
    {
        private static readonly Random random = Random.Shared;

        /// <summary>
        /// Creates a basic treasure chest with optional locking.
        /// </summary>
        /// <param name="isLocked">Whether the chest is locked. Default is false.</param>
        /// <param name="name">The name of the chest. Default is "Treasure Chest".</param>
        /// <returns>A new TreasureChest object.</returns>
        public static TreasureChest CreateTreasureChest(bool isLocked = false, string name = "Treasure Chest")
        {
            return new TreasureChest(name, isLocked);
        }

        /// <summary>
        /// Creates a random treasure chest with a theme-based probability of being locked.
        /// </summary>
        /// <param name="theme">The theme of the dungeon, which affects locking probability.</param>
        /// <returns>A new TreasureChest object.</returns>
        public static TreasureChest CreateRandomTreasureChest(string? theme = null)
        {
            // Base chance of being locked is 30%
            double lockChance = 0.3;

            // Adjust lock chance based on theme
            if (!string.IsNullOrWhiteSpace(theme))
            {
                if (theme.Contains("ancient", StringComparison.OrdinalIgnoreCase) ||
                    theme.Contains("secure", StringComparison.OrdinalIgnoreCase) ||
                    theme.Contains("vault", StringComparison.OrdinalIgnoreCase))
                {
                    lockChance = 0.6; // 60% chance if the theme involves security
                }
                else if (theme.Contains("abandoned", StringComparison.OrdinalIgnoreCase) ||
                         theme.Contains("ruined", StringComparison.OrdinalIgnoreCase))
                {
                    lockChance = 0.15; // 15% chance if the theme suggests disrepair
                }
            }

            bool isLocked = random.NextDouble() < lockChance;
            return new TreasureChest("Treasure Chest", isLocked);
        }

        /// <summary>
        /// Asynchronously generates a description for a treasure chest based on its contents and the dungeon theme.
        /// </summary>
        /// <param name="chest">The treasure chest to describe.</param>
        /// <param name="theme">The theme of the dungeon.</param>
        /// <param name="llmClient">The LLM client instance to use for generation.</param>
        /// <param name="logger">The logger instance to use for warnings and errors.</param>
        /// <returns>A Task representing the asynchronous operation, containing the generated description as a string.</returns>
        public static async Task<string> GenerateChestDescriptionAsync(
            TreasureChest chest, string theme, ILLMClient llmClient, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(chest);
            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);

            if (string.IsNullOrWhiteSpace(theme))
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));

            string treasureDescription = chest.ContainedTreasure?.ToString() ?? "unknown treasure";
            string lockState = chest.IsLocked ? "locked" : "unlocked";

            string userPrompt = $@"
You are an expert fantasy game designer helping build immersive dungeon content.

Generate a vivid and engaging description of a treasure chest for a dungeon crawler game.
Your description should fit the overall theme of the dungeon and match the characteristics of the chest.

Chest details:
- Contains: {treasureDescription}
- Lock state: {lockState}
- Dungeon theme: {theme}

Your description should be flavorful, atmospheric, and game-appropriate (no more than 3-4 sentences).
Focus on the appearance, material, age, and condition of the chest. Don't explicitly state the contents
or whether it's locked - that will be revealed through gameplay.

Respond only with the description. Return only plain text, don't use markdown.";

            logger.Log($"Generating description for treasure chest containing {treasureDescription}");

            string response = await llmClient.GetResponseAsync(userPrompt);

            // Return the trimmed response
            return response.Trim();
        }
    }
}