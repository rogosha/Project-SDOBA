namespace Project_SDOBA.Models;

public class Conversation
{
    public uint Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ConversationMember> Members { get; set; } = new();

    public string DisplayName { get; set; } = string.Empty;

    public string LastMessage { get; set; } = string.Empty;
}

public class ConversationMember
{
    public uint Id { get; set; }

    public uint ConversationId { get; set; }

    public uint UserId { get; set; }

    public User? User { get; set; }
}