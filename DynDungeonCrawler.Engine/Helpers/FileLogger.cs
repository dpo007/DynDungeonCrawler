using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public FileLogger(string filePath = "dyncrawler.log")
        {
            _filePath = filePath;
        }

        public void Log(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_filePath, logLine + Environment.NewLine);
            }
        }
    }
}