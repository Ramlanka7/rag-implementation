using System.Text;

namespace RagApi.Services;

/// <summary>
/// Assembles the system prompt by injecting retrieved context documents into the template.
/// </summary>
public class PromptBuilder
{
    private const string SystemPromptTemplate =
        """
        You are an AI assistant for the AdventureWorks business.
        Answer only using the context below.
        If the context does not contain enough information, say you don't know.
        Do not make up data.

        Context:
        {0}
        """;

    /// <summary>
    /// Builds a system prompt string with all context documents embedded.
    /// </summary>
    public string BuildPrompt(string question, List<SearchResult> context)
    {
        if (context.Count == 0)
            return string.Format(SystemPromptTemplate, "(No context available.)");

        var sb = new StringBuilder();
        for (int i = 0; i < context.Count; i++)
        {
            var doc = context[i];
            sb.AppendLine($"[{i + 1}] (id: {doc.Id}, source: {doc.Source})");
            sb.AppendLine(doc.Content);
            sb.AppendLine();
        }

        return string.Format(SystemPromptTemplate, sb.ToString().TrimEnd());
    }
}
