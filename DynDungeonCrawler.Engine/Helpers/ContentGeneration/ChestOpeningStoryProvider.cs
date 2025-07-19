using DynDungeonCrawler.Engine.Interfaces;
using System.Text;

namespace DynDungeonCrawler.Engine.Helpers.ContentGeneration
{
    /// <summary>
    /// Provides narratively rich descriptions of adventurers opening treasure chests,
    /// generated based on the dungeon theme and tailored for the D&D-style fantasy setting.
    /// </summary>
    public static class ChestOpeningStoryProvider
    {
        /// <summary>
        /// Generates a collection of descriptive narratives for chest opening moments.
        /// Each story is 2-3 lines long, describes the process of opening a chest (sometimes with struggle,
        /// sometimes with ease), and always ends with success. The stories are influenced by the provided dungeon theme.
        /// </summary>
        /// <param name="theme">The dungeon theme to base the stories on.</param>
        /// <param name="llmClient">The LLM client for generating content.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <param name="count">Number of stories to generate (default: 5). Must be 1 or more.</param>
        /// <returns>A list of thematic chest opening stories.</returns>
        /// <exception cref="ArgumentNullException">Thrown if llmClient or logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown if theme is null or empty, or if count is less than 1.</exception>
        public static async Task<List<string>> GenerateChestOpeningStoriesAsync(
            string theme,
            ILLMClient llmClient,
            ILogger logger,
            int count = 5)
        {
            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);

            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));
            }

            if (count < 1)
            {
                throw new ArgumentException("Count must be 1 or more.", nameof(count));
            }

            logger.Log($"Generating {count} chest opening stories based on theme: {theme}");

            string systemPrompt = "You are a creative fantasy RPG narrative writer. Your task is to create short, vivid descriptions of adventurers opening treasure chests in a dungeon. Each story should be 2-3 lines long, evocative of D&D adventure narratives, and appropriate for an RPG dungeon crawling game. Focus on the process of opening the chest, the adventurer's actions, and the atmosphere, not the chest's contents. Stories should be self-contained and vary in style, with some depicting easy openings and others showing struggle, but all must end with the chest being successfully opened.";

            string userPrompt = $"Create {count} short, vivid descriptions of an adventurer opening a treasure chest in a dungeon with the theme: \"{theme}\".\n\n" +
                                "For each description:\n" +
                                "1. Write 2-3 lines (30-70 words) of engaging narrative\n" +
                                "2. Describe the process of opening the chest (sometimes with struggle, sometimes with ease)\n" +
                                "3. Always end with success (the chest opens)\n" +
                                "4. Use atmospheric, D&D-style fantasy language\n" +
                                "5. Match the theme of the dungeon\n" +
                                "6. Vary the style and approach across the set\n" +
                                "7. Don't include specific treasure contents\n" +
                                "8. Don't name the adventurer (use terms like \"the adventurer\", \"the explorer\", etc.)\n\n" +
                                $"Number each description 1 through {count}. Return only the descriptions, no additional commentary.";

            // Call the LLM
            string response = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            // Parse the response into individual stories
            List<string> stories = ParseStoriesFromResponse(response, count, logger);

            // If we didn't get enough stories, add some generic fallbacks
            if (stories.Count < count)
            {
                logger.Log($"LLM returned only {stories.Count} usable stories. Adding fallbacks to reach {count}.");
                stories.AddRange(GetFallbackStories(count - stories.Count, theme));
            }

            logger.Log($"Generated {stories.Count} chest opening stories successfully.");
            return stories;
        }

        /// <summary>
        /// Parses the LLM response into individual stories, cleaning up formatting and numbering.
        /// </summary>
        /// <param name="response">The raw LLM response text.</param>
        /// <param name="expectedCount">The expected number of stories.</param>
        /// <param name="logger">Logger for errors and warnings.</param>
        /// <returns>A list of parsed stories.</returns>
        private static List<string> ParseStoriesFromResponse(string response, int expectedCount, ILogger logger)
        {
            List<string> result = new();

            // Split the text by lines
            string[] lines = response.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            // Process each line to extract stories
            StringBuilder currentStory = new();
            int currentNumber = 0;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                // Check if this line starts a new story (begins with a number)
                if (trimmedLine.Length >= 2 && char.IsDigit(trimmedLine[0]) &&
                    (trimmedLine[1] == '.' || trimmedLine[1] == ':' || trimmedLine[1] == ')'))
                {
                    int number;
                    if (int.TryParse(trimmedLine[0].ToString(), out number))
                    {
                        // If we were building a story, add it to results
                        if (currentStory.Length > 0)
                        {
                            result.Add(currentStory.ToString().Trim());
                            currentStory.Clear();
                        }

                        // Start new story, removing the number prefix
                        currentNumber = number;
                        currentStory.Append(trimmedLine.Substring(2).Trim());
                    }
                    else
                    {
                        // Not a valid number prefix, append to current story
                        if (currentStory.Length > 0)
                        {
                            currentStory.Append(' ');
                        }
                        currentStory.Append(trimmedLine);
                    }
                }
                else
                {
                    // Continue current story
                    if (currentStory.Length > 0)
                    {
                        currentStory.Append(' ');
                    }
                    currentStory.Append(trimmedLine);
                }
            }

            // Add the last story if there is one
            if (currentStory.Length > 0)
            {
                result.Add(currentStory.ToString().Trim());
            }

            // Validate and log
            if (result.Count < expectedCount)
            {
                logger.Log($"Warning: Only parsed {result.Count} stories from LLM response (expected {expectedCount})");
            }

            return result;
        }

        /// <summary>
        /// Provides fallback stories when the LLM fails to generate enough.
        /// </summary>
        /// <param name="count">Number of fallback stories needed.</param>
        /// <param name="theme">The dungeon theme to reference.</param>
        /// <returns>A list of generic fallback stories.</returns>
        private static List<string> GetFallbackStories(int count, string theme)
        {
            List<string> fallbacks = new()
            {
                "The adventurer traces the intricate patterns on the chest's surface before inserting the ancient key. With a satisfying click, the mechanism yields and the lid opens with a whisper of escaping air.",

                "Fingers trembling with anticipation, the explorer works the rusted lock. After several tense moments of struggle, persistence pays off as the chest's heavy lid finally creaks open.",

                "A determined push against the chest's weathered lid reveals it to be unlocked. The hinges protest with age but ultimately surrender, exposing the treasures within.",

                "The chest presents a complex puzzle lock, challenging the adventurer's wits. After several failed attempts, a pattern emerges, and with a triumphant twist, the chest opens with a satisfying click.",

                "Kneeling before the ancient chest, the adventurer carefully examines its worn surface. Finding no traps, they gently lift the lid, which opens surprisingly easily despite its apparent age.",

                "With a grunt of effort, the adventurer forces their blade between the chest's lid and base. A moment of leverage, and the old lock breaks with a snap. The treasure is theirs for the taking.",

                "The chest's lock proves stubborn under the adventurer's attempts. Frustration builds until, with one final determined effort, the mechanism finally gives way and the lid swings open.",

                "Dust billows as the adventurer wipes centuries of grime from the chest's surface. Finding the keyhole, they insert a slender tool and work patiently until rewarded with the sound of tumbling pins."
            };

            // Return the requested number of fallbacks (cycling through if we need more than we have)
            return fallbacks.Take(count).ToList();
        }
    }
}