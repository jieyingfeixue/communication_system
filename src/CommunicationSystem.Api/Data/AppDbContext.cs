using CommunicationSystem.Shared.Entities;
using CommunicationSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunicationSystem.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<ChatGroup> Groups => Set<ChatGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.Nickname).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.Avatar).HasMaxLength(255);
        });

        modelBuilder.Entity<Friendship>(e =>
        {
            e.ToTable("Friendship");
            e.HasIndex(x => new { x.UserId1, x.UserId2 }).IsUnique();
            e.HasOne(x => x.User1).WithMany(x => x.FriendshipsInitiated).HasForeignKey(x => x.UserId1);
            e.HasOne(x => x.User2).WithMany(x => x.FriendshipsReceived).HasForeignKey(x => x.UserId2);
        });

        modelBuilder.Entity<ChatGroup>(e =>
        {
            e.ToTable("Group");
            e.Property(x => x.GroupName).HasMaxLength(100);
            e.HasOne(x => x.Owner).WithMany(x => x.OwnedGroups).HasForeignKey(x => x.OwnerId);
        });

        modelBuilder.Entity<GroupMember>(e =>
        {
            e.ToTable("GroupMember");
            e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            e.HasOne(x => x.Group).WithMany(x => x.Members).HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.User).WithMany(x => x.GroupMemberships).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("Message");
            e.HasOne(x => x.Sender).WithMany(x => x.SentMessages).HasForeignKey(x => x.SenderId);
            e.HasOne(x => x.Receiver).WithMany(x => x.ReceivedMessages).HasForeignKey(x => x.ReceiverId);
            e.HasOne(x => x.Group).WithMany(x => x.Messages).HasForeignKey(x => x.GroupId);
        });
    }

    public async Task SeedAsync()
    {
        if (await Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Nickname = "系统管理员",
            Status = UserStatus.Active,
            Role = UserRole.Admin
        });
        await SaveChangesAsync();
    }
}
