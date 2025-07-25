﻿using DynDungeonCrawler.Engine.Classes;
using DynDungeonCrawler.Engine.Models;

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
        /// Writes a game message with appropriate styling based on message type.
        /// </summary>
        /// <param name="message">The game message to write.</param>
        /// <param name="center">Whether to center the message horizontally in the output view.</param>
        void WriteMessage(GameMessage message, bool center = false);

        /// <summary>
        /// Writes multiple game messages in sequence.
        /// </summary>
        /// <param name="messages">The collection of game messages to write.</param>
        void WriteMessages(IEnumerable<GameMessage> messages);

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
        /// Updates the player's status at the top of the UI, showing name, strength, defense, HP, and coins.
        /// </summary>
        /// <param name="player">The Adventurer whose status to display.</param>
        void UpdateStatus(Adventurer player);

        /// <summary>
        /// Displays text one sentence at a time with pauses between sentences for dramatic effect.
        /// </summary>
        /// <param name="text">The text to display sentence by sentence.</param>
        /// <param name="pauseMs">Optional fixed pause duration in milliseconds between sentences.
        /// If not specified, random pauses between 2000-4000ms will be used.</param>
        /// <param name="endNewLine">Whether to add a newline after all sentences are displayed.</param>
        /// <returns>A task that completes when all sentences have been displayed.</returns>
        Task WriteSlowlyBySentenceAsync(string text, int? pauseMs = null, bool endNewLine = true);
    }
}