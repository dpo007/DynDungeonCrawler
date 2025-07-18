﻿using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Helpers.LLM;
using DynDungeonCrawler.Engine.Interfaces;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Helpers.ContentGeneration
{
    /// <summary>
    /// Provides utilities for generating names and descriptions for dungeon rooms using an LLM client.
    /// Supports batching and parallel processing for efficient LLM calls.
    /// </summary>
    public static class RoomDescriptionGenerator
    {
        /// <summary>
        /// Generates names and descriptions for a list of rooms using an LLM client.
        /// Rooms are batched (max 5 per batch) and up to 3 batches are processed in parallel.
        /// Progress and batch information is logged throughout the process.
        /// </summary>
        /// <param name="rooms">The list of rooms to process.</param>
        /// <param name="theme">The dungeon theme to use for description generation.</param>
        /// <param name="llmClient">The LLM client for generating descriptions.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <param name="allowClobber">If true, all rooms are processed; otherwise, only rooms with empty descriptions.</param>
        public static async Task GenerateRoomDescriptionsAsync(
            List<Room> rooms,
            string theme,
            ILLMClient llmClient,
            ILogger logger,
            bool allowClobber = false)
        {
            // Determine which rooms need descriptions
            List<Room> targetRooms = allowClobber
                ? rooms
                : rooms.Where(r => string.IsNullOrWhiteSpace(r.Description)).ToList();

            // Log skipped rooms if not clobbering
            if (!allowClobber)
            {
                foreach (Room room in rooms)
                {
                    if (!string.IsNullOrWhiteSpace(room.Description))
                    {
                        logger.Log($"[RoomGen] Skipping room {room.Id} (already has a description)");
                    }
                }
            }

            if (targetRooms.Count == 0)
            {
                logger.Log("No rooms require description generation.");
                return;
            }

            // Separate exit rooms from normal rooms
            List<Room> exitRooms = targetRooms.Where(r => r.Type == RoomType.Exit).ToList();
            List<Room> normalRooms = targetRooms.Where(r => r.Type != RoomType.Exit).ToList();

            // Stores all generated room data by room ID
            Dictionary<Guid, (string Name, string Description)> allRoomData = new();

            // Helper for batching a list into sublists of a given size
            static IEnumerable<List<Room>> BatchRooms(List<Room> rooms, int batchSize)
            {
                for (int i = 0; i < rooms.Count; i += batchSize)
                {
                    yield return rooms.GetRange(i, Math.Min(batchSize, rooms.Count - i));
                }
            }

            const int batchSize = 5; // Maximum rooms per LLM call
            const int maxParallelBatches = 3; // Maximum concurrent LLM calls

            // Process normal (non-exit) rooms in batches
            if (normalRooms.Count > 0)
            {
                List<List<Room>> normalBatches = BatchRooms(normalRooms, batchSize).ToList();

                logger.Log($"[RoomGen] Normal rooms: {normalRooms.Count}, batching into {normalBatches.Count} batches of up to {batchSize} (processing up to {maxParallelBatches} in parallel)");

                string systemPrompt = "You are a masterful fantasy narrator who writes vivid, immersive, and richly detailed room descriptions for procedurally generated RPG dungeons. You always structure your descriptions with a full paragraph about the room itself, followed by a blank line, then a second paragraph describing each exit. You never invent content beyond the input data and always preserve the JSON structure.";

                string promptTemplate = @"
You are a fantasy world-building assistant.

Your task is to name and describe rooms in a procedurally generated RPG dungeon, using the given **dungeon theme** and a list of room objects in JSON format.

For each room:

- Use the **dungeon theme** and **room type** to inspire your writing.
- Write a detailed, vivid **description** that is always split into **two clearly separated paragraphs**:

  1. The first paragraph must be **4–6 full sentences** describing the room's interior:
     - Include sensory details such as architecture, lighting, sound, temperature, smell, remnants of prior use, and magical corruption or ambiance.

  2. Then insert a **literal blank line** (`\n\n`) between the first and second paragraphs.

  3. In the second paragraph, describe **each available exit**, using the room's `exits` field:
     - Begin each exit with the direction (e.g., “To the north, ...”).
     - Describe the portal’s materials, style, age, carvings, damage, magical effects, light, or markings.
     - Provide **at least one sentence per exit**. Vary language to avoid repetition.

- The tone should be immersive, moody, and suitable for a fantasy dungeon crawler.
- Generate a **room name** that is:
  - 2 to 5 words long
  - Unique, thematic, and inspired by the room’s mood, type, and features

Output Format:

- Return the **exact same JSON structure**, but add a `name` and `description` to each room.
- Do **not** change or remove any existing fields (such as `id`, `type`, or `exits`).
- Return only **valid, minified JSON** — no markdown, no line breaks outside of the paragraph split, no extra text.
- DO NOT RETURN MARKDOWN.

{{inputJson}}
";

                ConcurrentDictionary<Guid, (string Name, string Description)> normalRoomDataBag = new ConcurrentDictionary<Guid, (string Name, string Description)>();

                // Process batches in groups of up to maxParallelBatches
                for (int i = 0; i < normalBatches.Count; i += maxParallelBatches)
                {
                    List<List<Room>> batchGroup = normalBatches.Skip(i).Take(maxParallelBatches).ToList();

                    logger.Log($"[RoomGen] Processing normal room batches {i + 1}-{i + batchGroup.Count} of {normalBatches.Count}...");

                    // Launch LLM calls for this group in parallel
                    List<Task<Dictionary<Guid, (string Name, string Description)>>> tasks = batchGroup.Select((batch, batchIdx) => ProcessNormalBatchAsync(batch, theme, promptTemplate, systemPrompt, llmClient, logger, i + batchIdx + 1, normalBatches.Count)).ToList();
                    Dictionary<Guid, (string Name, string Description)>[] results = await Task.WhenAll(tasks);

                    // Aggregate results
                    foreach (Dictionary<Guid, (string Name, string Description)>? dict in results)
                    {
                        foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in dict)
                        {
                            normalRoomDataBag[kvp.Key] = kvp.Value;
                        }
                    }
                }

                foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in normalRoomDataBag)
                {
                    allRoomData[kvp.Key] = kvp.Value;
                }
            }

            // Process exit rooms in batches
            if (exitRooms.Count > 0)
            {
                List<List<Room>> exitBatches = BatchRooms(exitRooms, batchSize).ToList();

                logger.Log($"[RoomGen] Exit rooms: {exitRooms.Count}, batching into {exitBatches.Count} batches of up to {batchSize} (processing up to {maxParallelBatches} in parallel)");

                string exitSystemPrompt = "You are a fantasy narrator who writes vivid, emotionally powerful descriptions for dungeon escape rooms. You evoke relief, triumph, or finality, and invent fitting names for each space. Your tone is dramatic, poetic, or serene — depending on the moment.";

                string exitPromptTemplate = @"
You are a fantasy narrative generator.

Given a dungeon **theme** and a list of **exit rooms** in JSON format, write an emotionally rich **description** and an evocative **room name** for each one.

For each room:
- Do not mention exits or doors.
- Focus entirely on the **atmosphere of escape** — feelings of victory, exhaustion, awe, or transition.
- Describe the room’s **appearance, lighting, sounds, symbols, or magic** that signal the end of the dungeon journey.
- Write at least **a full paragraph (4–6 sentences)** of vivid, poetic, or dramatic language.
- Create a **room name** that is:
  - Short (2–5 words)
  - Unique, thematic, and emotionally resonant
  - Inspired by the feeling of closure, transcendence, or freedom

Output rules:
- Return the exact same JSON structure, with added `name` and `description` fields for each room.
- Do not change any existing fields.
- Return only valid, minified JSON — no markdown, no extra output.

{{exitJson}}
";

                ConcurrentDictionary<Guid, (string Name, string Description)> exitRoomDataBag = new ConcurrentDictionary<Guid, (string Name, string Description)>();

                // Process batches in groups of up to maxParallelBatches
                for (int i = 0; i < exitBatches.Count; i += maxParallelBatches)
                {
                    List<List<Room>> batchGroup = exitBatches.Skip(i).Take(maxParallelBatches).ToList();

                    logger.Log($"[RoomGen] Processing exit room batches {i + 1}-{i + batchGroup.Count} of {exitBatches.Count}...");

                    // Launch LLM calls for this group in parallel
                    List<Task<Dictionary<Guid, (string Name, string Description)>>> tasks = batchGroup.Select((batch, batchIdx) => ProcessExitBatchAsync(batch, theme, exitPromptTemplate, exitSystemPrompt, llmClient, logger, i + batchIdx + 1, exitBatches.Count)).ToList();
                    Dictionary<Guid, (string Name, string Description)>[] results = await Task.WhenAll(tasks);

                    // Aggregate results
                    foreach (Dictionary<Guid, (string Name, string Description)>? dict in results)
                    {
                        foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in dict)
                        {
                            exitRoomDataBag[kvp.Key] = kvp.Value;
                        }
                    }
                }

                foreach (KeyValuePair<Guid, (string Name, string Description)> kvp in exitRoomDataBag)
                {
                    allRoomData[kvp.Key] = kvp.Value;
                }
            }

            // Update the original room objects with generated names and descriptions
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

        /// <summary>
        /// Calls the LLM client to generate names and descriptions for a batch of rooms.
        /// Handles JSON parsing and retry logic for malformed responses.
        /// </summary>
        /// <param name="rooms">The batch of rooms to process.</param>
        /// <param name="userPrompt">The user prompt for the LLM.</param>
        /// <param name="systemPrompt">The system prompt for the LLM.</param>
        /// <param name="llmClient">The LLM client instance.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <returns>A dictionary mapping room IDs to generated names and descriptions.</returns>
        private static async Task<Dictionary<Guid, (string Name, string Description)>> GetNamesAndDescriptionsFromLLM(
            List<Room> rooms,
            string userPrompt,
            string systemPrompt,
            ILLMClient llmClient,
            ILogger logger)
        {
            string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            // Clean the LLM response to remove markdown formatting and other unwanted elements
            llmResponse = LLMJsonCleaner.CleanJsonResponse(llmResponse);

            const int maxAttempts = 5;
            int attempt = 0;
            JsonDocument? doc = null;

            // Retry parsing the LLM response up to maxAttempts times if it is not valid JSON
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

                // Extract name and description for each room in the response
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

        /// <summary>
        /// Processes a batch of normal (non-exit) rooms, logging before and after the LLM call.
        /// </summary>
        /// <param name="batch">The batch of rooms to process.</param>
        /// <param name="theme">The dungeon theme.</param>
        /// <param name="promptTemplate">The prompt template for the LLM.</param>
        /// <param name="systemPrompt">The system prompt for the LLM.</param>
        /// <param name="llmClient">The LLM client instance.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <param name="batchIdx">The 1-based index of this batch.</param>
        /// <param name="totalBatches">The total number of batches.</param>
        /// <returns>A dictionary mapping room IDs to generated names and descriptions.</returns>
        private static async Task<Dictionary<Guid, (string Name, string Description)>> ProcessNormalBatchAsync(
            List<Room> batch,
            string theme,
            string promptTemplate,
            string systemPrompt,
            ILLMClient llmClient,
            ILogger logger,
            int batchIdx,
            int totalBatches)
        {
            logger.Log($"[RoomGen]   Starting LLM call for normal batch {batchIdx}/{totalBatches} (rooms: {batch.Count})");

            var normalRequest = new
            {
                theme,
                rooms = batch.Select(r => new
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
            string prompt = promptTemplate.Replace("{{inputJson}}", inputJson);

            Dictionary<Guid, (string Name, string Description)> result = await GetNamesAndDescriptionsFromLLM(batch, prompt, systemPrompt, llmClient, logger);

            logger.Log($"[RoomGen]   Finished LLM call for normal batch {batchIdx}/{totalBatches}");

            return result;
        }

        /// <summary>
        /// Processes a batch of exit rooms, logging before and after the LLM call.
        /// </summary>
        /// <param name="batch">The batch of exit rooms to process.</param>
        /// <param name="theme">The dungeon theme.</param>
        /// <param name="exitPromptTemplate">The prompt template for the LLM.</param>
        /// <param name="exitSystemPrompt">The system prompt for the LLM.</param>
        /// <param name="llmClient">The LLM client instance.</param>
        /// <param name="logger">Logger for progress and error messages.</param>
        /// <param name="batchIdx">The 1-based index of this batch.</param>
        /// <param name="totalBatches">The total number of batches.</param>
        /// <returns>A dictionary mapping room IDs to generated names and descriptions.</returns>
        private static async Task<Dictionary<Guid, (string Name, string Description)>> ProcessExitBatchAsync(
            List<Room> batch,
            string theme,
            string exitPromptTemplate,
            string exitSystemPrompt,
            ILLMClient llmClient,
            ILogger logger,
            int batchIdx,
            int totalBatches)
        {
            logger.Log($"[RoomGen]   Starting LLM call for exit batch {batchIdx}/{totalBatches} (rooms: {batch.Count})");

            var exitRequest = new
            {
                theme,
                rooms = batch.Select(r => new
                {
                    id = r.Id,
                    type = r.Type.ToString()
                }).ToList()
            };

            string exitJson = JsonSerializer.Serialize(exitRequest);
            string exitPrompt = exitPromptTemplate.Replace("{{exitJson}}", exitJson);

            Dictionary<Guid, (string Name, string Description)> result = await GetNamesAndDescriptionsFromLLM(batch, exitPrompt, exitSystemPrompt, llmClient, logger);

            logger.Log($"[RoomGen]   Finished LLM call for exit batch {batchIdx}/{totalBatches}");

            return result;
        }
    }
}