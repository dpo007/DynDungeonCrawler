using DynDungeonCrawler.Engine.Helpers.LLM;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers.ContentGeneration
{
    /// <summary>
    /// Provides a random dungeon theme from a persistent list, with a small chance to generate new themes using the LLM.
    /// </summary>
    public static class DungeonThemeProvider
    {
        private static readonly string ThemeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DungeonThemes.json");
        private static readonly SemaphoreSlim FileLock = new SemaphoreSlim(1, 1);
        private const int MaxThemeLength = 255;

        private static readonly string[] DefaultThemes = new[] {
            "A D&D style dungeon filled with ancient secrets.",
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
        /// <param name="isPrimingFile">Indicates if the method is being used for file priming.</param>
        /// <returns>A random dungeon theme string.</returns>
        public static async Task<string> GetRandomThemeAsync(ILLMClient llmClient, ILogger logger, int llmBatchSize = 5, bool isPrimingFile = false)
        {
            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);

            // Only check for file population if not priming
            if (!isPrimingFile)
            {
                await EnsureThemeFilePopulatedAsync(llmClient, logger);
            }

            // Use LLM always during priming, otherwise use ~2% chance
            bool useLlm = isPrimingFile || Random.Shared.NextDouble() < 0.02;

            if (useLlm)
            {
                logger.Log($"[ThemeGen] Requesting {llmBatchSize} new themes from LLM...");

                string userPrompt = $"Give me {llmBatchSize} unique RPG dungeon theme ideas, mixing traditional and funny styles. Each theme must be a single line, 255 characters or fewer. Format exactly like: <title>: <description>. Return only the list, with one theme per line. Do not include any extra text.";

                string systemPrompt = "You are a creative fantasy RPG dungeon theme generator. Your job is to return a list of vivid, imaginative dungeon themes. Each theme must be a single line and strictly 255 characters or fewer, including the title and description. Format exactly like: <title>: <description>. Return only the list, with one theme per line. Do not include numbering, markdown, or any extra text. If a theme exceeds the limit, shorten or omit it.";

                int retries = 0;
                List<string> newThemes = new();
                while (retries < 3)
                {
                    string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

                    // For theme generation, we don't need full JSON cleaning since we're expecting a plain text list
                    // but we should clean up potential markdown code blocks
                    if (llmResponse.Contains("```"))
                    {
                        llmResponse = LLMJsonCleaner.CleanJsonResponse(llmResponse);
                    }

                    // Split the response into lines, trim whitespace, and filter out empty lines
                    List<string> allLines = llmResponse
                        .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .ToList();

                    HashSet<string> uniqueThemes = new(StringComparer.OrdinalIgnoreCase);
                    List<string> acceptedThemes = new();

                    foreach (string line in allLines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            logger.Log("[ThemeGen] Rejected LLM theme: empty or whitespace.");
                            continue;
                        }
                        if (line.Length > MaxThemeLength)
                        {
                            logger.Log($"[ThemeGen] Rejected LLM theme (too long): \"{line}\"");
                            continue;
                        }
                        if (!uniqueThemes.Add(line))
                        {
                            logger.Log($"[ThemeGen] Rejected LLM theme (duplicate): \"{line}\"");
                            continue;
                        }
                        acceptedThemes.Add(line);
                    }
                    newThemes = acceptedThemes;

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

                // Return one of the new themes at random (or the whole batch if priming)
                return isPrimingFile ? string.Join("\n", newThemes) : newThemes[Random.Shared.Next(newThemes.Count)];
            }
            else
            {
                return await GetRandomThemeFromFileAsync(logger);
            }
        }

        /// <summary>
        /// Ensures the theme file exists and, if not, creates it with defaults and then appends 10 LLM-generated themes.
        /// </summary>
        /// <param name="llmClient">The LLM client to use for theme generation.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        private static async Task EnsureThemeFilePopulatedAsync(ILLMClient llmClient, ILogger logger)
        {
            if (File.Exists(ThemeFilePath))
            {
                return;
            }

            logger.Log("[ThemeGen] DungeonThemes.json not found. Creating with defaults and LLM-generated themes...");

            // Write defaults
            Directory.CreateDirectory(Path.GetDirectoryName(ThemeFilePath)!);
            List<string> allThemes = DefaultThemes.ToList();
            await File.WriteAllTextAsync(ThemeFilePath, JsonSerializer.Serialize(allThemes));

            // Use GetRandomThemeAsync to get up to 10 LLM themes (in one call)
            try
            {
                string llmBatch = await GetRandomThemeAsync(llmClient, logger, 10, isPrimingFile: true);
                List<string> llmThemes = llmBatch
                    .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length <= MaxThemeLength)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int beforeCount = allThemes.Count;
                foreach (string theme in llmThemes)
                {
                    if (!allThemes.Contains(theme, StringComparer.OrdinalIgnoreCase))
                    {
                        allThemes.Add(theme);
                    }
                }
                if (allThemes.Count > beforeCount)
                {
                    await File.WriteAllTextAsync(ThemeFilePath, JsonSerializer.Serialize(allThemes));
                    logger.Log($"[ThemeGen] Added {allThemes.Count - beforeCount} LLM-generated themes to DungeonThemes.json.");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"[ThemeGen] LLM theme generation failed: {ex.Message}");
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