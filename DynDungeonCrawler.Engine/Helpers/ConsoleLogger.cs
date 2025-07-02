using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class ConsoleLogger : ILogger
    {
        private static readonly object _consoleLock = new();

        public void Log(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("» ");
                Console.ResetColor();
                Console.WriteLine(message);
            }
        }
    }
}