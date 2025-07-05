# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular C# engine for procedural dungeon generation, interactive console exploration, and graphical map visualization.  
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
  - LLM-powered room descriptions generated on demand during exploration

- **Entity & Loot System**
  - Supports Enemies and Treasure Chests (easily extensible)
  - Each entity has a Name, Description, Type, and unique ID
  - Randomized placement of entities in appropriate rooms
  - Treasure Chests contain randomly generated loot (Money, Gold, Jewels)
  - Loot value scales with rarity; some chests may be locked

- **AI (LLM) Integration**
  - **OpenAI** and **Azure OpenAI**: Generate fantasy names, descriptions, and content based on dungeon themes
  - **Ollama Compatibility**: Switch to local LLMs via the `ILLMClient` abstraction
  - Easily extend or swap AI providers without modifying engine code
  - Used for enemies, rooms, adventurers, and dynamic world flavor

- **Serialization and Export**
  - Full dungeon (rooms, entities, connections) serialized to JSON
  - DTOs (`DungeonData`, `RoomData`, `EntityData`) separate runtime logic from export format

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
  - `settings.json` manages API keys, LLM provider selection (OpenAI, Azure OpenAI, Ollama), and global settings
  - Includes:
    - `OpenAIApiKey`, `AzureOpenAIApiKey`, `AzureOpenAIEndpoint`, `AzureOpenAIDeployment`
    - `LogFilePath`: Path to the log file for file-based logging
    - `DungeonFilePath`: Default path to the dungeon JSON file
    - `LLMProvider`: Selects the LLM provider (e.g., OpenAI, Azure, Ollama)
  - Auto-generates a default settings file if missing
  - Validates LLM configuration and alerts if missing or incomplete

- **Robust Logging**
  - Pluggable logging via the `ILogger` interface
  - Console and file logging included; easily extendable for other targets
  - Logs key events: dungeon generation steps, entity placement, LLM usage, and errors

---

## 🏗️ Solution Structure

| Project                                 | Purpose                                                                 |
|:-----------------------------------------|:------------------------------------------------------------------------|
| **DynDungeonCrawler.Engine**             | Core engine: dungeon generation, room/entity structures, AI, serialization, logging |
| **DynDungeonCrawler.GeneratorApp**       | Console runner: generates, populates, prints, and exports dungeons to JSON |
| **DynDungeonCrawler.ConDungeon**         | Interactive console dungeon crawler: play through a generated dungeon    |
| **DynDungeonCrawler.MapViewer**          | WPF graphical map viewer: load and visualize dungeon JSON files          |

**DynDungeonCrawler.Engine Project Folders**:

| Folder           | Purpose                                                                                  |
|:-----------------|:----------------------------------------------------------------------------------------|
| `Classes/`       | Core classes (`Dungeon`, `Room`, `Entity`, `Enemy`, `TreasureChest`, `Adventurer`, etc.)|
| `Data/`          | DTOs for dungeon export/import (`DungeonData`, `RoomData`, `EntityData`)                |
| `Configuration/` | `Settings.cs` for managing OpenAI/Ollama/Azure keys and settings                        |
| `Constants/`     | Default values for dungeon generation and LLM prompts (`DungeonDefaults`, `LLMDefaults`)|
| `Interfaces/`    | `ILLMClient`, `ILogger` interfaces for AI and logging abstraction                       |
| `Helpers/`       | Logging and LLM integration helpers (e.g., `ConsoleLogger`, `FileLogger`, `LLMClientBase`) |
| `Factories/`     | Entity factories (e.g., `TreasureChestFactory`, `EnemyFactory`)                         |

**DynDungeonCrawler.GeneratorApp Project Folders**:

| Folder           | Purpose                                                                                  |
|:-----------------|:----------------------------------------------------------------------------------------|
| `Utilities/`     | Utility classes for dungeon generation and app logic                                    |

**DynDungeonCrawler.ConDungeon Project Folders**:

| Folder / File         | Purpose                                                                                  |
|:----------------------|:----------------------------------------------------------------------------------------|
| `GameLoop/`           | Modular game loop logic: input handling, command processing, room rendering, main loop  |
| &nbsp;&nbsp;└─ `InputHandler.cs`      | Handles all player input and command key validation                        |
| &nbsp;&nbsp;└─ `CommandProcessor.cs`  | Processes player commands, movement, inventory, and look actions           |
| &nbsp;&nbsp;└─ `RoomRenderer.cs`      | Renders the current room and its contents to the UI                        |
| &nbsp;&nbsp;└─ `GameLoopRunner.cs`    | Orchestrates the main game loop                                            |
| `GameInitializer.cs`  | Handles all game setup and initialization logic                                         |
| `ConDungeon.cs`       | Entry point; wires up initialization and the main game loop                             |

---

## 🤖 LLM (AI) Integration

- **Provider Abstraction:**  
  The engine uses the `ILLMClient` interface to abstract all LLM interactions.
  - **OpenAI**: Built-in support via API key
  - **Azure OpenAI**: Native support via endpoint, deployment name, and key
  - **Ollama**: Easily switch to local LLMs (such as Llama 3) by implementing the interface
- **Usage:**  
  LLMs are used to generate enemy names and descriptions, room descriptions, and adventurer names
- **Configuration:**  
  Choose your LLM provider and set credentials in `settings.json`

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

> The ConDungeon project is now fully modular, with separate classes for game initialization, input handling, command processing, room rendering, and the main game loop. This makes the codebase easier to maintain and extend.

---

## 🖼️ WPF Map Viewer

- **Load and visualize dungeon JSON files in a graphical interface**
- **View maps in two modes: paths only, or with entities**
- **Color-coded display for rooms, paths, treasures, and enemies**
- **Synchronized vertical and horizontal scrolling between map views**
- **Horizontal scrolling appears only when needed**

---

## 🚀 Future Goals

- Expanded entity types (Bosses, Keys, NPCs, Magical Items)
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
5. Update your `settings.json`:
    - Set your OpenAI or Azure OpenAI credentials, or
    - Configure Ollama/local LLM settings as needed.
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
