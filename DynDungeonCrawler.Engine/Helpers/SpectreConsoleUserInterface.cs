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
        /// Uses Spectre.Console's SelectionPrompt for a native, interactive pick list with color support and a cancel option.
        /// </summary>
        public Task<int> ShowPickListAsync<T>(
            string prompt,
            IReadOnlyList<T> items,
            Func<T, string> displaySelector,
            Func<T, string>? colorSelector = null,
            string cancelPrompt = "Cancel")
        {
            // Return -1 immediately if there are no items to select from
            if (items.Count == 0)
            {
                return Task.FromResult(-1);
            }

            // Build a list of display strings for each item, with optional color markup
            List<string> displayList = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                string display = displaySelector(items[i]);
                string color = colorSelector?.Invoke(items[i]) ?? "white";
                // Validate the color; fallback to white if invalid
                try { Style.Parse(color); } catch { color = "white"; }
                // Format the display string with color markup and item index
                string colored = $"[{color}]{EscapeMarkup(display)}[/]";
                displayList.Add($"{colored}");
            }

            // Add a cancel option at the end of the list
            string cancelOption = $"[grey]{cancelPrompt}[/]";
            displayList.Add(cancelOption);

            // Create and configure the Spectre.Console SelectionPrompt
            SelectionPrompt<string> selectionPrompt = new SelectionPrompt<string>()
                .Title($"[bold]{EscapeMarkup(prompt)}[/]") // Set the prompt title
                .AddChoices(displayList)                   // Add all choices (including cancel)
                .HighlightStyle("bold invert");                 // Highlight style for the selected item

            // Show the prompt and get the user's selection
            string selected = AnsiConsole.Prompt(selectionPrompt);

            // If the user selected the cancel option, return -1
            if (selected == cancelOption)
            {
                Clear();
                return Task.FromResult(-1);
            }

            // Otherwise, find the index of the selected item and return it
            for (int i = 0; i < items.Count; i++)
            {
                if (selected == displayList[i])
                {
                    return Task.FromResult(i);
                }
            }

            // Fallback: return -1 if no valid selection was made
            return Task.FromResult(-1);
        }
    }
}