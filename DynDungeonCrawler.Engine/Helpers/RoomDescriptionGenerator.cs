using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers
{
    public static class RoomDescriptionGenerator
    {
        public static async Task GenerateRoomDescriptionsAsync(
            List<Room> rooms,
            string theme,
            ILLMClient llmClient,
            ILogger logger,
            bool allowClobber = false)
        {
            List<Room> targetRooms = allowClobber
                ? rooms
                : rooms.Where(r => string.IsNullOrWhiteSpace(r.Description)).ToList();

            if (targetRooms.Count == 0)
            {
                logger.Log("No rooms require description generation.");
                return;
            }

            List<Room> exitRooms = targetRooms.Where(r => r.Type == RoomType.Exit).ToList();
            List<Room> normalRooms = targetRooms.Where(r => r.Type != RoomType.Exit).ToList();

            Dictionary<Guid, string> allDescriptions = new Dictionary<Guid, string>();

            if (normalRooms.Count > 0)
            {
                var normalRequest = new
                {
                    theme,
                    rooms = normalRooms.Select(r => new
                    {
                        id = r.Id,
                        type = r.Type.ToString(),
                        exits = new
                        {
                            north = r.ConnectedNorth,
                            east = r.ConnectedEast,
                            south = r.ConnectedSouth,
                            west = r.ConnectedWest
                        }
                    }).ToList()
                };

                string inputJson = JsonSerializer.Serialize(normalRequest);
                string prompt = $@"
Given the following dungeon theme and room data in JSON, generate a vivid, concise fantasy description for each room.

For each room:
- Always mention the available exits (north, east, south, west).
- For each exit, describe the **physical appearance** of the doorway, arch, gate, or passage — including material, style, damage, light, markings, etc.
- Do **not** describe what lies beyond the exit — focus only on the portal itself.

Return the same JSON structure, but add a 'description' field to each room, generated based on the theme, room type, and exits.

Do not change the IDs or exits. Only return valid JSON, with no markdown formatting.

{inputJson}";

                string systemPrompt = "You are a creative fantasy room description generator for RPG dungeons.";

                Dictionary<Guid, string> normalDescriptions = await GetDescriptionsFromLLM(normalRooms, prompt, systemPrompt, llmClient, logger);
                foreach (KeyValuePair<Guid, string> kvp in normalDescriptions)
                {
                    allDescriptions[kvp.Key] = kvp.Value;
                }
            }

            if (exitRooms.Count > 0)
            {
                var exitRequest = new
                {
                    theme,
                    rooms = exitRooms.Select(r => new
                    {
                        id = r.Id,
                        type = r.Type.ToString()
                    }).ToList()
                };

                string exitJson = JsonSerializer.Serialize(exitRequest);
                string exitPrompt = $@"
You are a fantasy narrative generator.

Given the theme and room data, generate a short, vivid, and triumphant description congratulating the adventurer on escaping the dungeon.

For each room:
- Do NOT mention exits or physical portals.
- Describe the sense of relief, victory, or dramatic escape.
- Make it thematic, vivid, and emotionally resonant.
- Keep it concise (1–3 sentences).

Return the same JSON structure, but with a new 'description' field for each room.

Do not change any other data. Only return valid JSON, with no markdown formatting.

{exitJson}";

                string exitSystemPrompt = "You are a creative fantasy narrator celebrating a dungeon escape.";

                Dictionary<Guid, string> exitDescriptions = await GetDescriptionsFromLLM(exitRooms, exitPrompt, exitSystemPrompt, llmClient, logger);
                foreach (KeyValuePair<Guid, string> kvp in exitDescriptions)
                {
                    allDescriptions[kvp.Key] = kvp.Value;
                }
            }

            foreach (Room room in targetRooms)
            {
                if (allDescriptions.TryGetValue(room.Id, out string? desc) && !string.IsNullOrWhiteSpace(desc))
                {
                    room.Description = desc.Trim();
                    logger.Log($"Generated description for room {room.Id}");
                }
            }
        }

        private static async Task<Dictionary<Guid, string>> GetDescriptionsFromLLM(
            List<Room> rooms,
            string userPrompt,
            string systemPrompt,
            ILLMClient llmClient,
            ILogger logger)
        {
            string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            const int maxAttempts = 5;
            int attempt = 0;
            JsonDocument? doc = null;

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
                JsonElement responseRooms = doc.RootElement.GetProperty("rooms");

                return responseRooms.EnumerateArray()
                    .ToDictionary(
                        r => r.GetProperty("id").GetGuid(),
                        r => r.TryGetProperty("description", out JsonElement desc) ? desc.GetString() ?? "" : ""
                    );
            }
        }
    }
}