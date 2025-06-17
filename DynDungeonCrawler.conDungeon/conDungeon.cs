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

            // Load settings and check API key
            var settings = Settings.Load();
            if (string.IsNullOrEmpty(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                Console.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Setup logger and LLM client
            ILogger logger = new ConsoleLogger();
            ILLMClient llmClient = new OpenAIHelper(new HttpClient(), settings.OpenAIApiKey);

            // Load dungeon from JSON
            string filePath = "DungeonExports/MyDungeon.json";
            Dungeon dungeon = Dungeon.LoadFromJson(filePath, llmClient, logger);
            Console.WriteLine("Dungeon loaded successfully.");

            // Display the dungeon theme
            Console.WriteLine($"Dungeon Theme: \"{dungeon.Theme}\"");

            // Ask user for their Name (optional)
            Console.Write("Enter your adventurer's name (or press Enter for a generated name): ");
            string playerName = Console.ReadLine()?.Trim() ?? string.Empty;

            // Ask user for their Gender (M/F, or press Enter for unspecified)
            AdventurerGender gender = AdventurerGender.Unspecified;
            Console.Write("Enter your adventurer's gender ([M]ale/[F]emale, or press Enter for unspecified): ");
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            Console.WriteLine();

            if (keyInfo.Key == ConsoleKey.M)
                gender = AdventurerGender.Male;
            else if (keyInfo.Key == ConsoleKey.F)
                gender = AdventurerGender.Female;
            // Any other key (including Enter) leaves gender as Unspecified

            if (string.IsNullOrEmpty(playerName))
            {
                // Generate a name using the LLM, passing the theme and gender
                playerName = Adventurer.GenerateNameAsync(llmClient, dungeon.Theme, gender).GetAwaiter().GetResult();
                Console.WriteLine($"Generated adventurer name: {playerName}");
            }

            // Find entrance and create adventurer
            Room entrance = dungeon.Rooms.First(r => r.Type == RoomType.Entrance);
            Adventurer player = new Adventurer(playerName, entrance);

            // Main game loop (simplified)
            while (true)
            {
                // Check for game-ending conditions
                if (player.Health <= 0)
                {
                    Console.WriteLine("You have perished in the dungeon. Game over!");
                    break;
                }
                if (player.CurrentRoom?.Type == RoomType.Exit)
                {
                    Console.WriteLine("Congratulations! You have found the exit and escaped the dungeon!");
                    break;
                }

                // Display room info, including exits
                Console.WriteLine($"You are in room ({player.CurrentRoom.X}, {player.CurrentRoom.Y}): {player.CurrentRoom.Description}");
                Console.WriteLine("Exits:");
                if (player.CurrentRoom.ConnectedNorth)
                    Console.WriteLine(" - North");
                if (player.CurrentRoom.ConnectedEast)
                    Console.WriteLine(" - East");
                if (player.CurrentRoom.ConnectedSouth)
                    Console.WriteLine(" - South");
                if (player.CurrentRoom.ConnectedWest)
                    Console.WriteLine(" - West");

                // Ask for player input
                Console.Write("Enter command (move [N/E/S/W], look, inventory, exit): ");
                string command = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
            }
        }
    }
}