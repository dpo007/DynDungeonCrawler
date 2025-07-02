using DynDungeonCrawler.Engine.Interfaces;

namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Dummy LLM client for scenarios where no LLM calls are needed (e.g., map viewing).
    /// Always returns an empty string.
    /// </summary>
    public class DummyLLMClient : ILLMClient
    {
        public Task<string> GetResponseAsync(string userPrompt, string systemPrompt = "")
            => Task.FromResult(string.Empty);
    }
}