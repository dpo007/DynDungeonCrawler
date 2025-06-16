namespace DynDungeonCrawler.Engine.Classes;

public class Adventurer : Entity
{
    public int Health { get; set; }
    public int Strength { get; set; }
    public int Armour { get; set; }
    public int Wealth { get; private set; }
    public List<Entity> Inventory { get; private set; }
    public Room? CurrentRoom { get; set; }

    public Adventurer(string name)
        : base(EntityType.Adventurer, name)
    {
        Health = 100;
        Strength = 5;
        Armour = 0;
        Wealth = 0;
        Inventory = new List<Entity>();
        CurrentRoom = null;
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
}