using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class ConsoleUserInterface : IUserInterface
    {
        public void Write(string message) => Console.Write(message);

        public void WriteLine(string message) => Console.WriteLine(message);

        public string ReadLine() => Console.ReadLine() ?? string.Empty;

        public ConsoleKey ReadKey(bool intercept = false) => Console.ReadKey(intercept).Key;

        public void Clear() => Console.Clear();
    }
}