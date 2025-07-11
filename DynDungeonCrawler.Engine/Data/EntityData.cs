﻿using DynDungeonCrawler.Engine.Classes;

namespace DynDungeonCrawler.Engine.Data
{
    public class EntityData
    {
        public Guid Id { get; set; }
        public EntityType Type { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";

        // TreasureChest specific properties
        public bool? IsLocked { get; set; }

        public bool? IsOpened { get; set; }
        public TreasureType? TreasureType { get; set; }
        public int? TreasureValue { get; set; }

        // Enemy specific properties
        public int? Health { get; set; }

        public int? Strength { get; set; }
        public int MoneyReward { get; set; } = 0;
    }
}