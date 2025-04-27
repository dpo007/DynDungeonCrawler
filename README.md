# DynDungeonCrawler

**DynDungeonCrawler** is a dynamic, procedural dungeon generator and simulation engine written in C#.  
It is designed for creating complex dungeon layouts, populating them with entities like enemies and treasures, and exporting the result to JSON for integration into games, visualization tools, or other projects.

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Generates a main path from Entrance to Exit.
  - Adds random side branches and occasional loops.
  - Configurable dungeon size and minimum path length.

- **Room System**
  - Rooms are placed on a dynamic grid (size can scale as needed).
  - Each room has a type (Entrance, Exit, Normal) and a unique ID.
  - Connection metadata: tracks open passages north, south, east, west.

- **Entity System**
  - Entities include Enemies, Treasure Chests, Traps, Keys, NPCs.
  - Entities are extensible and assigned to rooms during population.

- **Treasure Chests and Loot System**
  - Chests can be locked or unlocked.
  - Randomized loot generation (money, gold bars, jewels) with varying value ranges.

- **Serialization Support**
  - Full dungeon state (rooms, entities, connections) can be exported as JSON.
  - Designed for easy re-import and front-end consumption.

- **Console Map Visualization**
  - Prints dungeon layout to the console.
  - Can toggle between basic structure view and entity view (showing treasures and enemies).

## 🚀 Future Goals

- **Player Movement and Exploration Mechanics**
  - Adding a player object to explore the generated dungeon.

- **Expanded Entity Types**
  - Bosses, magical items, traps with effects, puzzles, friendly NPCs.

- **Room Theme Variations**
  - Environmental themes (ice caves, lava pits, ancient temples) that affect room behavior and visuals.

- **Procedural Room Description Generation**
  - Dynamic flavor text and descriptions based on room type, contents, and theme.

- **Dungeon Theme Metadata**
  - Dungeons will have an overarching "theme" (e.g., "Forgotten Catacombs of a Lost Civilization").

- **Front-End Rendering Integration**
  - Support for graphical front-end clients (e.g., Unity, web browsers).

- **Enhanced Save/Load Systems**
  - JSON import/export allowing persistent worlds and reloading partial dungeons.

- **Minimap and Pathfinding Enhancements**
  - Generation of player-friendly minimaps and smarter enemy movement.

## 🔹 Getting Started

> Note: Basic familiarity with C# and Visual Studio 2022 is recommended.

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Build and run the project to generate a random dungeon and display it in the console.
4. Review the generated JSON file for dungeon data output.

---

> **Project Status:** Actively evolving — foundational systems complete, expanding into gameplay and worldbuilding next!

---

✨ _Designed to turn grids into grand adventures!_ ✨

