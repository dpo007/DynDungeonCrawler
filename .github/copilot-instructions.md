## ✅ Coding Standards

### General
- Prefer async where appropriate  
- Write clean, testable, modular code  
- Use vertical spacing for readability (between methods and logical sections)  

### Style
- `PascalCase` for types and members; `camelCase` for locals and parameters  
- Prefer **explicit types** over `var`  
  - Use `var` only when it improves clarity (e.g., LINQ queries, anonymous types)  
- Use expression-bodied members and pattern matching where it enhances readability  

### Architecture
- Separate UI from logic using `IUserInterface`  
  - Responsible for both input **and** output  
- All entities must derive from the abstract base class `Entity`  
- Instantiate game objects via factories (`EntityFactory`, `EnemyFactory`, etc.)  
- Avoid mixing rendering, AI, or persistence into core engine logic  

---

## 🧠 Copilot Instructions

### ✅ Do
- Suggest clean, modular, extensible code  
- Enforce separation between engine, UI, and LLM logic layers  
- Use abstractions like `ILLMClient`, `ILogger`, and `IUserInterface`  
  - `IUserInterface` should handle **all input/output** operations  
- Assume a variety of `Entity` types beyond just `Enemy` and `TreasureChest`  
- Use structured DTOs (e.g., `DungeonData`, `RoomData`, `EntityData`) for all data export or serialization  
- Maintain vertical spacing and consistent layout for readability  
- Prefer explicit types unless `var` significantly improves clarity or brevity  
- **Include full XML summary header comments** for all methods and public members  
  - Include `<summary>`, `<param>`, and `<returns>` tags where applicable  
  - Use **one line per `<param>` tag**, no inline or multi-line formats  
  - Write clear, meaningful summaries (avoid placeholder text like "todo")  
- Add **inline comments** for non-trivial logic, edge cases, or decisions that may not be immediately obvious  
  - Aim comments at a competent hobbyist programmer — assume general C# familiarity  
  - Keep comments concise and high-value — avoid stating the obvious  
  - Use comments to clarify **why** something is done, not just **what** it does  

### ❌ Avoid
- Hardcoded strings (API keys, file paths, or UI text)  
- Direct use of `Console.ReadLine()` or `Console.WriteLine()` — always use `IUserInterface`  
- Mixing UI, AI, or serialization logic into core engine or data models  
- Writing raw/unstructured JSON — always define and use DTO classes  
