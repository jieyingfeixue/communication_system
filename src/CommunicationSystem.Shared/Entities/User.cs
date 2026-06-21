using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<Friendship> FriendshipsInitiated { get; set; } = [];
    public ICollection<Friendship> FriendshipsReceived { get; set; } = [];
    public ICollection<ChatGroup> OwnedGroups { get; set; } = [];
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<Message> ReceivedMessages { get; set; } = [];
}
