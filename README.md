# DynDungeonCrawler

![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)

**DynDungeonCrawler** is a modular C#/.NET 9 solution for procedural dungeon generation, interactive console exploration, and graphical map visualization. It creates complex, solvable dungeon layouts, populates them with AI-generated enemies and treasures, and exports structured JSON for integration into games, visualization tools, or other projects. The engine is highly configurable, supports multiple LLM providers, and features robust logging and configuration management.

---

## 🌟 Core Features

- **Procedural Dungeon Generation:**
  - Solvable main path, randomized branches, configurable size and path length
- **Room & Entity System:**
  - Distinct room types, unique GUIDs, 4-way connectivity
  - Extensible entities: Enemies, Treasure Chests, Magical Lock Picks, etc.
  - Entity factories and randomized placement
- **Combat System:**
  - UI-agnostic, turn-based combat via `ICombatPresenter`
  - Multiple UI implementations (Spectre.Console, plain text)
  - Actions: Attack, Defend, Flee; detailed outcomes
- **Console & Graphical UI:**
  - Rich console experience with [Spectre.Console](https://github.com/spectreconsole/spectre.console)
  - WPF map viewer for dungeon JSON files
- **AI (LLM) Integration:**
  - OpenAI, Azure OpenAI, Ollama support via `ILLMClient`
  - Generates names, descriptions, and content for rooms, entities, and adventurers
  - Efficient batching, parallelization, and robust error handling
- **Serialization & Export:**
  - Full dungeon exported to JSON via DTOs
- **Settings & Configuration:**
  - Centralized and app-specific settings, auto-generated and validated
- **Robust Logging:**
  - Pluggable via `ILogger`, logs key events and errors

---

## 🏗️ Solution Structure

| Project                                 | Purpose                                                                 |
|:-----------------------------------------|:------------------------------------------------------------------------|
| **DynDungeonCrawler.Engine**             | Core engine: dungeon generation, room/entity structures, LLM integration, serialization, logging |
| **DynDungeonCrawler.GeneratorApp**       | Console runner: generates, populates, prints, and exports dungeons to JSON |
| **DynDungeonCrawler.ConDungeon**         | Interactive console dungeon crawler: play through a generated dungeon    |
| **DynDungeonCrawler.MapViewer**          | WPF graphical map viewer: load and visualize dungeon JSON files          |

---

## 🔑 Key Components

- **Procedural Generation:**
  - Generates complex, solvable dungeons with replayable layouts
- **Room & Entity System:**
  - Unique rooms, extensible entities, LLM-powered descriptions
- **Combat System:**
  - Turn-based, UI-agnostic, supports multiple presenters
- **UI Implementation:**
  - Console UI (Spectre.Console), graphical WPF map viewer
- **LLM Integration:**
  - Abstracted via `ILLMClient`, supports OpenAI, Azure, Ollama
- **Settings & Logging:**
  - Centralized config, robust logging via `ILogger`

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
  - Run the generator app, enter a dungeon theme, and export a dungeon to JSON. View map modes: structure-only and with entities.
- **Interactive Play:**
  - Run the ConDungeon app, load a generated dungeon, create or generate an adventurer, and explore room by room. Room descriptions and names are generated as you explore, and you can interact with treasures and enemies. Engage in turn-based combat with enemies, select your target, and choose your actions each round.
- **WPF Map Viewer:**
  - Use the MapViewer app to load a dungeon JSON file and view the map in two modes (paths only, with entities). Maps are color-coded and support synchronized scrolling.

---

## 🚀 Future Goals

- Expanded entity types (Bosses, Keys, NPCs, Magical Items, Lock Picks, etc.)
- Advanced procedural description generation
- Dungeon biomes and theming
- Graphical front-end rendering (Unity, WebGL, etc.)
- Enhanced save/load systems
- Minimap and smarter pathfinding
- Interactive events (traps, puzzles, lore drops)
- Additional combat presenter implementations

---

> **Project Status:** Foundational systems complete — now featuring interactive exploration, graphical map viewing, LLM-powered content generation, and a robust combat system with UI separation. Expanding into deeper gameplay mechanics, AI-driven storytelling, and worldbuilding next! 🚀

---

✨ _Turning grids into grand adventures — one room at a time!_ ✨
