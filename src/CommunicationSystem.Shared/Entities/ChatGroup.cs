namespace CommunicationSystem.Shared.Entities;

public class ChatGroup
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
