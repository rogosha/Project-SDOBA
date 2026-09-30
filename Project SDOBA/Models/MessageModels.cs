using System.Text.Json.Serialization;

namespace Project_SDOBA.Models;

public class Message
{
    public uint Id { get; set; }
    public uint ConversationId { get; set; }
    public uint SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? Sender { get; set; }

    [JsonIgnore]
    public bool IsOwnMessage { get; set; }

    [JsonIgnore]
    public bool ShowDate { get; set; }
}

public class User
{
    public uint Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}