using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Configuration;
using DynDungeonCrawler.Engine.Helpers;
using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.ConDungeon
{
    internal class ConDungeon
    {
        private static void Main(string[] args)
        {
            // Initialize the user interface using Spectre.Console for enhanced console output and input.
            IUserInterface ui = new SpectreConsoleUserInterface();

            ui.WriteLine("*** [bold]Dynamic Dungeon Crawler![/] ***");

            // Load settings and check API key
            var settings = Settings.Load();
            if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey) || settings.OpenAIApiKey == "your-api-key-here")
            {
                ui.WriteLine("OpenAI API key is not set. Please update 'settings.json' with your actual API key.");
                ui.WriteLine("Press any key to exit.");
                ui.ReadKey();
                return;
            }
        }
    }
}