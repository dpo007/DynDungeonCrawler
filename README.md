# DynDungeonCrawler

**DynDungeonCrawler** is a modular, dynamic dungeon generation engine built in C#.  
It procedurally creates complex dungeon layouts, populates them with enemies and treasure, dynamically generates content using AI (LLMs), and exports fully structured JSON for integration into games, visualization tools, or other projects.

---

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Creates a guaranteed solvable main path from Entrance ➔ Exit.
  - Adds randomized side branches and occasional loops.
  - Configurable maximum dungeon size (`MaxDungeonWidth`, `MaxDungeonHeight`) and minimum path length.

- **Room System**
  - Rooms have distinct types: Entrance, Exit, or Normal.
  - Each room has a unique ID (GUID) for external linking.
  - Tracks 4-way connectivity (north, south, east, west).
  - Designed for future environmental theming.

- **Entity System**
  - Entities include Enemies and Treasure Chests (more easily extensible).
  - Each Entity has a Name, Description, Type, and unique ID.
  - Randomized placement of entities in appropriate rooms.

- **Treasure and Loot System**
  - Treasure Chests contain randomly generated loot (Money, Gold, Jewels).
  - Loot value scales based on rarity.

- **LLM (AI) Integration**
  - Uses OpenAI's GPT-4o-mini to dynamically generate fantasy enemy names based on dungeon theme.
  - Built behind an `ILLMClient` interface for easy future AI swapping.

- **Serialization and Export**
  - Entire dungeon (rooms, entities, connections) serialized cleanly to JSON.
  - DTOs (`DungeonData`, `RoomData`, `EntityData`) separate runtime logic from export format.
  - Saved JSON enables easy integration into external front-end clients.

- **Console Map Visualization**
  - Dual-mode console printer:
    - Basic structural view (entrance, exit, path).
    - Detailed entity view (showing treasures and enemies).

- **Settings and Configuration**
  - `settings.json` file manages API keys and project settings.
  - Auto-creates default settings file if missing.

---

## 🏗️ Solution Structure

| Project Folder | Purpose |
|:---------------|:--------|
| `Classes` | Core runtime classes (Dungeon, Room, Entity, Enemy, TreasureChest, etc.) |
| `Data` | Serializable DTOs for export/import (DungeonData, RoomData, EntityData) |
| `Configuration` | Settings loader (settings.json manager) |
| `Interfaces` | `ILLMClient` for AI integration |
| `Constants` | Default values for Dungeon generation and LLM prompts |

---

## 🚀 Future Goals

- **Player Movement and Exploration Mechanics**
- **Expanded Entities** (Bosses, Keys, NPCs, Traps, Magical Items)
- **Procedural Room Descriptions** (flavor text generation)
- **Dungeon Theming and Biomes** (lava caves, ice caverns, ancient ruins)
- **Front-End Visualization** (Unity, WebGL, or custom rendering)
- **Save/Load Systems** (full or partial dungeon reloads)
- **Minimap and Smarter Pathfinding**
- **Event-Driven Room Effects** (traps, puzzles, environmental hazards)

---

## 🔹 Getting Started

> Basic familiarity with C# and Visual Studio 2022 recommended.

1. Clone the repository.
2. Open the solution (`DynDungeonCrawler.sln`) in Visual Studio.
3. Build and run the project.
4. Update your `settings.json` with your OpenAI API key if using LLM features.
5. Generate a dungeon, explore the maps, and review the exported JSON!

---

> **Project Status:** Foundational systems complete — expanding into gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
