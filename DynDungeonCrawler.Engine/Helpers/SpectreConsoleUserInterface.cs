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
                Console.SetCursorPosition(0, top);
                AnsiConsole.Markup(BuildRainbowMarkup(message, offset++, padLeft));
                Thread.Sleep(delay);
            }

            if (writeLine)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays a pick list of items using Spectre.Console's styling and returns the selected item's index.
        /// Uses single-keypress selection for immediate response.
        /// </summary>
        public async Task<int> ShowPickListAsync<T>(
            string prompt,
            IReadOnlyList<T> items,
            Func<T, string> displaySelector,
            Func<T, string>? colorSelector = null,
            string cancelPrompt = "press Enter to cancel")
        {
            if (items.Count == 0)
            {
                return -1; // No items to select from
            }

            // Display the prompt
            AnsiConsole.MarkupLine($"[bold]{prompt}[/]");

            // Display each item with its number and optional color
            for (int i = 0; i < items.Count; i++)
            {
                string display = displaySelector(items[i]);
                string color = colorSelector?.Invoke(items[i]) ?? "white";

                // Make sure the color is valid for Spectre.Console
                try
                {
                    Style.Parse(color);
                }
                catch
                {
                    color = "white"; // Fallback to white if color is invalid
                }

                AnsiConsole.MarkupLine($" [[{i + 1}]] [{color}]{EscapeMarkup(display)}[/]");
            }

            // Show the cancel prompt
            AnsiConsole.Markup($"Enter number (or {cancelPrompt}): ");

            // Wait for a valid key press
            while (true)
            {
                string key = await ReadKeyAsync(intercept: true);

                // Check for cancel (Enter key)
                if (string.IsNullOrEmpty(key) || key == "\r" || key == "\n")
                {
                    Clear();
                    return -1;
                }

                // Check for a valid digit
                if (key.Length == 1 && char.IsDigit(key[0]))
                {
                    int num = key[0] - '0';
                    if (num >= 1 && num <= items.Count)
                    {
                        return num - 1; // Return zero-based index
                    }
                }

                // Invalid key, continue waiting
            }
        }
    }
}