namespace DynDungeonCrawler.Engine.Helpers
{
    /// <summary>
    /// Provides utility methods for cleaning and preparing JSON responses from LLMs.
    /// </summary>
    public static class LLMJsonCleaner
    {
        /// <summary>
        /// Cleans up LLM responses by removing markdown code fences, language tags, and trimming whitespace.
        /// </summary>
        /// <param name="llmResponse">The raw response from the LLM.</param>
        /// <returns>The cleaned JSON string.</returns>
        public static string CleanJsonResponse(string llmResponse)
        {
            if (string.IsNullOrWhiteSpace(llmResponse))
            {
                return llmResponse;
            }

            string cleaned = llmResponse.Trim();

            // Remove markdown code fences (e.g., ```json ... ```)
            if (cleaned.StartsWith("```"))
            {
                int firstFence = cleaned.IndexOf("```");
                int secondFence = cleaned.LastIndexOf("```");
                if (secondFence > firstFence)
                {
                    // Extract content between fences
                    cleaned = cleaned.Substring(firstFence + 3, secondFence - (firstFence + 3)).Trim();
                }
                else
                {
                    // Only opening fence found, remove it
                    cleaned = cleaned.Substring(firstFence + 3).Trim();
                }
            }

            // Remove language tag if present at the beginning (e.g., json, csharp, etc.)
            if (cleaned.Length > 0 && char.IsLetter(cleaned[0]) && !char.IsDigit(cleaned[0]) && !cleaned.StartsWith("{") && !cleaned.StartsWith("["))
            {
                int firstLineBreak = cleaned.IndexOfAny(new[] { '\r', '\n' });
                if (firstLineBreak > 0 && firstLineBreak < 20) // Reasonable language tag length
                {
                    string potentialTag = cleaned.Substring(0, firstLineBreak).Trim();
                    if (potentialTag.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                    {
                        cleaned = cleaned.Substring(firstLineBreak).Trim();
                    }
                }
            }

            // Ensure the string starts with a valid JSON character
            cleaned = cleaned.TrimStart();
            if (cleaned.Length > 0 && cleaned[0] != '{' && cleaned[0] != '[')
            {
                // Find the first instance of { or [
                int jsonStart = Math.Min(
                    cleaned.IndexOf('{') >= 0 ? cleaned.IndexOf('{') : int.MaxValue,
                    cleaned.IndexOf('[') >= 0 ? cleaned.IndexOf('[') : int.MaxValue
                );

                if (jsonStart < int.MaxValue)
                {
                    cleaned = cleaned.Substring(jsonStart);
                }
            }

            // Handle trailing content after the JSON
            if (cleaned.Length > 0)
            {
                // Find the last valid JSON closing character
                int lastBrace = cleaned.LastIndexOf('}');
                int lastBracket = cleaned.LastIndexOf(']');
                int jsonEnd = Math.Max(lastBrace, lastBracket);

                if (jsonEnd >= 0 && jsonEnd < cleaned.Length - 1)
                {
                    cleaned = cleaned.Substring(0, jsonEnd + 1);
                }
            }

            return cleaned;
        }
    }
}