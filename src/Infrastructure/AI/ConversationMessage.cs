namespace AiAdvisor.Infrastructure.AI;

/// <summary>
/// Represents a message in a conversation history.
/// </summary>
public record ConversationMessage(
    string Role,
    string Content,
    DateTime Timestamp = default)
{
    public ConversationMessage(string role, string content) 
        : this(role, content, DateTime.UtcNow)
    {
    }

    public static ConversationMessage User(string content) => new("user", content);
    public static ConversationMessage Assistant(string content) => new("assistant", content);
}
