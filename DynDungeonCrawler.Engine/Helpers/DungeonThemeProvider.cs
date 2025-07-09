using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Provides a random dungeon theme from a persistent list, with a small chance to generate new themes using the LLM.
    /// </summary>
    public static class DungeonThemeProvider
    {
        private static readonly string ThemeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DungeonThemes.json");
        private static readonly SemaphoreSlim FileLock = new SemaphoreSlim(1, 1);
        private const int MaxThemeLength = 255;

        private static readonly string[] DefaultThemes = new[]
        {
            // All themes are under 255 characters
            "Ancient Crypts: A labyrinth of crumbling tombs and haunted mausoleums.",
            "Mushroom Caverns: Bioluminescent fungi and spore-filled tunnels.",
            "Clockwork Fortress: Gears, pistons, and mechanical guardians.",
            "Candy Catacombs: Sugary walls, licorice traps, and gumdrop monsters.",
            "Frozen Ruins: Icy halls and frostbitten undead."
        };

        /// <summary>
        /// Returns a random dungeon theme from a persistent list, with a small chance to generate new themes using the LLM.
        /// </summary>
        /// <param name="llmClient">The LLM client to use for theme generation.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <param name="llmBatchSize">How many new themes to request from the LLM if generating (default 5).</param>
        /// <returns>A random dungeon theme string.</returns>
        public static async Task<string> GetRandomThemeAsync(ILLMClient llmClient, ILogger logger, int llmBatchSize = 5)
        {
            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);

            // Decide if we should call the LLM (~2% chance)
            bool useLlm = Random.Shared.NextDouble() < 0.02;

            if (useLlm)
            {
                logger.Log($"[ThemeGen] Requesting {llmBatchSize} new themes from LLM...");
                string userPrompt = $"Give me {llmBatchSize} RPG dungeon theme ideas, mixing traditional and funny styles. Each theme must be 255 characters or fewer. Format each as a single line like: <title> <description>. Return only the list, no extra text.";
                string systemPrompt = "You are a creative fantasy RPG dungeon theme generator. You only reply with a list of unique, vivid, and imaginative dungeon themes, each as a single line: <title> <description>. Each theme must be 255 characters or fewer. Do not include any extra text, explanations, or markdown.";

                int retries = 0;
                List<string> newThemes = new();
                while (retries < 3)
                {
                    string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);
                    newThemes = llmResponse
                        .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length <= MaxThemeLength)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (newThemes.Count > 0)
                    {
                        break;
                    }

                    retries++;
                }

                if (newThemes.Count == 0)
                {
                    logger.Log("[ThemeGen] LLM returned no usable themes. Falling back to file.");
                    return await GetRandomThemeFromFileAsync(logger);
                }

                // Add all new themes to the file (deduplicated)
                await FileLock.WaitAsync();
                try
                {
                    List<string> allThemes = await ReadOrCreateThemeFileAsync();
                    int beforeCount = allThemes.Count;
                    foreach (string theme in newThemes)
                    {
                        if (!allThemes.Contains(theme, StringComparer.OrdinalIgnoreCase))
                        {
                            allThemes.Add(theme);
                        }
                    }
                    if (allThemes.Count > beforeCount)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(ThemeFilePath)!);
                        await File.WriteAllTextAsync(ThemeFilePath, JsonSerializer.Serialize(allThemes));
                        logger.Log($"[ThemeGen] Added {allThemes.Count - beforeCount} new themes to file.");
                    }
                }
                finally
                {
                    FileLock.Release();
                }

                // Return one of the new themes at random
                return newThemes[Random.Shared.Next(newThemes.Count)];
            }
            else
            {
                return await GetRandomThemeFromFileAsync(logger);
            }
        }

        private static async Task<string> GetRandomThemeFromFileAsync(ILogger logger)
        {
            await FileLock.WaitAsync();
            try
            {
                List<string> allThemes = await ReadOrCreateThemeFileAsync();
                if (allThemes.Count == 0)
                {
                    logger.Log("[ThemeGen] Theme file is empty. Using default theme.");
                    return DefaultThemes[0];
                }
                return allThemes[Random.Shared.Next(allThemes.Count)];
            }
            finally
            {
                FileLock.Release();
            }
        }

        private static async Task<List<string>> ReadOrCreateThemeFileAsync()
        {
            if (!File.Exists(ThemeFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemeFilePath)!);
                await File.WriteAllTextAsync(ThemeFilePath, JsonSerializer.Serialize(DefaultThemes));
                return DefaultThemes.ToList();
            }
            string json = await File.ReadAllTextAsync(ThemeFilePath);
            try
            {
                List<string>? themes = JsonSerializer.Deserialize<List<string>>(json);
                return themes ?? DefaultThemes.ToList();
            }
            catch
            {
                // If file is corrupt, reset to defaults
                await File.WriteAllTextAsync(ThemeFilePath, JsonSerializer.Serialize(DefaultThemes));
                return DefaultThemes.ToList();
            }
        }
    }
}