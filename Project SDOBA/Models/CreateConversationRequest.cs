using System.Text.Json.Serialization;

namespace Project_SDOBA.Models;

public class CreateConversationRequest
{
    [JsonPropertyName("user_ids")]
    public List<uint> UserIds { get; set; } = new();
}