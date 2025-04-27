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

- **Entity System**
  - Entities include Enemies and Treasure Chests (easily extensible).
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

- **Console Map Visualization**
  - Dual-mode console printer:
    - Basic structure (Entrance, Exit, Path Arrows)
    - Detailed entities (Treasures, Enemies)

- **Settings and Configuration**
  - `settings.json` manages API keys and global project settings.
  - Auto-generates default settings file if missing.

---

## 🏗️ Solution Structure

| Project | Purpose |
|:--------|:--------|
| **DynDungeonCrawler.ConsoleApp** | Console runner to generate, populate, and display dungeons. |
| **DynDungeonCrawler.Generator** | Core engine library: dungeon logic, entities, AI integration, serialization. |
| **DynDungeonCrawler.GeneratorApp** | Future expansion space (e.g., editor tools, importers, alternate visualizers). |

**DynDungeonCrawler.Generator Project Folders**:

| Folder | Purpose |
|:-------|:--------|
| `Classes/` | Core runtime classes (Dungeon, Room, Entity, Enemy, TreasureChest, etc.) |
| `Data/` | DTOs for dungeon export/import |
| `Configuration/` | Settings management |
| `Constants/` | Default values (DungeonDefaults, LLMDefaults) |
| `Interfaces/` | Abstracted LLM client (`ILLMClient`) |

---

## 🚀 Future Goals

- **Player Movement and Exploration Mechanics**
- **Expanded Entity Types** (Bosses, Keys, NPCs, Magical Items)
- **Procedural Room Description Generation**
- **Dungeon Biomes and Theming** (ice caverns, lava dungeons, temples)
- **Graphical Front-End Rendering** (Unity, WebGL, etc.)
- **Enhanced Save/Load Systems** (partial or full dungeons)
- **Minimap and Smarter Pathfinding**
- **Interactive Events (Traps, Puzzles, Lore Drops)**

---

## 🔹 Getting Started

> Basic familiarity with C# and Visual Studio 2022 recommended.

1. Clone the repository.
2. Open the solution (`DynDungeonCrawler.sln`) in Visual Studio.
3. Build the entire solution.
4. Set `DynDungeonCrawler.ConsoleApp` as the startup project.
5. Update your `settings.json` with your OpenAI API key if using LLM features.
6. Run the console app to generate a dungeon, view the maps, and export to JSON.

---

> **Project Status:** Foundational systems complete — expanding into gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
