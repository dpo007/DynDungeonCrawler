using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Constants;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Load settings
        var settings = Settings.Load();

        // Check if OpenAI API key is set
        if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
        {
            Console.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }

        // Create LLM client
        ILLMClient llmClient = new OpenAIHelper(settings.OpenAIApiKey);

        // Create a new dungeon instance
        Dungeon dungeon = new Dungeon(DungeonDefaults.MaxDungeonWidth, DungeonDefaults.MaxDungeonHeight, llmClient);

        // Generate the dungeon layout
        dungeon.GenerateDungeon();

        // Populate rooms with treasure chests and enemies
        await dungeon.PopulateRoomContentsAsync();

        // Print maps
        Console.WriteLine("Dungeon Map (Paths Only):");
        dungeon.PrintDungeonMap(showEntities: false); // Basic view: Entrance/Exit/Path only

        Console.WriteLine("\nDungeon Map (With Entities):");
        dungeon.PrintDungeonMap(showEntities: true); // Detailed view: showing treasure and enemies

        // Export dungeon (rooms + entities) to JSON
        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DungeonExports");
        Directory.CreateDirectory(exportFolder);
        string exportPath = Path.Combine(exportFolder, "MyDungeon.json");
        dungeon.SaveToJson(exportPath);

        Console.WriteLine($"\nDungeon saved to {exportPath}");
        Console.ReadKey();
    }
}