using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
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