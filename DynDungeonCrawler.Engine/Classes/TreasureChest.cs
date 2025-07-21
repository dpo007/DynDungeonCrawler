using DynDungeonCrawler.Engine.Data;
using DynDungeonCrawler.Engine.Models;

namespace DynDungeonCrawler.Engine.Classes
{
    public class TreasureChest : Entity
    {
        public bool IsLocked { get; set; } = false;
        public bool IsOpened { get; set; } = false;
        public bool IsGuarded { get; set; } = false;

        public Treasure? ContainedTreasure { get; private set; } = null;

        private static readonly Random random = Random.Shared;

        /// <summary>
        /// Standard constructor that generates a random treasure.
        /// </summary>
        /// <param name="name">The name of the chest.</param>
        /// <param name="isLocked">Whether the chest is locked.</param>
        public TreasureChest(string name = "Treasure Chest", bool isLocked = false)
            : base(EntityType.TreasureChest, name)
        {
            IsLocked = isLocked;
            GenerateTreasure();
        }

        /// <summary>
        /// Deserialization constructor that allows setting a specific treasure.
        /// </summary>
        /// <param name="name">The name of the chest.</param>
        /// <param name="isLocked">Whether the chest is locked.</param>
        /// <param name="treasureType">The type of treasure.</param>
        /// <param name="treasureValue">The value of the treasure.</param>
        public TreasureChest(string name, bool isLocked, TreasureType treasureType, int treasureValue)
            : base(EntityType.TreasureChest, name)
        {
            IsLocked = isLocked;
            ContainedTreasure = new Treasure(treasureType, treasureValue);
        }

        /// <summary>
        /// Unlocks the chest using a magical lock pick.
        /// </summary>
        /// <param name="lockPick">The magical lock pick to use.</param>
        /// <returns>True if the chest was successfully unlocked; otherwise, false.</returns>
        public bool UnlockWith(MagicalLockPick lockPick)
        {
            if (lockPick == null)
            {
                return false;
            }

            if (!IsLocked)
            {
                return false; // Already unlocked
            }

            IsLocked = false;
            return true;
        }

        /// <summary>
        /// Generates random treasure for the chest based on rarity probabilities.
        /// </summary>
        private void GenerateTreasure()
        {
            double roll = random.NextDouble();

            if (roll < 0.6)
            {
                ContainedTreasure = new Treasure(TreasureType.Money, random.Next(10, 501));
            }
            else if (roll < 0.9)
            {
                ContainedTreasure = new Treasure(TreasureType.Gold, random.Next(500, 5001));
            }
            else
            {
                ContainedTreasure = new Treasure(TreasureType.Jewels, random.Next(1000, 10001));
            }
        }

        /// <summary>
        /// Attempts to open the chest, checking for all blocking conditions.
        /// </summary>
        /// <returns>True if the chest was successfully opened; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the chest is already opened or if it's being guarded by enemies.
        /// </exception>
        public bool Open()
        {
            if (IsOpened)
            {
                throw new InvalidOperationException("Cannot open: chest is already opened.");
            }

            if (IsGuarded)
            {
                throw new InvalidOperationException("Cannot open: chest is being guarded by enemies. You must defeat them first.");
            }

            if (IsLocked)
            {
                return false; // Cannot open because it's locked
            }

            IsOpened = true;
            return true;
        }

        /// <summary>
        /// Checks if the chest can be opened in its current state.
        /// </summary>
        /// <returns>True if the chest can be opened; otherwise, false.</returns>
        public bool CanOpen()
        {
            return !IsOpened && !IsGuarded && !IsLocked;
        }

        /// <summary>
        /// Gets a status message describing the current state of the chest.
        /// </summary>
        /// <param name="enemies">Optional list of enemies guarding the chest for specific messaging.</param>
        /// <returns>A descriptive message about why the chest cannot be opened, or confirmation it's ready to open.</returns>
        public string GetStatusMessage(List<Enemy>? enemies = null)
        {
            if (IsOpened)
            {
                return $"The {Name} is already open and empty.";
            }

            if (IsGuarded && enemies != null && enemies.Count > 0)
            {
                return GetGuardedMessage(enemies);
            }
            else if (IsGuarded)
            {
                return $"The {Name} is being guarded by creatures. You must defeat them before accessing it.";
            }

            if (IsLocked)
            {
                return $"The {Name} is locked. You need a key or lock pick to open it.";
            }

            return $"The {Name} appears ready to be opened.";
        }

        /// <summary>
        /// Gets a game message describing the current state of the chest.
        /// </summary>
        /// <param name="enemies">Optional list of enemies guarding the chest for specific messaging.</param>
        /// <returns>A GameMessage with appropriate styling and content.</returns>
        public GameMessage GetStatusGameMessage(List<Enemy>? enemies = null)
        {
            if (IsOpened)
            {
                return GameMessage.Normal($"The {Name} is already open and empty.");
            }

            if (IsGuarded && enemies != null && enemies.Count > 0)
            {
                string enemyNames = FormatEnemyNames(enemies);
                return GameMessage.EnemyStatus(
                    $"The {Name} is being guarded by {enemyNames}! Defeat them before you can approach the chest."
                );
            }
            else if (IsGuarded)
            {
                return GameMessage.EnemyStatus($"The {Name} is being guarded by creatures. You must defeat them before accessing it.");
            }

            if (IsLocked)
            {
                return GameMessage.ItemInfo($"The {Name} is locked. You need a key or lock pick to open it.");
            }

            return GameMessage.Success($"The {Name} appears ready to be opened.");
        }

        /// <summary>
        /// Generates a specific guarded message listing the enemy names.
        /// </summary>
        /// <param name="enemies">List of enemies currently guarding the chest.</param>
        /// <returns>A message listing the specific enemies guarding the chest.</returns>
        private string GetGuardedMessage(List<Enemy> enemies)
        {
            if (enemies.Count == 1)
            {
                return $"The {Name} is being guarded by a {enemies[0].Name}! Defeat them before you can approach the chest.";
            }
            else if (enemies.Count == 2)
            {
                return $"The {Name} is being guarded by a {enemies[0].Name} and a {enemies[1].Name}! Defeat them before you can approach the chest.";
            }
            else
            {
                string lastEnemy = enemies[enemies.Count - 1].Name;
                string otherEnemies = string.Join(", a ", enemies.Take(enemies.Count - 1).Select(e => e.Name));
                return $"The {Name} is being guarded by a {otherEnemies}, and a {lastEnemy}! Defeat them before you can approach the chest.";
            }
        }

        /// <summary>
        /// Formats a list of enemy names into a readable string for GameMessage usage.
        /// </summary>
        /// <param name="enemies">List of enemies to format.</param>
        /// <returns>A formatted string listing the enemy names.</returns>
        private string FormatEnemyNames(List<Enemy> enemies)
        {
            if (enemies.Count == 0)
            {
                return "enemies";
            }

            if (enemies.Count == 1)
            {
                return enemies[0].Name;
            }

            if (enemies.Count == 2)
            {
                return $"a {enemies[0].Name} and a {enemies[1].Name}";
            }

            return string.Join(", ", enemies.Take(enemies.Count - 1).Select(e => e.Name))
                + $", and a {enemies[enemies.Count - 1].Name}";
        }

        /// <summary>
        /// Updates the guarded status of the chest based on whether enemies are present in the room.
        /// This should be called whenever enemies are defeated or when entering a room.
        /// </summary>
        /// <param name="enemiesPresent">True if enemies are present in the room; otherwise, false.</param>
        public void UpdateGuardedStatus(bool enemiesPresent)
        {
            IsGuarded = enemiesPresent;
        }

        /// <summary>
        /// Converts the entity to a data object for serialization.
        /// </summary>
        /// <returns>An EntityData object representing the entity.</returns>
        public override EntityData ToEntityData()
        {
            EntityData data = base.ToEntityData();
            data.IsLocked = IsLocked;
            data.IsOpened = IsOpened;
            data.IsGuarded = IsGuarded;
            data.TreasureType = ContainedTreasure?.Type;
            data.TreasureValue = ContainedTreasure?.Value;
            return data;
        }
    }
}