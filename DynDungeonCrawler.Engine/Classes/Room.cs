using DynDungeonCrawler.Engine.Interfaces;
using System.Text.Json;

namespace DynDungeonCrawler.Engine.Classes
{
    public enum RoomType
    {
        Normal,
        Entrance,
        Exit
    }

    public class Room
    {
        public Guid Id { get; internal set; } // Unique ID for external linking
        public int X { get; set; }            // Grid X coordinate
        public int Y { get; set; }            // Grid Y coordinate
        public RoomType Type { get; set; }    // Type of room
        public string Description { get; set; } = string.Empty;
        private readonly object _contentsLock = new();
        public List<Entity> Contents { get; } = new();

        // Room connection flags
        // These can be used to determine if a room is connected to another room in a specific direction
        public bool ConnectedNorth { get; set; }

        public bool ConnectedEast { get; set; }
        public bool ConnectedSouth { get; set; }
        public bool ConnectedWest { get; set; }

        // Constructor with RoomType
        public Room(int x, int y, RoomType type)
        {
            // Validate coordinates
            if (x < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "X must be ≥ 0.");
            }

            if (y < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Y must be ≥ 0.");
            }

            // Validate RoomType
            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Invalid RoomType value.");
            }

            Id = Guid.NewGuid();
            X = x;
            Y = y;
            Type = type;
        }

        // Constructor without RoomType (defaults to Normal)
        public Room(int x, int y) : this(x, y, RoomType.Normal)
        {
        }

        /// <summary>
        /// Adds an Entity to the room, enforcing validation rules.
        /// Throws an exception if the entity is null or if an entity with the same ID already exists in the room.
        /// </summary>
        /// <param name="entity">The Entity to add to the room.</param>
        /// <exception cref="ArgumentNullException">Thrown if the entity is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an entity with the same ID already exists in the room.</exception>
        public void AddEntity(Entity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            lock (_contentsLock)
            {
                if (Contents.Any(e => e.Id == entity.Id))
                {
                    throw new InvalidOperationException("Entity with the same ID already exists in the room.");
                }

                Contents.Add(entity);
            }
        }

        /// <summary>
        /// Removes an Entity from the room by its ID.
        /// </summary>
        /// <param name="entityId">The unique identifier of the Entity to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntityById(Guid entityId)
        {
            lock (_contentsLock)
            {
                Entity? entity = Contents.FirstOrDefault(e => e.Id == entityId);
                if (entity != null)
                {
                    Contents.Remove(entity);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Removes an Entity from the room by its object reference.
        /// </summary>
        /// <param name="entity">The Entity object to remove.</param>
        /// <returns>True if the Entity was found and removed; otherwise, false.</returns>
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null)
            {
                return false;
            }

            lock (_contentsLock)
            {
                return Contents.Remove(entity);
            }
        }

        /// <summary>
        /// Returns a list of rooms that are directly accessible from this room.
        /// A room is considered directly accessible if it is adjacent in the grid
        /// and there is an exit from this room to that room (i.e., the corresponding
        /// Connected* property is true).
        /// </summary>
        /// <param name="grid">The 2D array of rooms representing the dungeon layout.</param>
        /// <returns>A list of directly accessible neighbouring rooms.</returns>
        public List<Room> GetAccessibleNeighbours(Room[,] grid)
        {
            List<Room> neighbours = new List<Room>();
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            // North
            if (ConnectedNorth && Y > 0)
            {
                Room north = grid[X, Y - 1];
                if (north != null)
                {
                    neighbours.Add(north);
                }
            }
            // East
            if (ConnectedEast && X < width - 1)
            {
                Room east = grid[X + 1, Y];
                if (east != null)
                {
                    neighbours.Add(east);
                }
            }
            // South
            if (ConnectedSouth && Y < height - 1)
            {
                Room south = grid[X, Y + 1];
                if (south != null)
                {
                    neighbours.Add(south);
                }
            }
            // West
            if (ConnectedWest && X > 0)
            {
                Room west = grid[X - 1, Y];
                if (west != null)
                {
                    neighbours.Add(west);
                }
            }

            return neighbours;
        }

        public Room? GetNeighbour(RoomDirection direction, Room[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            return direction switch
            {
                RoomDirection.North when ConnectedNorth && Y > 0 => grid[X, Y - 1],
                RoomDirection.East when ConnectedEast && X < width - 1 => grid[X + 1, Y],
                RoomDirection.South when ConnectedSouth && Y < height - 1 => grid[X, Y + 1],
                RoomDirection.West when ConnectedWest && X > 0 => grid[X - 1, Y],
                _ => null
            };
        }

        /// <summary>
        /// Generates vivid, concise fantasy descriptions for a list of rooms using an LLM client.
        /// </summary>
        /// <param name="rooms">The list of Room instances to generate descriptions for.</param>
        /// <param name="theme">The dungeon theme to inspire the descriptions.</param>
        /// <param name="llmClient">The LLM client used to generate the descriptions.</param>
        /// <param name="logger">The logger for recording generation events.</param>
        /// <param name="allowClobber">If true, will overwrite existing descriptions; otherwise, only rooms with empty descriptions are processed.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task GenerateRoomDescriptionsAsync(
           List<Room> rooms,
           string theme,
           ILLMClient llmClient,
           ILogger logger,
           bool allowClobber = false)
        {
            // Determine which rooms need new descriptions.
            // If allowClobber is true, process all rooms; otherwise, only those with empty descriptions.
            List<Room> targetRooms = allowClobber
                ? rooms
                : rooms.Where(r => string.IsNullOrWhiteSpace(r.Description)).ToList();

            // If there are no rooms to process, log and exit early.
            if (targetRooms.Count == 0)
            {
                logger.Log("No rooms require description generation.");
                return;
            }

            // Prepare the request object for the LLM, including theme and minimal room data.
            // Each room includes its ID, type, and available exits.
            var request = new
            {
                theme,
                rooms = targetRooms.Select(r => new
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

            // Serialize the request object to JSON for the LLM prompt.
            string inputJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });

            // Build the user prompt for the LLM, instructing it to generate descriptions
            // and to always mention and describe available exits.
            string userPrompt = $@"
Given the following dungeon theme and room data in JSON, generate a vivid, concise fantasy description for each room.

For each room:
- Always mention the available exits (north, east, south, west).
- For each exit, describe the **physical appearance** of the doorway, arch, gate, or passage — including material, style, damage, light, markings, etc.
- Do **not** describe what lies beyond the exit — focus only on the portal itself.

Return the same JSON structure, but add a 'description' field to each room, generated based on the theme, room type, and exits.

Do not change the IDs or exits. Only return valid JSON, with no markdown formatting.

    {inputJson}";

            // System prompt to guide the LLM's behavior.
            string systemPrompt = "You are a creative fantasy room description generator for RPG dungeons.";

            // Send the prompt to the LLM and await the generated response.
            string llmResponse = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            // Parse the LLM's JSON response with up to 5 retries if parsing fails.
            const int maxParseAttempts = 5;
            int attempt = 0;
            JsonDocument? doc = null;
            while (attempt < maxParseAttempts)
            {
                try
                {
                    doc = JsonDocument.Parse(llmResponse);
                    break; // Success
                }
                catch (JsonException ex)
                {
                    attempt++;
                    if (attempt >= maxParseAttempts)
                    {
                        logger.Log($"Failed to parse LLM response after {maxParseAttempts} attempts: {ex.Message}");
                        // log promtps and response for debugging
                        logger.Log($"User Prompt: {userPrompt}");
                        logger.Log($"LLM Response: {llmResponse}");
                        throw;
                    }

                    // Log the error and retry after a short delay
                    logger.Log($"JSON parse failed (attempt {attempt}): {ex.Message}. Retrying...");
                    await Task.Delay(100 * attempt); // Optional: backoff before retrying
                }
            }

            if (doc == null)
            {
                throw new InvalidOperationException("Failed to parse LLM response after retries.");
            }

            using (doc)
            {
                JsonElement responseRooms = doc.RootElement.GetProperty("rooms");

                // Build a dictionary mapping room IDs to their generated descriptions.
                Dictionary<Guid, string> descById = responseRooms.EnumerateArray()
                    .ToDictionary(
                        r => r.GetProperty("id").GetGuid(),
                        r => r.TryGetProperty("description", out JsonElement desc) ? desc.GetString() ?? "" : ""
                    );

                // Update each target room's Description property with the generated text, if available.
                foreach (Room room in targetRooms)
                {
                    if (descById.TryGetValue(room.Id, out string? desc) && !string.IsNullOrWhiteSpace(desc))
                    {
                        room.Description = desc.Trim();
                        logger.Log($"Generated description for room {room.Id}");
                    }
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the Room.
        /// </summary>
        /// <returns>A string describing the Room's type, ID, and coordinates.</returns>
        public override string ToString()
        {
            return $"{Type} Room [{Id}] at ({X}, {Y})";
        }
    }
}