using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.Entities;

public class Friendship
{
    public int Id { get; set; }
    public int UserId1 { get; set; }
    public int UserId2 { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
}
