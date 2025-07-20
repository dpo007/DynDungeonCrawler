using DynDungeonCrawler.Engine.Data;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Classes
{
    public enum AdventurerGender
    {
        Unspecified,
        Male,
        Female
    }

    public class Adventurer : Entity, ICombatable
    {
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Defense { get; set; }
        public int Wealth { get; private set; }
        public List<Entity> Inventory { get; private set; }
        public Room? CurrentRoom { get; set; }
        public Room? PreviousRoom { get; set; }
        public HashSet<Guid> VisitedRoomIds { get; } = new();

        /// <summary>
        /// Gets a value indicating whether the adventurer is in a defensive stance.
        /// </summary>
        public bool IsDefending { get; private set; }

        public Adventurer(string name)
            : base(EntityType.Adventurer, name)
        {
            Health = 100;
            Strength = 5;
            Defense = 0;
            Wealth = 0;
            Inventory = new List<Entity>();
            CurrentRoom = null;
            IsDefending = false;
        }

        public Adventurer(string name, Room currentRoom) : this(name)
        {
            CurrentRoom = currentRoom;
            if (currentRoom != null)
            {
                VisitedRoomIds.Add(currentRoom.Id);
            }
        }

        /// <summary>
        /// Uses the LLM to generate a fantasy adventurer name based on the dungeon theme and optional gender.
        /// </summary>
        /// <param name="llmClient">The LLM client to use for name generation.</param>
        /// <param name="theme">The dungeon theme to inspire the name.</param>
        /// <param name="gender">Optional: Male, Female, or Unspecified (default).</param>
        /// <returns>A generated adventurer name.</returns>
        public static async Task<string> GenerateNameAsync(
            ILLMClient llmClient,
            string theme,
            AdventurerGender gender = AdventurerGender.Unspecified)
        {
            if (llmClient == null)
            {
                throw new ArgumentNullException(nameof(llmClient));
            }

            if (string.IsNullOrWhiteSpace(theme))
            {
                throw new ArgumentException("Theme must be provided.", nameof(theme));
            }

            string genderPrompt = gender switch
            {
                AdventurerGender.Male => "male",
                AdventurerGender.Female => "female",
                _ => "any gender"
            };

            string userPrompt = $"Generate a unique, fantasy-style {genderPrompt} adventurer name suitable for a dungeon themed '{theme}'. " +
                                "Return only the name, no description or extra text.";

            // Use a simple system prompt for consistency
            string systemPrompt = "You are a creative fantasy name generator for RPG characters.";

            string name = await llmClient.GetResponseAsync(userPrompt, systemPrompt);

            // Clean up the name (remove quotes, trim whitespace)
            return name?.Trim(' ', '\"', '\n', '\r') ?? "Adventurer";
        }

        /// <summary>
        /// Sets the adventurer's defending status.
        /// </summary>
        /// <param name="isDefending">True to enter defensive stance; false to exit it.</param>
        public void SetDefendingStatus(bool isDefending)
        {
            IsDefending = isDefending;
        }

        /// <summary>
        /// Adds an amount to the adventurer's wealth.
        /// </summary>
        /// <param name="amount">Amount to add.</param>
        public void AddWealth(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be a positive number.", nameof(amount));
            }

            Wealth += amount;
        }

        /// <summary>
        /// Subtracts an amount from the adventurer's wealth.
        /// </summary>
        /// <param name="amount">Amount to subtract.</param>
        public void SubtractWealth(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be a positive number.", nameof(amount));
            }

            if (Wealth >= amount)
            {
                Wealth -= amount;
            }
            else
            {
                Wealth = 0; // Set wealth to zero if the amount exceeds current wealth
            }
        }

        /// <summary>
        /// Drops an entity from the adventurer's inventory into the current room's contents.
        /// </summary>
        /// <param name="entity">The entity to drop.</param>
        /// <returns>True if the entity was successfully dropped; otherwise, false.</returns>
        public bool DropEntity(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }

            if (CurrentRoom == null)
            {
                throw new InvalidOperationException("The adventurer is not in a room.");
            }

            if (Inventory.Remove(entity))
            {
                CurrentRoom.Contents.Add(entity);
                return true;
            }

            return false; // Entity was not in the inventory
        }

        /// <summary>
        /// Picks up an entity from the current room's contents and adds it to the adventurer's inventory.
        /// </summary>
        /// <param name="entity">The entity to pick up.</param>
        /// <returns>True if the entity was successfully picked up; otherwise, false.</returns>
        public bool PickUpEntity(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }

            if (CurrentRoom == null)
            {
                throw new InvalidOperationException("The adventurer is not in a room.");
            }

            if (CurrentRoom.Contents.Remove(entity))
            {
                Inventory.Add(entity);
                return true;
            }

            return false; // Entity was not found in the room's contents
        }

        /// <summary>
        /// Converts the adventurer to a data object for serialization.
        /// </summary>
        /// <returns>An EntityData object representing the adventurer.</returns>
        public override EntityData ToEntityData()
        {
            EntityData data = base.ToEntityData();
            data.Health = Health;
            data.Strength = Strength;
            data.Defense = Defense;
            data.Wealth = Wealth;
            return data;
        }
    }
}