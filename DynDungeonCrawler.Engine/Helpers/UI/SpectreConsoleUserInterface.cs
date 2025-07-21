using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Interfaces;
using DynDungeonCrawler.Engine.Models;
using Spectre.Console;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Writes a game message with appropriate styling based on message type.
        /// Applies Spectre.Console markup based on the message type.
        /// </summary>
        /// <param name="message">The game message to write.</param>
        /// <param name="center">Whether to center the message horizontally in the output view.</param>
        public void WriteMessage(GameMessage message, bool center = false)
        {
            string formattedText = FormatMessageForSpectre(message);
            WriteLine(formattedText, center);
        }

        /// <summary>
        /// Writes multiple game messages in sequence.
        /// </summary>
        /// <param name="messages">The collection of game messages to write.</param>
        public void WriteMessages(IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages)
            {
                WriteMessage(message);
            }
        }

        /// <summary>
        /// Formats a GameMessage for Spectre.Console with appropriate markup.
        /// </summary>
        /// <param name="message">The game message to format.</param>
        /// <returns>A formatted string with Spectre.Console markup.</returns>
        private string FormatMessageForSpectre(GameMessage message)
        {
            return message.Type switch
            {
                UIMessageType.Error => $"[bold red]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.Success => $"[bold green]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.Emphasis => $"[bold]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.Header => $"[underline bold]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.CombatAction => $"[blue]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.CombatCritical => $"[bold yellow]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.PlayerStatus => $"[green]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.EnemyStatus => $"[red]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.ItemInfo => $"[cyan]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.Help => $"[grey italic]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.RoomDescription => $"[dim]{EscapeMarkup(message.Content)}[/]",
                UIMessageType.System => $"[grey]{EscapeMarkup(message.Content)}[/]",
                _ => EscapeMarkup(message.Content)
            };
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
            // Replace Spectre.Console emoji shortcodes with Unicode
            noMarkup = ReplaceSpectreEmojis(noMarkup);
            // Restore placeholders to single brackets
            noMarkup = noMarkup.Replace("\uFFF0", "[").Replace("\uFFF1", "]");
            // Calculate display width (count double-width chars)
            int width = 0;
            foreach (System.Text.Rune rune in noMarkup.EnumerateRunes())
            {
                // Most emojis and CJK chars are double-width
                width += RuneDisplayWidth(rune);
            }
            return width;
        }

        // Map Spectre.Console emoji shortcodes to Unicode
        private static string ReplaceSpectreEmojis(string input)
        {
            // Add more mappings as needed
            Dictionary<string, string> emojiMap = new Dictionary<string, string>
            {
                { ":bust_in_silhouette:", "\U0001F464" },
                { ":flexed_biceps:", "\U0001F4AA" },
                { ":shield:", "\U0001F6E1" },
                { ":beating_heart:", "\U0001F493" },
                { ":money_bag:", "\U0001F4B0" }
            };
            return System.Text.RegularExpressions.Regex.Replace(input, @":([a-zA-Z0-9_]+):", m =>
            {
                if (emojiMap.TryGetValue(m.Value, out string emoji))
                {
                    return emoji;
                }

                return string.Empty; // Remove unknown emoji shortcodes
            });
        }

        // Returns display width for a Unicode rune (emoji, CJK, etc.)
        private static int RuneDisplayWidth(System.Text.Rune rune)
        {
            // Most emojis and CJK chars are double-width
            // Use Unicode ranges for emoji and CJK
            int codepoint = rune.Value;
            if ((codepoint >= 0x1F300 && codepoint <= 0x1FAFF) || // Emoji
                (codepoint >= 0x1100 && codepoint <= 0x115F) ||   // Hangul Jamo
                (codepoint >= 0x2E80 && codepoint <= 0xA4CF) ||   // CJK
                (codepoint >= 0xAC00 && codepoint <= 0xD7A3))     // Hangul Syllables
            {
                return 2;
            }
            // Most other chars are single-width
            return 1;
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
                bool inMarkup = false;
                int colorIndex = o;
                for (int i = 0; i < t.Length; i++)
                {
                    char c = t[i];
                    if (c == '[')
                    {
                        inMarkup = true;
                        result += c;
                    }
                    else if (c == ']')
                    {
                        inMarkup = false;
                        result += c;
                    }
                    else if (inMarkup)
                    {
                        result += c;
                    }
                    else
                    {
                        string color = rainbowColors[colorIndex++ % rainbowColors.Length];
                        result += $"[{color}]{c}[/]";
                    }
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
        /// Updates the player's status at the top-left of the console, showing name, strength, defense, HP, and coins between two rules.
        /// Only moves and restores the cursor position if the current position is not (0,0).
        /// Uses Spectre.Console emojis for visual clarity.
        /// </summary>
        /// <param name="player">The Adventurer whose status to display.</param>
        public void UpdateStatus(Adventurer player)
        {
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;
            bool shouldRestoreCursor = !(origLeft == 0 && origTop == 0);

            if (shouldRestoreCursor)
            {
                Console.SetCursorPosition(0, 0);
            }

            WriteRule();

            // Use Spectre.Console emojis for name, strength, defense, HP, coins
            string status = $"[bold white]:bust_in_silhouette: {EscapeMarkup(player.Name)}[/]   [bold yellow]:flexed_biceps: Strength:[/] {player.Strength}   [bold blue]:shield:  Defense:[/] {player.Defense}   [bold green]:beating_heart: HP:[/] {player.Health}   [bold gold1]:money_bag: Coins:[/] {player.Wealth}";
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

        /// <summary>
        /// Displays text one sentence at a time with pauses between sentences for dramatic effect.
        /// Each sentence is displayed on its own line to ensure proper wrapping and alignment.
        /// </summary>
        /// <param name="text">The text to display sentence by sentence.</param>
        /// <param name="pauseMs">Optional fixed pause duration in milliseconds between sentences.
        /// If not specified, random pauses between 2000-4000ms will be used.</param>
        /// <param name="endNewLine">Whether to add a newline after all sentences are displayed.</param>
        /// <returns>A task that completes when all sentences have been displayed.</returns>
        public async Task WriteSlowlyBySentenceAsync(string text, int? pauseMs = null, bool endNewLine = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // For Spectre.Console, we need to preserve any markup tags in the original text
            // Extract the markup tags and text content separately
            string extractedText = text;

            // Extract global markup tags that wrap the entire text
            // Find opening and closing markup tags that wrap the entire text
            Match globalMarkupMatch = Regex.Match(text, @"^\[([^\]]+)\](.*)\[/\]$", RegexOptions.Singleline);
            string globalMarkupPrefix = string.Empty;
            string globalMarkupSuffix = string.Empty;

            if (globalMarkupMatch.Success)
            {
                // Found global markup like "[italic yellow]...[/]"
                globalMarkupPrefix = $"[{globalMarkupMatch.Groups[1].Value}]";
                globalMarkupSuffix = "[/]";
                extractedText = globalMarkupMatch.Groups[2].Value;
            }

            // Split text into sentences using regex to handle various end-of-sentence punctuation
            string pattern = @"(\.|\!|\?|…)(\s+|$)";
            List<string> sentences = new List<string>();

            int startIndex = 0;
            foreach (Match match in Regex.Matches(extractedText, pattern))
            {
                if (match.Index >= startIndex)
                {
                    // Get the sentence including its punctuation and add it to the list
                    string sentence = extractedText.Substring(startIndex, match.Index + match.Length - startIndex);
                    sentences.Add(sentence);
                    startIndex = match.Index + match.Length;
                }
            }

            // If there's any text left (e.g., no final punctuation), add it as the last sentence
            if (startIndex < extractedText.Length)
            {
                sentences.Add(extractedText.Substring(startIndex));
            }

            // If no sentences were found (no punctuation), treat the entire text as one sentence
            if (sentences.Count == 0)
            {
                sentences.Add(extractedText);
            }

            Random random = Random.Shared;

            // Display each sentence with a pause
            for (int i = 0; i < sentences.Count; i++)
            {
                string sentence = sentences[i].TrimStart();
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    // Apply the global markup tags to each sentence
                    string formattedSentence = $"{globalMarkupPrefix}{sentence}{globalMarkupSuffix}";

                    // Only apply markup if we found global markup tags
                    if (string.IsNullOrEmpty(globalMarkupPrefix))
                    {
                        formattedSentence = sentence;
                    }

                    // Write each sentence on its own line for proper wrapping
                    SafeMarkup(formattedSentence, newline: true);

                    // If this isn't the last sentence, add a pause
                    if (i < sentences.Count - 1)
                    {
                        int actualPauseMs = pauseMs ?? random.Next(2000, 4001);
                        await Task.Delay(actualPauseMs);
                    }
                }
            }

            // Add final newline if requested
            if (endNewLine)
            {
                WriteLine();
            }
        }
    }
}