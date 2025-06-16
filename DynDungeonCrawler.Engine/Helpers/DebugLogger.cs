using DynDungeonCrawler.Engine.Interfaces;
using System.Diagnostics;

namespace DynDungeonCrawler.Engine.Helpers
{
    public class DebugLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.WriteLine(message);
        }
    }
}