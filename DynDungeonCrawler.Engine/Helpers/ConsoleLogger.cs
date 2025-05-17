using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("» ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}