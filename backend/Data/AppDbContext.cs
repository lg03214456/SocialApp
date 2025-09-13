// Data/AppDbContext.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SocialApp.Api.Models;            // ★ 一定要是 .Api.Models
using SocialApp.Api.Models.Core;
using SocialApp.Api.Models.Auth;
using SocialApp.Api.Models.Social;
using SocialApp.Api.Models.Notify;

namespace SocialApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Phase A
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Users (core)
        b.Entity<User>(e =>
        {
            e.ToTable("Users", "core");
            e.HasKey(x => x.Id);

            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();

            e.Property(x => x.UserId).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.UserId).IsUnique();

            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.PasswordHash).IsRequired();

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.LastSeenAt).HasColumnType("datetimeoffset(7)");

            e.HasIndex(x => x.UpdatedAt);

            // RowVersion（rowversion；陰影或實體屬性皆可）
            e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
        });

        // RefreshTokens (auth)
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens", "auth");
            e.HasKey(x => x.Id);

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.ExpiresAt).HasColumnType("datetimeoffset(7)");
            e.Property(x => x.RevokedAt).HasColumnType("datetimeoffset(7)");

            e.HasIndex(x => new { x.UserId, x.TokenHash }).IsUnique();

            e.HasOne(x => x.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // UserDevices (core)
        b.Entity<UserDevice>(e =>
        {
            e.ToTable("UserDevices", "core");
            e.HasKey(x => x.Id);

            e.Property(x => x.DeviceId).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeviceType).HasMaxLength(16).IsRequired();
            e.Property(x => x.PushToken).HasMaxLength(256);

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.LastSeenAt).HasColumnType("datetimeoffset(7)");

            e.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.PushToken);

            e.HasOne(x => x.User)
             .WithMany(u => u.Devices)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // RowVersion
            e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
        });

        // Friendships (social)
        b.Entity<Friendship>(e =>
        {
            e.ToTable("Friendships", "social");
            e.HasKey(x => x.Id);

            e.Property(x => x.Status).HasMaxLength(16).IsRequired();

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            e.HasIndex(x => new { x.UserId, x.FriendUserId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Status, x.UpdatedAt });

            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.FriendUser).WithMany()
             .HasForeignKey(x => x.FriendUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // RowVersion
            e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();

            // 值域檢核
            e.ToTable(tb => tb.HasCheckConstraint("CK_Friendships_Status",
                "[Status] IN ('pending','accepted','blocked')"));

            // 常用清單（只查 accepted）
            e.HasIndex(x => new { x.UserId, x.UpdatedAt }).HasFilter("[Status] = 'accepted'");
        });

        // FriendRequests (social)
        b.Entity<FriendRequest>(e =>
        {
            e.ToTable("FriendRequests", "social");
            e.HasKey(x => x.RequestId);

            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.Message).HasMaxLength(200);

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            e.Property(x => x.RespondedAt).HasColumnType("datetimeoffset(7)");

            // UpdatedAt：有沒有實體屬性都 OK（Fluent 會套到同名欄位）
            e.Property<DateTimeOffset>("UpdatedAt")
             .HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            e.HasIndex(x => new { x.FromUserId, x.ToUserId, x.Status }).IsUnique();
            e.HasIndex(x => new { x.ToUserId, x.Status, x.CreatedAt });

            e.HasOne(x => x.FromUser).WithMany()
             .HasForeignKey(x => x.FromUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ToUser).WithMany()
             .HasForeignKey(x => x.ToUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // RowVersion
            e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();

            // 檢核：不可自己邀自己 + 狀態值域
            e.ToTable(tb => tb.HasCheckConstraint("CK_FriendRequests_From_To", "[FromUserId] <> [ToUserId]"));
            e.ToTable(tb => tb.HasCheckConstraint("CK_FriendRequests_Status",
                "[Status] IN ('pending','accepted','rejected','canceled')"));

            // 我收到的 pending（時間序）
            e.HasIndex(x => new { x.ToUserId, x.CreatedAt }).HasFilter("[Status] = 'pending'");
        });

        // Notifications (notify)
        b.Entity<Notification>(e =>
        {
            e.ToTable("Notifications", "notify");
            e.HasKey(x => x.NotificationId);

            e.Property(x => x.Type).HasMaxLength(24).IsRequired();
            e.Property(x => x.Payload).IsRequired();

            e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // UpdatedAt：有沒有實體屬性都 OK
            e.Property<DateTimeOffset>("UpdatedAt")
             .HasColumnType("datetimeoffset(7)")
             .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // 未讀快查（含 CreatedAt 作為 INCLUDE）
            e.HasIndex(x => new { x.UserId, x.IsRead })
             .IncludeProperties(x => new { x.CreatedAt });

            // 最新通知（時間序）
            e.HasIndex(x => new { x.UserId, x.CreatedAt });

            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // RowVersion
            e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
        });
    }

    // ★ 在提交前自動把「所有需要的表」的 UpdatedAt 設為現在
    public override int SaveChanges()
    {
        TouchUpdatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;

        // 1) 這三張有實體屬性 UpdatedAt：直接寫屬性
        foreach (var e in ChangeTracker.Entries<User>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
            e.Entity.UpdatedAt = now;

        foreach (var e in ChangeTracker.Entries<UserDevice>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
            e.Entity.UpdatedAt = now;

        foreach (var e in ChangeTracker.Entries<Friendship>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
            e.Entity.UpdatedAt = now;

        // 2) 這兩張用欄位名指定（有沒有屬性都能對應到同一欄位）
        foreach (var e in ChangeTracker.Entries<FriendRequest>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
            e.Entity.UpdatedAt = now;

        foreach (var e in ChangeTracker.Entries<Notification>()
                     .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified))
            e.Entity.UpdatedAt = now;
    }
}
