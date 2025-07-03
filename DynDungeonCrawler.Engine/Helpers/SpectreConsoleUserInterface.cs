using DynDungeonCrawler.Engine.Interfaces;
using Spectre.Console;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class SpectreConsoleUserInterface : IUserInterface
    {
        public void Write(string message) => SafeMarkup(message, newline: false);

        public void WriteLine(string message) => SafeMarkup(message, newline: true);

        public void WriteLine() => AnsiConsole.MarkupLine(string.Empty);

        public Task<string> ReadLineAsync() => Task.FromResult(Console.ReadLine() ?? string.Empty);

        public Task<string> ReadKeyAsync(bool intercept = false) =>
            Task.FromResult(Console.ReadKey(intercept).KeyChar.ToString());

        public void Clear() => AnsiConsole.Clear();

        private void SafeMarkup(string message, bool newline)
        {
            try
            {
                if (newline)
                {
                    AnsiConsole.MarkupLine(message);
                }
                else
                {
                    AnsiConsole.Markup(message);
                }
            }
            catch (InvalidOperationException)
            {
                // Fallback to escaped version for unknown style/color
                string escaped = EscapeMarkup(message);
                if (newline)
                {
                    AnsiConsole.MarkupLine(escaped);
                }
                else
                {
                    AnsiConsole.Markup(escaped);
                }
            }
        }

        private static string EscapeMarkup(string input) =>
            input.Replace("[", "[[").Replace("]", "]]");

        public void WriteRule(string? text = null)
        {
            Rule rule = text is not null
                ? new Rule(text) { Style = Style.Parse("grey dim") }
                : new Rule() { Style = Style.Parse("grey dim") };

            AnsiConsole.Write(rule);
        }

        public void ShowSpecialMessage(string message, int durationMs = 2000, bool center = false, bool writeLine = false)
        {
            string[] rainbowColors = { "red", "orange1", "yellow1", "green", "deepskyblue1", "blue", "magenta" };
            int delay = 100;
            int steps = durationMs / delay;
            int offset = 0;

            string BuildRainbowMarkup(string t, int o, int padLeft)
            {
                string result = new string(' ', padLeft);
                for (int i = 0; i < t.Length; i++)
                {
                    string color = rainbowColors[(o + i) % rainbowColors.Length];
                    result += $"[{color}]{t[i]}[/]";
                }
                return result;
            }

            // Save the current cursor line and console width
            int top = Console.CursorTop;
            int consoleWidth = Console.WindowWidth;
            int padLeft = center ? Math.Max(0, (consoleWidth - message.Length) / 2) : 0;

            for (int i = 0; i < steps; i++)
            {
                offset = (offset + 1) % rainbowColors.Length;

                Console.SetCursorPosition(0, top);
                Console.Write(new string(' ', consoleWidth)); // Clear line

                Console.SetCursorPosition(0, top);
                string rainbow = BuildRainbowMarkup(message, offset, padLeft);
                AnsiConsole.Markup(rainbow);

                Thread.Sleep(delay);
            }

            // Move cursor below or just to end of line
            if (writeLine)
            {
                Console.SetCursorPosition(0, top + 1);
            }
            else
            {
                Console.SetCursorPosition(message.Length + padLeft, top); // after message
            }
        }
    }
}