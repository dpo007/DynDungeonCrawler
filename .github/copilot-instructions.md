## ✅ Coding Standards

**General:**

- Async where appropriate  
- Clean, testable, modular code  
- Use vertical spacing to improve readability (e.g., between methods, logical blocks)  

**Style:**

- `PascalCase` for types/members, `camelCase` for locals/params  
- Prefer **explicit types** over `var`  
  - Use `var` only when it improves clarity (e.g., LINQ, anonymous types)  
- Use expression-bodied members and pattern matching where appropriate  

**Architecture:**

- Separate UI from logic using `IUserInterface`  
  - Use for both input **and** output where applicable  
- All entities derive from the abstract `Entity` class  
- Instantiation should be handled via factories (`EntityFactory`, `EnemyFactory`, etc.)  
- Avoid mixing rendering, AI, or persistence inside core engine logic  

---

## 🧠 Copilot Instructions

**✅ Do:**

- Suggest clean, modular, extensible code  
- Enforce separation between engine logic, UI, and LLM layers  
- Use abstractions like `ILLMClient`, `ILogger`, and `IUserInterface`  
  - `IUserInterface` should handle all input/output where appropriate  
- Assume multiple `Entity` types beyond `Enemy` and `TreasureChest`  
- Use structured DTOs (`DungeonData`, `RoomData`, `EntityData`) for export  
- Maintain spacing and layout for readability  
- Prefer explicit types unless `var` significantly improves clarity  

**❌ Avoid:**

- Hardcoded strings for API keys, paths, or console output  
- Direct use of `Console.ReadLine` or `Console.WriteLine` — use `IUserInterface`  
- Mixing UI, AI, or serialization inside engine/data models  
- Writing raw/unstructured JSON — always use DTOs  