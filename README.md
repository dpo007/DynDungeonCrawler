# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular, dynamic dungeon generation and gameplay engine built in C#.  
It procedurally creates complex dungeon layouts, populates them with enemies and treasures, dynamically generates content using AI (LLMs), and exports fully structured JSON for integration into games, visualization tools, or future front-ends.

---

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Guaranteed solvable main path (Entrance ➔ Exit).
  - Random side branches, occasional loops.
  - Configurable dungeon dimensions and path lengths.

- **Room System**
  - Distinct room types (Entrance, Exit, Normal).
  - 4-way directional connectivity (north, south, east, west).
  - Unique GUID for external linking.

- **Entity System**
  - Unified base class for Enemies, Treasure Chests, Adventurers, etc.
  - Randomized room population.
  - Extendable for future entity types (traps, NPCs, items).

- **Adventurer System**
  - Player-controlled character with Health, Attack, Defense, Inventory, and Wealth tracking.
  - Future-ready for movement, combat, and inventory management.

- **Treasure and Loot**
  - TreasureChests contain random Money, Gold, or Jewels.
  - Wealth accumulation system for the Adventurer.

- **LLM (AI) Integration**
  - OpenAI GPT-4o-mini used for generating enemy names based on dungeon theme.
  - Abstracted through `ILLMClient` interface for easy swapping.

- **Serialization**
  - Full dungeon (Rooms + Entities + Connections) exported cleanly to JSON.
  - Runtime and export formats separated with DTOs.

- **Console Visualization**
  - Dual mode console printer:
    - Basic structure (Entrance/Exit/Paths).
    - Detailed map (Entities: Enemies, Treasures).

- **Settings Management**
  - API keys and configuration handled via `settings.json`.
  - Auto-creates settings file if missing.

---

## 🏗️ Solution Structure

| Project | Purpose |
|:--------|:--------|
| **DynDungeonCrawler.Engine** | Core engine: dungeon generation, entities, serialization, and AI integration. |
| **DynDungeonCrawler.GeneratorApp** | Console app for dungeon generation, printing, and exporting. |
| **DynDungeonCrawler.ConsoleApp** | (Planned) Front-end for playing dungeons interactively via text UI. |

**Engine Organization**:

| Folder | Purpose |
|:-------|:--------|
| `Classes/` | Core classes (`Dungeon`, `Room`, `Entity`, `Enemy`, `TreasureChest`, `Adventurer`, etc.) |
| `Data/` | Data Transfer Objects (`DungeonData`, `RoomData`, `EntityData`) |
| `Configuration/` | Settings management (`Settings.cs`) |
| `Constants/` | Default parameters (`DungeonDefaults`, `LLMDefaults`) |
| `Interfaces/` | LLM abstraction (`ILLMClient`) |
| `Services/` | AI helpers (`OpenAIHelper`) |

---

## 🚀 Roadmap

- Player Movement and Exploration
- Combat Mechanics (Attack/Defend vs Enemies)
- Inventory System (Keys, Potions, Equipment)
- Bosses, Traps, and Interactive Objects
- Procedural Room Descriptions and Lore
- Dungeon Themes (e.g., Lava, Ice, Ruins)
- Save/Load Adventure Sessions
- Graphical Front-End Rendering (Unity, Web, etc.)

---

## 🔹 Getting Started

> Basic familiarity with C# and Visual Studio 2022+ recommended.

1. Clone the repository.
2. Open the `DynDungeonCrawler.sln` solution in Visual Studio.
3. Build the entire solution.
4. Set `DynDungeonCrawler.GeneratorApp` as the startup project.
5. Update your `settings.json` with your OpenAI API key if using LLM features.
6. Run to generate, view, and export your first dungeon!

---

> **Project Status:** Core systems in place — expanding into gameplay mechanics and adventure features! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
