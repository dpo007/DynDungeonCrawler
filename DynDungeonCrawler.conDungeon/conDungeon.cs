using System;
using System.Linq;
using System.Net.Http;
using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;

namespace conDungeon
{
    internal class conDungeon
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Dungeon Crawler!");

            // 1. Load settings and check API key
            var settings = Settings.Load();
            if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                Console.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // 2. Setup logger and LLM client
            ILogger logger = new ConsoleLogger();
            ILLMClient llmClient = new OpenAIHelper(new HttpClient(), settings.OpenAIApiKey);

            // 3. Load dungeon from JSON
            string filePath = "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);

            // 4. Find entrance and create adventurer
            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(entrance);

            // 5. Main game loop (simplified)
            while (true)
            {
                // Display room info, prompt for action, process input...
            }
        }
    }
}