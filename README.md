# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular, extensible dungeon generation engine written in modern C# (.NET 9).  
It procedurally creates complex, solvable dungeon layouts, populates them with AI-generated enemies and treasures, and exports structured JSON for use in games, visualization tools, or other projects.  
The engine is designed for flexibility, supporting both OpenAI and Ollama LLMs for content generation, and features robust logging and configuration systems.

---

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Guaranteed solvable main path from Entrance ➔ Exit.
  - Randomized side branches and optional loops for replayability.
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
  - **OpenAI GPT-4o-mini**: Generates fantasy enemy names and descriptions based on dungeon theme.
  - **Ollama Compatibility**: Easily switch to local LLMs via the `ILLMClient` interface.
  - LLM integration is fully abstracted, allowing future expansion or swapping of AI providers.

- **Serialization and Export**
  - Full dungeon (rooms, entities, connections) serialized to JSON.
  - DTOs (`DungeonData`, `RoomData`, `EntityData`) separate runtime logic from export format.

- **Console Map Visualization**
  - Dual-mode console printer:
    - Basic structural view (Entrance, Exit, Paths).
    - Detailed entity view (Treasures and Enemies).
  - Color-coded map legend for clarity.

- **Settings and Configuration**
  - `settings.json` manages API keys, LLM provider selection, and global settings.
  - Auto-generates a default settings file if missing.
  - Notifies user if OpenAI API key is not set or if LLM configuration is incomplete.

- **Robust Logging**
  - Pluggable logging via the `ILogger` interface.
  - Console logging included by default; easily extendable for file or remote logging.
  - Logs key events: dungeon generation steps, entity placement, LLM usage, and errors.

---

## 🏗️ Solution Structure

| Project                             | Purpose                                                                 |
|:-------------------------------------|:------------------------------------------------------------------------|
| **DynDungeonCrawler.Engine**         | Core engine: dungeon generation, room/entity structures, AI, serialization, logging. |
| **DynDungeonCrawler.GeneratorApp**   | Console runner: generates, populates, prints, and exports dungeons to JSON. |
| **DynDungeonCrawler.ConsoleApp**     | (Planned) Console-based application for interactive dungeon exploration.   |

**DynDungeonCrawler.Engine Project Folders**:

| Folder           | Purpose                                                                                  |
|:-----------------|:----------------------------------------------------------------------------------------|
| `Classes/`       | Core classes (`Dungeon`, `Room`, `Entity`, `Enemy`, `TreasureChest`, `OpenAIHelper`, etc.) |
| `Data/`          | DTOs for dungeon export/import (`DungeonData`, `RoomData`, `EntityData`)                |
| `Configuration/` | `Settings.cs` for managing OpenAI/Ollama keys and settings                              |
| `Constants/`     | Default values for dungeon generation and LLM prompts (`DungeonDefaults`, `LLMDefaults`)|
| `Interfaces/`    | `ILLMClient`, `ILogger` interfaces for AI and logging abstraction                      |
| `Factories/`     | Factories for generating enemies and other entities                                     |
| `Helpers/`       | Utility and helper classes (e.g., logging, OpenAI/Ollama integration)                   |

---

## 🤖 LLM (AI) Integration

- **Provider Abstraction:**  
  The engine uses the `ILLMClient` interface to abstract all LLM interactions.  
  - **OpenAI:** Out-of-the-box support for GPT-4o-mini (API key required).
  - **Ollama:** Easily add or switch to local LLMs (such as Llama 3) by implementing the interface.
- **Usage:**  
  LLMs are used to generate enemy names and descriptions, and can be extended for room descriptions or other content.
- **Configuration:**  
  Select and configure your LLM provider in `settings.json`.

---

## 📝 Logging

- **ILogger Interface:**  
  All logging is routed through the `ILogger` interface.
- **ConsoleLogger:**  
  Default implementation logs to the console, including color-coded messages for key events.
- **Extensibility:**  
  Swap in your own logger (file, remote, etc.) by implementing `ILogger`.
- **Coverage:**  
  Logs dungeon generation steps, entity placement, LLM requests/responses, and errors.

---

## 🚀 Future Goals

- **Player Movement and Exploration Mechanics**
- **Expanded Entity Types** (Bosses, Keys, NPCs, Magical Items)
- **Procedural Room Description Generation (LLM-powered)**
- **Dungeon Biomes and Theming** (lava caves, ice caverns, ancient ruins)
- **Graphical Front-End Rendering** (Unity, WebGL, custom renderers)
- **Enhanced Save/Load Systems** (partial or full dungeons)
- **Minimap and Smarter Pathfinding**
- **Interactive Events (Traps, Puzzles, Lore Drops)**
- **Full Ollama and other LLM provider support in UI**

---

## 🔹 Getting Started

> Basic familiarity with C# and Visual Studio 2022 or later is recommended.

1. Clone the repository.
2. Open the solution (`DynDungeonCrawler.sln`) in Visual Studio 2022 or newer.
3. Build the entire solution.
4. Set `DynDungeonCrawler.GeneratorApp` as the startup project.
5. Update your `settings.json`:
    - Set your OpenAI API key, or
    - Configure Ollama/local LLM settings as needed.
6. Run the console app to generate a dungeon, view maps, and export to JSON.

---

## 🗺️ Example Usage

When you run the generator app, you will be prompted for a dungeon theme.  
The app will generate a dungeon, populate it with AI-generated enemies and treasures, print two map views (paths only, and with entities), and export the dungeon to a JSON file in the `DungeonExports` folder.

---

> **Project Status:** Foundational systems complete — expanding into gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
