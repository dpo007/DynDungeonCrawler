using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Provides a basic, plain-text, mono-colored console user interface implementation of <see cref="IUserInterface"/>.
    /// Intended for standard console environments with no advanced formatting or color features.
    /// </summary>
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
            // Prepare the message in uppercase and with some padding
            string decorated = $"*** {message.ToUpper()} ***";
            int consoleWidth;
            try
            {
                consoleWidth = Console.WindowWidth;
            }
            catch
            {
                consoleWidth = 80;
            }

            // Center the message if requested
            string output;
            if (center)
            {
                int padLeft = Math.Max(0, (consoleWidth - decorated.Length) / 2);
                output = new string(' ', padLeft) + decorated;
            }
            else
            {
                output = decorated;
            }

            // Draw a box around the message
            string border = new string('*', Math.Min(consoleWidth, decorated.Length + 4));
            if (center)
            {
                int padLeft = Math.Max(0, (consoleWidth - border.Length) / 2);
                border = new string(' ', padLeft) + border;
            }

            // Print the box
            Console.WriteLine();
            Console.WriteLine(border);
            Console.WriteLine(output);
            Console.WriteLine(border);

            // Optionally pause for effect
            if (durationMs > 0)
            {
                Thread.Sleep(durationMs);
            }

            if (writeLine)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays a pick list of items and returns the selected item's index.
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

            // Display each item with its number
            for (int i = 0; i < items.Count; i++)
            {
                string display = displaySelector(items[i]);
                Console.WriteLine($" [{i + 1}] {display}");
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

        /// <summary>
        /// Displays a spinner animation while performing an asynchronous operation.
        /// </summary>
        public async Task<T> ShowSpinnerAsync<T>(string message, Func<Task<T>> operation)
        {
            char[] spinnerChars = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;
            bool running = true;

            Thread spinnerThread = new Thread(() =>
            {
                while (running)
                {
                    Console.Write($"\r{message} {spinnerChars[spinnerIndex++ % spinnerChars.Length]}");
                    Thread.Sleep(100);
                }
            })
            {
                IsBackground = true
            };

            spinnerThread.Start();
            try
            {
                T result = await operation();
                return result;
            }
            finally
            {
                running = false;
                spinnerThread.Join();
                Console.Write("\r" + new string(' ', message.Length + 2) + "\r"); // Clear spinner line
            }
        }
    }
}