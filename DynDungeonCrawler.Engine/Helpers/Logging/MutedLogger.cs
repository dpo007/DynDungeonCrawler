using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers.Logging
{
    /// <summary>
    /// An ILogger implementation that ignores all log messages.
    /// </summary>
    public class MutedLogger : ILogger
    {
        public void Log(string message)
        {
            // Intentionally does nothing
        }
    }
}