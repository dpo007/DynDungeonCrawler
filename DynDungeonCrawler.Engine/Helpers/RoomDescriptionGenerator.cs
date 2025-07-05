using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;
using Spectre.Console;
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

            Dictionary<Guid, (string Name, string Description)> allRoomData = new();

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
You are a fantasy world-building assistant.

Given the following dungeon **theme** and a list of **room objects** in JSON format, generate a vivid and immersive **description** and a fitting **name** for each room.

Instructions for each room:
- Incorporate the overall dungeon **theme** and the **room type**.
- Always include the **available exits** (north, east, south, west).
- For **each exit**, vividly describe the **physical appearance** of the portal (doorway, arch, gate, passage, etc.).
  - Mention style, materials, damage, age, light, markings, carvings, or magical effects.
  - Use varied vocabulary to avoid repetition.
  - Each exit's description should begin with the direction (e.g., 'To the north, a cracked stone arch...').
- Use concise, sensory-rich language - imagine you are writing for a text-based adventure or dungeon crawler game.
- Limit each description to approximately 3-5 sentences.
- Generate a room name that is evocative, thematic, and suitable for a fantasy dungeon. The name should be short (2-5 words), unique, and inspired by the room's description and type.

Response format:
- Return the exact same JSON structure, with added ""name"" and ""description"" fields in each room object.
- Do not modify any existing fields (IDs, coordinates, or exits).
- Return only valid, minified JSON - no markdown, no comments, no extra output.

{inputJson}";

                string systemPrompt = "You are an expert fantasy narrator who writes vivid, concise, and immersive room descriptions and evocative room names for procedurally generated RPG dungeons. Your style is atmospheric and rich with sensory detail, while staying brief and game-ready. You always respect the input structure and never invent content not grounded in the data.";

                Dictionary<Guid, (string Name, string Description)> normalRoomData = await GetNamesAndDescriptionsFromLLM(normalRooms, prompt, systemPrompt, llmClient, logger);
                foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in normalRoomData)
                {
                    allRoomData[kvp.Key] = kvp.Value;
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

Given the theme and room data, generate a short, vivid, and triumphant description congratulating the adventurer on escaping the dungeon, and a fitting room name.

For each room:
- Do NOT mention exits or physical portals.
- Describe the sense of relief, victory, or dramatic escape.
- Make it thematic, vivid, and emotionally resonant.
- Keep it concise (1-3 sentences).
- Generate a room name that is evocative, thematic, and suitable for a fantasy dungeon exit. The name should be short (2-5 words), unique, and inspired by the sense of escape.

Return the same JSON structure, but with new 'name' and 'description' fields for each room.

Do not change any other data. Only return valid JSON, with no markdown formatting.

{exitJson}";

                string exitSystemPrompt = "You are a creative fantasy narrator celebrating a dungeon escape, and you also invent fitting room names.";

                Dictionary<Guid, (string Name, string Description)> exitRoomData = await GetNamesAndDescriptionsFromLLM(exitRooms, exitPrompt, exitSystemPrompt, llmClient, logger);
                foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in exitRoomData)
                {
                    allRoomData[kvp.Key] = kvp.Value;
                }
            }

            foreach (Room room in targetRooms)
            {
                if (allRoomData.TryGetValue(room.Id, out (string Name, string Description) data))
                {
                    if (!string.IsNullOrWhiteSpace(data.Name))
                    {
                        room.Name = data.Name.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(data.Description))
                    {
                        room.Description = data.Description.Trim();
                    }

                    logger.Log($"Generated name and description for room {room.Id}");
                }
            }
        }

        private static async Task<Dictionary<Guid, (string Name, string Description)>> GetNamesAndDescriptionsFromLLM(
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
                        r => (
                            r.TryGetProperty("name", out JsonElement nameElem) ? nameElem.GetString() ?? "" : "",
                            r.TryGetProperty("description", out JsonElement descElem) ? descElem.GetString() ?? "" : ""
                        )
                    );
            }
        }
    }
}