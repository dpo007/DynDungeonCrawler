using DynDungeonCrawler.Engine.Interfaces;
using Spectre.Console;

namespace DynDungeonCrawler.Engine.Helpers.UI
{
    public class SpectreConsoleUserInterface : IUserInterface
    {
        // Output methods
        public void Write(string message) => SafeMarkup(message, newline: false);

        /// <summary>
        /// Writes a message to the output followed by a newline.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="center">Whether to center the message horizontally in the output view.</param>
        public void WriteLine(string message, bool center = false)
        {
            if (center)
            {
                int consoleWidth = Console.WindowWidth;
                int visibleLength = GetVisibleLength(message);
                int padLeft = Math.Max(0, (consoleWidth - visibleLength) / 2);
                string padded = new string(' ', padLeft) + message;
                SafeMarkup(padded, newline: true);
            }
            else
            {
                SafeMarkup(message, newline: true);
            }
        }

        public void WriteLine() => AnsiConsole.MarkupLine(string.Empty);

        public void WriteRule(string? text = null)
        {
            Rule rule = text is not null
                ? new Rule(text) { Style = Style.Parse("grey dim") }
                : new Rule() { Style = Style.Parse("grey dim") };

            AnsiConsole.Write(rule);
        }

        public void Clear() => AnsiConsole.Clear();

        // Input methods
        public Task<string> ReadLineAsync() => Task.FromResult(Console.ReadLine() ?? string.Empty);

        public Task<string> ReadKeyAsync(bool intercept = false, bool hideCursor = false)
        {
            bool originalCursorVisible = true;
            bool changedCursor = false;

            if (hideCursor && OperatingSystem.IsWindows())
            {
                try
                {
                    originalCursorVisible = Console.CursorVisible;
                    Console.CursorVisible = false;
                    changedCursor = true;
                }
                catch
                {
                    // Ignore if not supported in this environment
                }
            }

            try
            {
                return Task.FromResult(Console.ReadKey(intercept).KeyChar.ToString());
            }
            finally
            {
                if (hideCursor && changedCursor && OperatingSystem.IsWindows())
                {
                    try
                    {
                        Console.CursorVisible = originalCursorVisible;
                    }
                    catch
                    {
                        // Ignore if not supported in this environment
                    }
                }
            }
        }

        // Markup helpers
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

        // Visible length calculation
        private static int GetVisibleLength(string markup)
        {
            // Replace escaped brackets with placeholders
            string temp = markup.Replace("[[", "\uFFF0").Replace("]]", "\uFFF1");
            // Remove Spectre.Console markup tags: [tag]...[/], [tag], [/tag]
            string noMarkup = System.Text.RegularExpressions.Regex.Replace(temp, @"\[[^\]]*\]", "");
            // Remove Spectre.Console emoji shortcodes: :emoji_name:
            noMarkup = System.Text.RegularExpressions.Regex.Replace(noMarkup, @":([a-zA-Z0-9_]+):", "");
            // Restore placeholders to single brackets
            noMarkup = noMarkup.Replace("\uFFF0", "[").Replace("\uFFF1", "]");
            return noMarkup.Length;
        }

        // Special output
        public void WriteSpecialMessage(string message, int durationMs = 2000, bool center = false, bool writeLine = false)
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

        // Pick list
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

        // Spinner
        public async Task<T> ShowSpinnerAsync<T>(string message, Func<Task<T>> operation)
        {
            T result = default!;
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .StartAsync(message, async ctx =>
                {
                    result = await operation();
                });
            return result;
        }

        // Status update
        /// <summary>
        /// Updates the player's status at the top-left of the console, showing name, health, and money between two rules.
        /// Only moves and restores the cursor position if the current position is not (0,0).
        /// Uses Spectre.Console emojis for visual clarity.
        /// </summary>
        /// <param name="health">Player's current health.</param>
        /// <param name="money">Player's current money.</param>
        /// <param name="name">Player's name.</param>
        public void UpdateStatus(int health, int money, string name)
        {
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;
            bool shouldRestoreCursor = !(origLeft == 0 && origTop == 0);

            if (shouldRestoreCursor)
            {
                Console.SetCursorPosition(0, 0);
            }

            WriteRule();

            // Use Spectre.Console emojis for name, health, and money
            string status = $"[bold white]:bust_in_silhouette: {EscapeMarkup(name)}[/]   [bold green]:beating_heart: Health:[/] {health}   [bold yellow]:money_bag: Money:[/] {money}";
            int consoleWidth = Console.WindowWidth;
            int visibleLength = GetVisibleLength(status);
            int padLeft = Math.Max(0, (consoleWidth - visibleLength) / 2);
            string paddedStatus = new string(' ', padLeft) + status;
            SafeMarkup(paddedStatus, newline: true);

            WriteRule();

            if (shouldRestoreCursor)
            {
                Console.SetCursorPosition(origLeft, origTop);
            }
        }
    }
}