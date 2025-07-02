using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class ConsoleUserInterface : IUserInterface
    {
        public void Write(string message) => Console.Write(message);

        public void WriteLine(string message) => Console.WriteLine(message);

        public void WriteLine() => Console.WriteLine();

        public void WriteRule(string? text = null)
        {
            int width;
            try
            {
                width = Console.WindowWidth;
            }
            catch
            {
                width = 80; // Fallback if not available
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Remove any newlines from text
                text = text.Replace(Environment.NewLine, " ");

                // Calculate padding
                int textLength = text.Length + 2; // spaces around text
                int dashes = Math.Max(0, width - textLength);
                int left = dashes / 2;
                int right = dashes - left;
                Console.WriteLine(new string('-', left) + " " + text + " " + new string('-', right));
            }
            else
            {
                Console.WriteLine(new string('-', width));
            }
        }

        public string ReadLine() => Console.ReadLine() ?? string.Empty;

        public ConsoleKey ReadKey(bool intercept = false) => Console.ReadKey(intercept).Key;

        public void Clear() => Console.Clear();
    }
}