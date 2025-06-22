using DynDungeonCrawler.Engine.Interfaces;
using Spectre.Console;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class ConsoleUserInterface : IUserInterface
    {
        public void Write(string message) => SafeMarkup(message, newline: false);

        public void WriteLine(string message) => SafeMarkup(message, newline: true);

        public void WriteLine() => AnsiConsole.MarkupLine(string.Empty);

        public string ReadLine() => Console.ReadLine() ?? string.Empty;

        public ConsoleKey ReadKey(bool intercept = false) => Console.ReadKey(intercept).Key;

        public void Clear() => AnsiConsole.Clear();

        private void SafeMarkup(string message, bool newline)
        {
            try
            {
                if (newline)
                    AnsiConsole.MarkupLine(message);
                else
                    AnsiConsole.Markup(message);
            }
            catch (InvalidOperationException)
            {
                // Fallback to escaped version for unknown style/color
                var escaped = EscapeMarkup(message);
                if (newline)
                    AnsiConsole.MarkupLine(escaped);
                else
                    AnsiConsole.Markup(escaped);
            }
        }

        private static string EscapeMarkup(string input) =>
            input.Replace("[", "[[").Replace("]", "]]");
    }
}