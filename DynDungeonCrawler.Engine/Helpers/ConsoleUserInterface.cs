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

        // Async implementations (preparing for HTML UI comatability)
        public Task<string> ReadLineAsync() => Task.FromResult(Console.ReadLine() ?? string.Empty);

        public Task<string> ReadKeyAsync(bool intercept = false) =>
            Task.FromResult(Console.ReadKey(intercept).KeyChar.ToString());

        public void Clear() => Console.Clear();

        public void ShowSpecialMessage(string message, int durationMs = 2000, bool center = false, bool writeLine = false)
        {
            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            int consoleWidth = Console.WindowWidth;
            int padLeft = center ? Math.Max(0, (consoleWidth - message.Length) / 2) : 0;
            string output = new string(' ', padLeft) + message.ToUpper();

            if (writeLine)
            {
                Console.WriteLine(output);
            }
            else
            {
                Console.Write(output);
            }

            Console.ForegroundColor = prevColor;
        }
    }
}