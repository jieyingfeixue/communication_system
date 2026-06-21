using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.Entities;

public class Message
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public User Sender { get; set; } = null!;
    public User? Receiver { get; set; }
    public ChatGroup? Group { get; set; }
}
