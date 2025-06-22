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
        void WriteLine(string message);

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
        /// Reads a line of input from the user.
        /// </summary>
        /// <returns>The input string, or an empty string if input is null.</returns>
        string ReadLine();

        /// <summary>
        /// Reads a key press from the user.
        /// </summary>
        /// <param name="intercept">If true, the key is not displayed in the output.</param>
        /// <returns>The key that was pressed.</returns>
        ConsoleKey ReadKey(bool intercept = false);

        /// <summary>
        /// Clears the output display.
        /// </summary>
        void Clear();
    }
}