﻿using DynDungeonCrawler.Engine.Data;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Classes
{
    public class Enemy : Entity, ICombatable
    {
        public int Health { get; set; }
        public int Strength { get; set; }

        /// <summary>
        /// Gets the defense value of the enemy, used to reduce incoming damage.
        /// </summary>
        public int Defense { get; set; }

        public int MoneyReward { get; private set; } = 0;

        private static readonly Random random = Random.Shared;

        public Enemy(string name = "Monster", string description = "A scarey monster!", int health = 10, int strength = 2)
            : this(name, description, string.Empty, health, strength) { }

        public Enemy(string name, string description, string shortDescription, int health = 10, int strength = 2)
            : base(EntityType.Enemy, name)
        {
            if (health <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(health), "Health must be greater than zero.");
            }

            if (strength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be greater than zero.");
            }

            Description = description;
            ShortDescription = shortDescription;
            Health = health;
            Strength = strength;
            Defense = GenerateDefense(strength);
            MoneyReward = GenerateMoneyReward();
        }

        /// <summary>
        /// Constructor for deserialization that allows setting a specific money reward.
        /// </summary>
        public Enemy(string name, string description, string shortDescription, int health, int strength, int moneyReward)
            : base(EntityType.Enemy, name)
        {
            if (health <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(health), "Health must be greater than zero.");
            }

            if (strength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strength), "Strength must be greater than zero.");
            }

            Description = description;
            ShortDescription = shortDescription;
            Health = health;
            Strength = strength;
            Defense = GenerateDefense(strength);
            MoneyReward = moneyReward;
        }

        private static int GenerateDefense(int strength)
        {
            // Defense is typically around 30-50% of strength, with some randomness
            return (int)(strength * (0.3 + (random.NextDouble() * 0.2)));
        }

        private static int GenerateMoneyReward()
        {
            double roll = random.NextDouble();

            if (roll < 0.7)
            {
                return 0; // 70% chance no money
            }
            else if (roll < 0.9)
            {
                return random.Next(1, 101); // 1-100 gold
            }
            else
            {
                return random.Next(100, 501); // 100-500 gold
            }
        }

        /// <summary>
        /// Converts the enemy to a data object for serialization.
        /// </summary>
        /// <returns>An EntityData object representing the enemy.</returns>
        public override EntityData ToEntityData()
        {
            EntityData data = base.ToEntityData();
            data.Health = Health;
            data.Strength = Strength;
            data.Defense = Defense;
            data.MoneyReward = MoneyReward;
            return data;
        }
    }
}