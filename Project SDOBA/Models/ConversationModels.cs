using System.Text.Json.Serialization;

namespace Project_SDOBA.Models;

public class Conversation
{
    public uint Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public object? Members { get; set; }
}