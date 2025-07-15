using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

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
        /// Asynchronously generates and assigns descriptions for all treasure chests in the provided rooms.
        /// </summary>
        /// <param name="roomsWithChests">Rooms containing treasure chests to describe.</param>
        /// <param name="theme">The dungeon theme.</param>
        /// <param name="llmClient">The LLM client instance.</param>
        /// <param name="logger">Logger for warnings and errors.</param>
        public static async Task GenerateTreasureDescriptionsAsync(List<Room> roomsWithChests, string theme, ILLMClient llmClient, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(roomsWithChests);
            ArgumentNullException.ThrowIfNull(llmClient);
            ArgumentNullException.ThrowIfNull(logger);
            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("Theme is required and cannot be null or empty.", nameof(theme));
            }

            // Gather all chests in all rooms
            List<(TreasureChest chest, Room room)> chests = (
                from room in roomsWithChests
                from entity in room.Contents
                let chest = entity as TreasureChest
                where chest != null && string.IsNullOrWhiteSpace(chest.Description)
                select (chest, room)
            ).ToList();

            if (chests.Count == 0)
            {
                logger.Log("No treasure chests require description generation.");
                return;
            }
            else
            {
                logger.Log($"Found {chests.Count} treasure chests to generate descriptions for.");
            }

            // Prepare JSON batch for LLM
            var chestRequest = new
            {
                theme,
                chests = chests.Select(tuple => new
                {
                    id = tuple.chest.Id,
                    roomDescription = tuple.room.Description,
                    contains = tuple.chest.ContainedTreasure?.ToString() ?? "unknown treasure",
                    lockState = tuple.chest.IsLocked ? "locked" : "unlocked"
                }).ToList()
            };
            string inputJson = JsonSerializer.Serialize(chestRequest);
            string userPrompt = $@"
Given the following dungeon theme and a list of treasure chests (with their room descriptions), generate for each chest:
- A vivid, atmospheric, and game-appropriate 'description' (3-4 sentences).
- A 'shortDescription' (1 concise sentence, 10-20 words) summarizing the chest for quick display.

For each chest:
- Use the room description to inspire the chest's appearance and style.
- Do NOT explicitly state the contents or whether it's locked.
- Focus on the chest's appearance, material, age, and condition.

Return the same JSON structure, but add both a 'description' and a 'shortDescription' field to each chest, generated based on the theme, chest details, and room context.

Do not change the IDs or other fields. Only return valid JSON, with no markdown formatting.

{inputJson}";
            string systemPrompt = LLMDefaults.DefaultSystemPrompt;

            // Call LLM
            string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            // Clean the LLM response to remove markdown formatting and other unwanted elements
            llmResponse = LLMJsonCleaner.CleanJsonResponse(llmResponse);

            // Parse response and assign descriptions
            JsonDocument? doc = null;
            const int maxAttempts = 5;
            int attempt = 0;
            while (attempt < maxAttempts)
            {
                try
                {
                    doc = JsonDocument.Parse(llmResponse);
                    break;
                }
                catch (JsonException ex)
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                    {
                        logger.Log($"Failed to parse LLM response after {maxAttempts} attempts: {ex.Message}");
                        logger.Log($"User Prompt: {userPrompt}");
                        logger.Log($"LLM Response: {llmResponse}");
                        throw;
                    }
                    logger.Log($"JSON parse failed (attempt {attempt}): {ex.Message}. Retrying...");
                    await Task.Delay(100 * attempt);
                }
            }
            if (doc == null)
            {
                throw new InvalidOperationException("Failed to parse LLM response after retries.");
            }

            using (doc)
            {
                JsonElement responseChests = doc.RootElement.GetProperty("chests");
                foreach ((TreasureChest chest, Room _) in chests)
                {
                    JsonElement chestJson = responseChests.EnumerateArray()
                        .FirstOrDefault(c => c.GetProperty("id").GetGuid() == chest.Id);

                    if (chestJson.ValueKind != JsonValueKind.Undefined)
                    {
                        if (chestJson.TryGetProperty("description", out JsonElement descElem))
                        {
                            chest.Description = descElem.GetString()?.Trim() ?? "";
                        }

                        if (chestJson.TryGetProperty("shortDescription", out JsonElement shortDescElem))
                        {
                            chest.ShortDescription = shortDescElem.GetString()?.Trim() ?? "";
                        }

                        logger.Log($"Generated description and short description for treasure chest {chest.Id}");
                    }
                }
            }
        }
    }
}