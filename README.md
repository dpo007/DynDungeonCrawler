# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular, dynamic dungeon generation engine built in C# (.NET 9).  
It procedurally creates complex dungeon layouts, populates them with enemies and treasures, leverages AI (LLMs) for dynamic content, and exports structured JSON for integration into games, visualization tools, or other projects.

---

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Guaranteed solvable main path from Entrance ➔ Exit.
  - Randomized side branches and occasional loops for replayability.
  - Configurable maximum dungeon size and minimum path length.

- **Room System**
  - Distinct room types: Entrance, Exit, Normal.
  - Each room has a unique GUID for external linking.
  - 4-way connectivity (north, south, east, west).

- **Entity System**
  - Supports Enemies and Treasure Chests (easily extensible).
  - Each entity has a Name, Description, Type, and unique ID.
  - Randomized placement of entities in appropriate rooms.

- **Treasure and Loot System**
  - Treasure Chests contain randomly generated loot (Money, Gold, Jewels).
  - Loot value scales with rarity; some chests may be locked.

- **AI (LLM) Integration**
  - Uses OpenAI's GPT-4o-mini to generate fantasy enemy names/descriptions based on dungeon theme.
  - LLM integration is abstracted behind the `ILLMClient` interface for easy swapping.

- **Serialization and Export**
  - Full dungeon (rooms, entities, connections) serialized to JSON.
  - DTOs (`DungeonData`, `RoomData`, `EntityData`) separate runtime logic from export format.

- **Console Map Visualization**
  - Dual-mode console printer:
    - Basic structural view (Entrance, Exit, Paths).
    - Detailed entity view (Treasures and Enemies).
  - Color-coded map legend for clarity.

- **Settings and Configuration**
  - `settings.json` manages API keys and global settings.
  - Auto-generates a default settings file if missing.
  - Notifies user if OpenAI API key is not set.

---

## 🏗️ Solution Structure

| Project                        | Purpose                                                                 |
|:-------------------------------|:------------------------------------------------------------------------|
| **DynDungeonCrawler.Engine**    | Core engine: dungeon generation, room/entity structures, AI, serialization. |
| **DynDungeonCrawler.GeneratorApp** | Console runner: generates, populates, prints, and exports dungeons to JSON. |
| **DynDungeonCrawler.ConsoleApp**   | (Planned) Console-based application for interactive dungeon exploration.   |

**DynDungeonCrawler.Engine Project Folders**:

| Folder           | Purpose                                                                                  |
|:-----------------|:----------------------------------------------------------------------------------------|
| `Classes/`       | Core classes (`Dungeon`, `Room`, `Entity`, `Enemy`, `TreasureChest`, `OpenAIHelper`, etc.) |
| `Data/`          | DTOs for dungeon export/import (`DungeonData`, `RoomData`, `EntityData`)                |
| `Configuration/` | `Settings.cs` for managing OpenAI keys and settings                                     |
| `Constants/`     | Default values for dungeon generation and LLM prompts (`DungeonDefaults`, `LLMDefaults`)|
| `Interfaces/`    | `ILLMClient` interface for abstracting AI integrations                                  |
| `Factories/`     | Factories for generating enemies and other entities                                     |
| `Helpers/`       | Utility and helper classes (e.g., logging, OpenAI integration)                          |

---

## 🚀 Future Goals

- **Player Movement and Exploration Mechanics**
- **Expanded Entity Types** (Bosses, Keys, NPCs, Magical Items)
- **Procedural Room Description Generation**
- **Dungeon Biomes and Theming** (lava caves, ice caverns, ancient ruins)
- **Graphical Front-End Rendering** (Unity, WebGL, custom renderers)
- **Enhanced Save/Load Systems** (partial or full dungeons)
- **Minimap and Smarter Pathfinding**
- **Interactive Events (Traps, Puzzles, Lore Drops)**

---

## 🔹 Getting Started

> Basic familiarity with C# and Visual Studio 2022 is recommended.

1. Clone the repository.
2. Open the solution (`DynDungeonCrawler.sln`) in Visual Studio 2022 or later.
3. Build the entire solution.
4. Set `DynDungeonCrawler.GeneratorApp` as the startup project.
5. Update your `settings.json` with your OpenAI API key to enable LLM features.
6. Run the console app to generate a dungeon, view maps, and export to JSON.

---

> **Project Status:** Foundational systems complete — expanding into gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
