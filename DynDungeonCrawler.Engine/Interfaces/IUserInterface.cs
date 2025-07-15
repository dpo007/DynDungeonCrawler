namespace DynDungeonCrawler.Engine.Interfaces
{
    /// <summary>
    /// Abstraction for user input and output operations, allowing different UI implementations (console, GUI, web, etc.).
    /// </summary>
    public interface IUserInterface
    {
        /// <summary>
        /// Writes a message to the output without a newline.
        /// </summary>
        /// <param name="message">The message to write.</param>
        void Write(string message);

        /// <summary>
        /// Writes a message to the output followed by a newline.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="center">Whether to center the message horizontally in the output view.</param>
        void WriteLine(string message, bool center = false);

        /// <summary>
        /// Writes a newline to the output.
        /// </summary>
        void WriteLine();

        /// <summary>
        /// Writes a horizontal rule to the output, optionally with a label.
        /// </summary>
        /// <param name="text">Optional text to display in the rule.</param>
        void WriteRule(string? text = null);

        /// <summary>
        /// Reads a line of input from the user asynchronously.
        /// </summary>
        /// <returns>A task that resolves to the input string, or an empty string if input is null.</returns>
        Task<string> ReadLineAsync();

        /// <summary>
        /// Reads a key press from the user asynchronously.
        /// Optionally hides the cursor while waiting for input (Windows only).
        /// </summary>
        /// <param name="intercept">If true, the key is not displayed in the output.</param>
        /// <param name="hideCursor">If true, hides the cursor while waiting for input (default: false, Windows only).</param>
        /// <returns>A task that resolves to a string representing the key that was pressed.</returns>
        Task<string> ReadKeyAsync(bool intercept = false, bool hideCursor = false);

        /// <summary>
        /// Clears the output display.
        /// </summary>
        void Clear();

        /// <summary>
        /// Displays a special, celebratory, or highlighted message with enhanced formatting or animation.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="durationMs">Optional duration for the effect, in milliseconds.</param>
        /// <param name="center">Whether to center the message in the display area.</param>
        /// <param name="writeLine">Whether to move the cursor to the next line after displaying.</param>
        void WriteSpecialMessage(string message, int durationMs = 2000, bool center = false, bool writeLine = false);

        /// <summary>
        /// Displays a pick list of items and returns the selected item's index or -1 if cancelled.
        /// </summary>
        /// <typeparam name="T">The type of items in the list.</typeparam>
        /// <param name="prompt">The prompt to display above the list.</param>
        /// <param name="items">The list of items to choose from.</param>
        /// <param name="displaySelector">Function that converts each item to its display string.</param>
        /// <param name="colorSelector">Optional function that returns a color name for each item.</param>
        /// <param name="cancelPrompt">The prompt for the cancel option (default: "press Enter to cancel").</param>
        /// <returns>The index of the selected item, or -1 if cancelled.</returns>
        Task<int> ShowPickListAsync<T>(
            string prompt,
            IReadOnlyList<T> items,
            Func<T, string> displaySelector,
            Func<T, string>? colorSelector = null,
            string cancelPrompt = "press Enter to cancel");

        /// <summary>
        /// Shows a spinner with an optional message while running the given async operation.
        /// </summary>
        /// <typeparam name="T">The result type of the operation.</typeparam>
        /// <param name="message">The message to display next to the spinner.</param>
        /// <param name="operation">The async operation to run.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ShowSpinnerAsync<T>(string message, Func<Task<T>> operation);

        /// <summary>
        /// Updates the player's status at the top of the UI, showing health and money.
        /// </summary>
        /// <param name="health">Player's current health.</param>
        /// <param name="money">Player's current money.</param>
        void UpdateStatus(int health, int money);
    }
}