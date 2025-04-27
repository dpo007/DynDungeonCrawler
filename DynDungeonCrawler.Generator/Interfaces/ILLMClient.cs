using DynDungeonCrawler.Constants;

namespace DynDungeonCrawler.Interfaces
{
    public interface ILLMClient
    {
        /// <summary>
        /// Sends a prompt and an optional system prompt to the LLM and retrieves the response.
        /// </summary>
        /// <param name="systemPrompt">The system-level prompt to guide the LLM's behavior. Defaults to a description generator prompt.</param>
        /// <param name="userPrompt">The user-level prompt to send to the LLM.</param>
        /// <returns>The response from the LLM.</returns>
        Task<string> GetResponseAsync(string userPrompt, string systemPrompt = LLMDefaults.DefaultSystemPrompt);
    }
}