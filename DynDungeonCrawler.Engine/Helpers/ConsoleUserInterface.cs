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

        /// <summary>
        /// Maps color names to ConsoleColor values.
        /// </summary>
        private static readonly Dictionary<string, ConsoleColor> ColorMap = new()
        {
            { "red", ConsoleColor.Red },
            { "green", ConsoleColor.Green },
            { "blue", ConsoleColor.Blue },
            { "yellow", ConsoleColor.Yellow },
            { "cyan", ConsoleColor.Cyan },
            { "cyan1", ConsoleColor.Cyan },
            { "magenta", ConsoleColor.Magenta },
            { "white", ConsoleColor.White },
            { "gray", ConsoleColor.Gray },
            { "grey", ConsoleColor.Gray },
            { "darkgray", ConsoleColor.DarkGray },
            { "darkgrey", ConsoleColor.DarkGray },
            { "black", ConsoleColor.Black },
            { "darkred", ConsoleColor.DarkRed },
            { "darkgreen", ConsoleColor.DarkGreen },
            { "darkblue", ConsoleColor.DarkBlue },
            { "darkyellow", ConsoleColor.DarkYellow },
            { "darkcyan", ConsoleColor.DarkCyan },
            { "darkmagenta", ConsoleColor.DarkMagenta },
            { "default", Console.ForegroundColor }
        };

        /// <summary>
        /// Attempts to map a color name to a ConsoleColor.
        /// </summary>
        private static ConsoleColor MapColor(string colorName)
        {
            colorName = colorName.ToLowerInvariant().Trim();
            return ColorMap.TryGetValue(colorName, out ConsoleColor color)
                ? color
                : Console.ForegroundColor;
        }

        /// <summary>
        /// Displays a pick list of items using standard Console colors and returns the selected item's index.
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
            Console.WriteLine(prompt);

            // Display each item with its number and optional color
            ConsoleColor originalColor = Console.ForegroundColor;

            for (int i = 0; i < items.Count; i++)
            {
                string display = displaySelector(items[i]);
                string colorName = colorSelector?.Invoke(items[i]) ?? "default";
                ConsoleColor itemColor = MapColor(colorName);

                // Display the item number in the original color
                Console.Write($" [{i + 1}] ");

                // Display the item text in the specified color
                Console.ForegroundColor = itemColor;
                Console.WriteLine(display);
                Console.ForegroundColor = originalColor;
            }

            // Show the cancel prompt
            Console.Write($"Enter number (or {cancelPrompt}): ");

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