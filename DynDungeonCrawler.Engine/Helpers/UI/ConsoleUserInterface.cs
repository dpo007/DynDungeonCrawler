using DynDungeonCrawler.Engine.Interfaces;
using System.Text.RegularExpressions;

namespace DynDungeonCrawler.Engine.Helpers.UI
{
    /// <summary>
    /// Provides a basic, plain-text, mono-colored console user interface implementation of <see cref="IUserInterface"/>.
    /// Intended for standard console environments with no advanced formatting or color features.
    /// </summary>
    public class ConsoleUserInterface : IUserInterface
    {
        public void Write(string message) => Console.Write(message);

        /// <summary>
        /// Writes a message to the output followed by a newline.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="center">Whether to center the message horizontally in the output view.</param>
        public void WriteLine(string message, bool center = false)
        {
            if (center)
            {
                int consoleWidth;
                try
                {
                    consoleWidth = Console.WindowWidth;
                }
                catch
                {
                    consoleWidth = 80;
                }
                int padLeft = Math.Max(0, (consoleWidth - message.Length) / 2);
                string padded = new string(' ', padLeft) + message;
                Console.WriteLine(padded);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        // Legacy overload for compatibility
        public void WriteLine(string message) => WriteLine(message, false);

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

        public void Clear() => Console.Clear();

        public void WriteSpecialMessage(string message, int durationMs = 2000, bool center = false, bool writeLine = false)
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

        /// <summary>
        /// Updates the player's status at the top-left of the console, showing name, health, and money between two rules.
        /// Only moves and restores the cursor position if the current position is not (0,0).
        /// </summary>
        /// <param name="health">Player's current health.</param>
        /// <param name="money">Player's current money.</param>
        /// <param name="name">Player's name.</param>
        public void UpdateStatus(string name, int strength, int defense, int health)
        {
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;
            bool shouldRestoreCursor = !(origLeft == 0 && origTop == 0);

            if (shouldRestoreCursor)
            {
                Console.SetCursorPosition(0, 0);
            }

            WriteRule();

            // Status line: name, strength, defense, health spaced evenly
            string status = $"{name}   Strength: {strength}   Defense: {defense}   Health: {health}";
            WriteLine(status, center: true);

            WriteRule();

            if (shouldRestoreCursor)
            {
                Console.SetCursorPosition(origLeft, origTop);
            }
        }

        /// <summary>
        /// Displays text one sentence at a time with pauses between sentences for dramatic effect.
        /// Each sentence is displayed on its own line to ensure proper wrapping and alignment.
        /// </summary>
        /// <param name="text">The text to display sentence by sentence.</param>
        /// <param name="pauseMs">Optional fixed pause duration in milliseconds between sentences.</param>
        /// <param name="endNewLine">Whether to add a newline after all sentences are displayed.</param>
        /// <returns>A task that completes when all sentences have been displayed.</returns>
        public async Task WriteSlowlyBySentenceAsync(string text, int? pauseMs = null, bool endNewLine = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Clean any markup codes that might be present (since ConsoleUI doesn't support them)
            // Remove Spectre.Console markup tags and emoji shortcodes
            string cleanText = Regex.Replace(text, @"\[[^\]]*\]", ""); // Remove style tags
            cleanText = Regex.Replace(cleanText, @":[a-zA-Z0-9_]+:", ""); // Remove emoji shortcodes

            // Split text into sentences using regex to handle various end-of-sentence punctuation
            string pattern = @"(\.|\!|\?|…)(\s+|$)";
            List<string> sentences = new List<string>();

            int startIndex = 0;
            foreach (Match match in Regex.Matches(cleanText, pattern))
            {
                if (match.Index >= startIndex)
                {
                    // Get the sentence including its punctuation and add it to the list
                    string sentence = cleanText.Substring(startIndex, match.Index + match.Length - startIndex);
                    sentences.Add(sentence);
                    startIndex = match.Index + match.Length;
                }
            }

            // If there's any text left (e.g., no final punctuation), add it as the last sentence
            if (startIndex < cleanText.Length)
            {
                sentences.Add(cleanText.Substring(startIndex));
            }

            // If no sentences were found (no punctuation), treat the entire text as one sentence
            if (sentences.Count == 0)
            {
                sentences.Add(cleanText);
            }

            Random random = Random.Shared;

            // Display each sentence and pause between them
            for (int i = 0; i < sentences.Count; i++)
            {
                string sentence = sentences[i].TrimStart();
                if (string.IsNullOrWhiteSpace(sentence))
                {
                    continue;
                }

                // Write the entire sentence on its own line for proper wrapping
                Console.WriteLine(sentence);

                // If this isn't the last sentence, add a pause
                if (i < sentences.Count - 1)
                {
                    int actualPauseMs = pauseMs ?? random.Next(2000, 4001);
                    await Task.Delay(actualPauseMs);
                }
            }

            // Add final newline if requested
            if (endNewLine)
            {
                Console.WriteLine();
            }
        }
    }
}