using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Models
{
    /// <summary>
    /// Represents a game message with content and metadata for UI rendering.
    /// </summary>
    public class GameMessage
    {
        /// <summary>
        /// Gets the raw content of the message.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the type of message for UI styling.
        /// </summary>
        public UIMessageType Type { get; }

        /// <summary>
        /// Gets optional formatting parameters for the message.
        /// </summary>
        public Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameMessage"/> class.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="type">The type of message for styling.</param>
        /// <param name="parameters">Optional formatting parameters.</param>
        public GameMessage(string content, UIMessageType type = UIMessageType.Normal, Dictionary<string, object>? parameters = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Type = type;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a normal message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Normal type.</returns>
        public static GameMessage Normal(string content) => new(content, UIMessageType.Normal);

        /// <summary>
        /// Creates an error message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Error type.</returns>
        public static GameMessage Error(string content) => new(content, UIMessageType.Error);

        /// <summary>
        /// Creates a success message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Success type.</returns>
        public static GameMessage Success(string content) => new(content, UIMessageType.Success);

        /// <summary>
        /// Creates an emphasis message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Emphasis type.</returns>
        public static GameMessage Emphasis(string content) => new(content, UIMessageType.Emphasis);

        /// <summary>
        /// Creates a combat action message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with CombatAction type.</returns>
        public static GameMessage CombatAction(string content) => new(content, UIMessageType.CombatAction);

        /// <summary>
        /// Creates a combat critical message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with CombatCritical type.</returns>
        public static GameMessage CombatCritical(string content) => new(content, UIMessageType.CombatCritical);

        /// <summary>
        /// Creates an enemy status message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with EnemyStatus type.</returns>
        public static GameMessage EnemyStatus(string content) => new(content, UIMessageType.EnemyStatus);

        /// <summary>
        /// Creates an item info message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with ItemInfo type.</returns>
        public static GameMessage ItemInfo(string content) => new(content, UIMessageType.ItemInfo);

        /// <summary>
        /// Creates a help message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Help type.</returns>
        public static GameMessage Help(string content) => new(content, UIMessageType.Help);

        /// <summary>
        /// Creates a header message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with Header type.</returns>
        public static GameMessage Header(string content) => new(content, UIMessageType.Header);

        /// <summary>
        /// Creates a player status message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with PlayerStatus type.</returns>
        public static GameMessage PlayerStatus(string content) => new(content, UIMessageType.PlayerStatus);

        /// <summary>
        /// Creates a room description message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with RoomDescription type.</returns>
        public static GameMessage RoomDescription(string content) => new(content, UIMessageType.RoomDescription);

        /// <summary>
        /// Creates a system message.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <returns>A new GameMessage with System type.</returns>
        public static GameMessage System(string content) => new(content, UIMessageType.System);

        /// <summary>
        /// Returns a string representation of the message for debugging.
        /// </summary>
        /// <returns>A string containing the message type and content.</returns>
        public override string ToString() => $"[{Type}] {Content}";
    }
}