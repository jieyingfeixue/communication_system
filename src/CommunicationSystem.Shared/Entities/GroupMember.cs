namespace CommunicationSystem.Shared.Entities;

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;

    public ChatGroup Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
