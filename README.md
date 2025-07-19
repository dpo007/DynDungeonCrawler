# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular C#/.NET 9 solution for procedural dungeon generation, interactive console exploration, and graphical map visualization.  
It creates complex, solvable dungeon layouts, populates them with AI-generated enemies and treasures, and exports structured JSON for integration into games, visualization tools, or other projects.  
The engine is highly configurable, supports multiple LLM providers, and features robust logging and configuration management.

---

## 🌟 Key Features

- **Procedural Dungeon Generation**
  - Guaranteed solvable main path from Entrance ➔ Exit
  - Randomized side branches and optional loops for replayability
  - Configurable maximum dungeon size and minimum path length

- **Room System**
  - Distinct room types: Entrance, Exit, Normal
  - Each room has a unique GUID for external linking
  - 4-way connectivity (north, south, east, west)
  - LLM-powered room descriptions generated on demand or in batch

- **Entity & Loot System**
  - Supports Enemies, Treasure Chests, Magical Lock Picks, and more (extensible)
  - All entities derive from the abstract base class `Entity` and are created via factories (`EntityFactory`, `EnemyFactory`, `TreasureChestFactory`, `LockPickFactory`, etc.)
  - Each entity has a Name, Description, Type, and unique ID
  - Randomized placement of entities in appropriate rooms
  - Treasure Chests contain randomly generated loot (Money, Gold, Jewels)
  - Loot value scales with rarity; some chests may be locked
  - Treasure Chests receive LLM-generated detailed and short descriptions, inspired by their room context
  - Easily extensible for new entity types: Bosses, Keys, NPCs, Magical Items, etc.

- **AI (LLM) Integration**
  - **OpenAI** and **Azure OpenAI**: Generate fantasy names, descriptions, and content based on dungeon themes
  - **Ollama Compatibility**: Switch to local LLMs via the `ILLMClient` abstraction
  - Used for enemies, rooms, adventurers, and dynamic world flavor
  - Efficient batching and parallelization for LLM calls (rooms and treasure chests)
  - Robust error handling and JSON validation for all LLM-generated content

- **Serialization and Export**
  - Full dungeon (rooms, entities, connections) serialized to JSON
  - Structured DTOs (`DungeonData`, `RoomData`, `EntityData`) separate runtime logic from export format

- **Console Map Visualization**
  - Dual-mode console printer:
    - Basic structural view (Entrance, Exit, Paths)
    - Detailed entity view (Treasures and Enemies)
  - Color-coded map legend for clarity

- **Graphical Map Viewer**
  - WPF-based tool for visualizing dungeons from JSON files
  - Color-coded display for rooms, paths, treasures, and enemies
  - Synchronized vertical and horizontal scrolling between map views

- **Interactive Console Dungeon Crawler**
  - Playable console game: load a dungeon, create an adventurer, and explore room by room
  - Player movement, inventory, and room/entity interaction
  - Room descriptions and names generated on-the-fly as you explore

- **Settings and Configuration**
  - Centralized LLM settings in `DynDungeonCrawler.Engine/Configuration/LLMSettings.cs` and `settings.json` (API keys, provider selection, etc.)
  - Each app manages its own settings file (e.g., `condugeon.settings.json` for ConDungeon, `generatorapp.settings.json` for GeneratorApp) for app-specific paths and options
  - Customizable entity placement probabilities in GeneratorApp (chest spawn chance, enemy chances, etc.)
  - All settings files are auto-generated and validated at startup; missing or invalid fields prompt user action
  - LLM settings are validated and shared across all projects via the engine

- **Robust Logging**
  - Pluggable logging via the `ILogger` interface
  - Console and file logging included; easily extensible for other targets
  - Logs key events: dungeon generation steps, entity placement, LLM usage, and errors

---

## 🏗️ Solution Structure

| Project                                 | Purpose                                                                 |
|:-----------------------------------------|:------------------------------------------------------------------------|
| **DynDungeonCrawler.Engine**             | Core engine: dungeon generation, room/entity structures, LLM integration, serialization, logging |
| **DynDungeonCrawler.GeneratorApp**       | Console runner: generates, populates, prints, and exports dungeons to JSON |
| **DynDungeonCrawler.ConDungeon**         | Interactive console dungeon crawler: play through a generated dungeon    |
| **DynDungeonCrawler.MapViewer**          | WPF graphical map viewer: load and visualize dungeon JSON files          |

**DynDungeonCrawler.Engine Project Folders**:

| Folder                       | Purpose                                                                                  |
|:-----------------------------|:----------------------------------------------------------------------------------------|
| `Classes/`                   | Core dungeon, room, entity, adventurer, enemy, treasure, magical item, lock pick types   |
| `Configuration/`             | Engine and LLM settings management                                                      |
| `Constants/`                 | Default values for dungeon generation and LLM prompts                                   |
| `Data/`                      | Data transfer objects for dungeon import/export                                         |
| `Factories/`                 | Entity creation logic (EnemyFactory, EntityFactory, TreasureChestFactory, LockPickFactory, etc.) |
| `Helpers/ContentGeneration/` | Content generation utilities (room descriptions, themes, adventurer save, chest stories) |
| `Helpers/LLM/`               | LLM integration helpers (OpenAI, Azure, Ollama, JSON cleaning, dummy client)            |
| `Helpers/Logging/`           | Logging utilities (ConsoleLogger, FileLogger, DebugLogger, MutedLogger)                 |
| `Helpers/UI/`                | Console and UI helpers (SpectreConsoleUserInterface, ConsoleUserInterface)              |
| `Interfaces/`                | Abstractions for LLM, logging, and UI                                                   |

**DynDungeonCrawler.GeneratorApp Project Folders**:

| Folder           | Purpose                                                                                  |
|:-----------------|:----------------------------------------------------------------------------------------|
| `Utilities/`     | General utilities for dungeon generation and app logic                                   |

**DynDungeonCrawler.GeneratorApp Settings**:

| Setting                     | Purpose                                                       | Default Value |
|:----------------------------|:--------------------------------------------------------------|:--------------|
| `MaxChestsPerRoom`          | Maximum treasure chests per room                              | 1             |
| `MaxEnemiesPerRoom`         | Maximum enemies per room                                      | 2             |
| `ChestSpawnChance`          | Probability (0-1) of a chest spawning in a room               | 0.10 (10%)    |
| `ChestLockChance`           | Probability (0-1) of a chest being locked                     | 0.30 (30%)    |
| `ChestRoomFirstEnemyChance` | Probability (0-1) of first enemy in a room with a chest       | 0.40 (40%)    |
| `ChestRoomSecondEnemyChance`| Probability (0-1) of second enemy in a room with a chest      | 0.05 (5%)     |
| `EmptyRoomFirstEnemyChance` | Probability (0-1) of first enemy in a room without a chest    | 0.10 (10%)    |
| `EmptyRoomSecondEnemyChance`| Probability (0-1) of second enemy in a room without a chest   | 0.03 (3%)     |
| `StrongestEnemyMinStrength` | Minimum strength for the enemy guarding the magical lock pick | 20            |

All these settings can be customized in the `generatorapp.settings.json` file, which is automatically created or updated when the application runs.

**DynDungeonCrawler.ConDungeon Project Folders**:

| Folder / File         | Purpose                                                                                  |
|:----------------------|:----------------------------------------------------------------------------------------|
| `Configuration/`      | Project-specific settings management                                                    |
| `ConDungeon.cs`       | Application entry point and main loop wiring                                            |
| `GameInitializer.cs`  | Game setup and initialization logic                                                    |
| `GameLoop/`           | Game loop logic: input handling, command processing, room rendering, main loop         |

**DynDungeonCrawler.MapViewer Project Folders**:

| File / Folder         | Purpose                                                                                  |
|:----------------------|:----------------------------------------------------------------------------------------|
| `MainWindow.xaml`     | Main WPF map viewer window                                                              |
| `MainWindow.xaml.cs`  | Map viewer logic                                                                        |
| `App.xaml`            | WPF application definition                                                              |
| `App.xaml.cs`         | WPF application startup logic                                                          |

---

## 🤖 LLM (AI) Integration

- **Provider Abstraction:**  
  The engine uses the `ILLMClient` interface to abstract all LLM interactions.
  - **OpenAI**: Built-in support via API key
  - **Azure OpenAI**: Native support via endpoint, deployment name, and key
  - **Ollama**: Easily switch to local LLMs (such as Llama 3) by implementing the interface
- **Usage:**  
  LLMs are used to generate enemy names and descriptions, room descriptions, treasure chest descriptions, and adventurer names
- **Batching and Parallelization:**  
  Room and treasure chest descriptions are generated in batches, with parallel LLM calls and automatic JSON validation/retry
- **Configuration:**  
  Centralized LLM settings in the engine; app-specific settings for file paths and options

---

## 📝 Logging

- **ILogger Interface:**  
  All logging is routed through the `ILogger` interface
- **ConsoleLogger and FileLogger:**  
  Console and file logging implementations included
- **Extensibility:**  
  Swap in your own logger (file, remote, etc.) by implementing `ILogger`
- **Coverage:**  
  Logs dungeon generation steps, entity placement, LLM requests/responses, and errors

---

## 🕹️ Interactive Console Dungeon Crawler

- **Play through a generated dungeon in the console**
- **Create or generate an adventurer (with LLM-powered names)**
- **Move between rooms, view descriptions, interact with treasures and enemies**
- **Room descriptions are generated on demand as you explore**
- **Inventory and basic player stats supported**
- **Game ends on player death or escape**

> The ConDungeon project is fully modular, with separate classes for game initialization, input handling, command processing, room rendering, and the main game loop.

---

## 🖼️ WPF Map Viewer

- **Load and visualize dungeon JSON files in a graphical interface**
- **View maps in two modes: paths only, or with entities**
- **Color-coded display for rooms, paths, treasures, and enemies**
- **Synchronized vertical and horizontal scrolling between map views**
- **Horizontal scrolling appears only when needed**

---

## 🚀 Future Goals

- Expanded entity types (Bosses, Keys, NPCs, Magical Items, Lock Picks, etc.)
- More advanced procedural room and entity description generation
- Dungeon biomes and theming (lava caves, ice caverns, ancient ruins)
- Graphical front-end rendering (Unity, WebGL, custom renderers)
- Enhanced save/load systems (partial or full dungeons)
- Minimap and smarter pathfinding
- Interactive events (traps, puzzles, lore drops)
- Full Ollama and other LLM provider support in UI

---

## 🔹 Getting Started

> Requires .NET 9.0 SDK and Visual Studio 2022 or later (or use the `dotnet` CLI).

1. Clone the repository.
2. Open the solution (`DynDungeonCrawler.sln`) in Visual Studio 2022 or newer.
3. Build the entire solution.
4. Set one of the following as the startup project:
    - `DynDungeonCrawler.GeneratorApp` (for generation/export)
    - `DynDungeonCrawler.ConDungeon` (for interactive play)
    - `DynDungeonCrawler.MapViewer` (for graphical map viewing)
5. Update your app-specific settings file:
    - Central LLM settings: `DynDungeonCrawler.Engine/Configuration/settings.json` and `LLMSettings.cs`
    - App settings: e.g., `condugeon.settings.json` for ConDungeon, `generatorapp.settings.json` for GeneratorApp (auto-generated and validated at startup)
    - Set your OpenAI or Azure OpenAI credentials, or configure Ollama/local LLM settings as needed
    - Set file paths and other options as needed
6. Run the selected app to generate a dungeon, play through it, or visualize/export to JSON.

---

## 🗺️ Example Usage

- **Dungeon Generation:**  
  Run the generator app, enter a dungeon theme, and export a dungeon to JSON.  
  View two map modes: structure-only and with entities.

- **Interactive Play:**  
  Run the ConDungeon app, load a generated dungeon, create or generate an adventurer, and explore room by room.  
  Room descriptions and names are generated as you explore, and you can interact with treasures and enemies.

- **WPF Map Viewer:**  
  Use the MapViewer app to load a dungeon JSON file and view the map in two modes (paths only, with entities).  
  Maps are color-coded and support synchronized scrolling.

---

> **Project Status:** Foundational systems complete — now featuring interactive exploration, graphical map viewing, and LLM-powered content generation. Expanding into deeper gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
