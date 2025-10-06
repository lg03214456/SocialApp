// Data/AppDbContext.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
//using SocialApp.Api.Models;            // ★ 一定要是 .Api.Models
using SocialApp.Api.Models.Core;
using SocialApp.Api.Models.Auth;
using SocialApp.Api.Models.Social;
using SocialApp.Api.Models.Notify;
using SocialApp.Api.Models.Chat;


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
  // === Chat ===
  public DbSet<Conversation> Conversations => Set<Conversation>();
  public DbSet<ConversationStats> ConversationStats => Set<ConversationStats>();
  public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
  public DbSet<Message> Messages => Set<Message>();
  public DbSet<Attachment> Attachments => Set<Attachment>();
  public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
  public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
  public DbSet<ConversationMembershipEvent> ConversationMembershipEvents => Set<ConversationMembershipEvent>();


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

    // Friendships (social) — 雙筆存、無 pending
    b.Entity<Friendship>(e =>
    {
      e.ToTable("Friendships", "social");
      e.HasKey(x => x.Id);

      e.Property(x => x.Status).HasMaxLength(16).IsRequired();

      e.Property(x => x.CreatedAt)
   .HasColumnType("datetimeoffset(7)")
   .HasDefaultValueSql("SYSDATETIMEOFFSET()");
      e.Property(x => x.UpdatedAt)
   .HasColumnType("datetimeoffset(7)")
   .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      // 同向唯一（雙筆存：A→B 與 B→A 各一筆）
      e.HasIndex(x => new { x.UserId, x.FriendUserId }).IsUnique();

      // 常用查詢：我的好友（只取 accepted）
      e.HasIndex(x => new { x.UserId, x.UpdatedAt })
   .HasFilter("[Status] = 'accepted'");

      e.HasOne(x => x.User).WithMany()
   .HasForeignKey(x => x.UserId)
   .OnDelete(DeleteBehavior.Restrict);

      e.HasOne(x => x.FriendUser).WithMany()
   .HasForeignKey(x => x.FriendUserId)
   .OnDelete(DeleteBehavior.Restrict);

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();

      // 不可自己加自己
      e.ToTable(tb => tb.HasCheckConstraint(
      "CK_Friendships_User_Distinct",
      "[UserId] <> [FriendUserId]"));

      // 狀態值域：『accepted』『blocked』；不需要 pending
      e.ToTable(tb => tb.HasCheckConstraint(
      "CK_Friendships_Status",
      "[Status] IN ('accepted','blocked')"));
    });

    // FriendRequests (social)
    b.Entity<FriendRequest>(e =>
    {
      e.ToTable("FriendRequests", "social");
      e.HasKey(x => x.RequestId);

      e.Property(x => x.Status).HasMaxLength(16).IsRequired();
      e.Property(x => x.Message).HasMaxLength(200);

      e.Property(x => x.CreatedAt)
   .HasColumnType("datetimeoffset(7)")
   .HasDefaultValueSql("SYSDATETIMEOFFSET()");
      e.Property(x => x.RespondedAt)
   .HasColumnType("datetimeoffset(7)");
      e.Property(x => x.UpdatedAt)
   .HasColumnType("datetimeoffset(7)")
   .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      // ✅ 只限制 pending 唯一
      e.HasIndex(x => new { x.FromUserId, x.ToUserId })
   .IsUnique()
   .HasFilter("[Status] = 'pending'");

      // 我收到的邀請（常用：依狀態與時間）
      e.HasIndex(x => new { x.ToUserId, x.Status, x.CreatedAt });

      e.HasOne(x => x.FromUser)
      .WithMany()
   .HasForeignKey(x => x.FromUserId)
   .OnDelete(DeleteBehavior.Restrict);

      e.HasOne(x => x.ToUser).WithMany()
   .HasForeignKey(x => x.ToUserId)
   .OnDelete(DeleteBehavior.Restrict);

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();

      e.ToTable(tb => tb.HasCheckConstraint(
      "CK_FriendRequests_From_To",
      "[FromUserId] <> [ToUserId]"));

      e.ToTable(tb => tb.HasCheckConstraint(
      "CK_FriendRequests_Status",
      "[Status] IN ('pending','accepted','rejected','canceled')"));

      // 收到的 pending（時間序）
      e.HasIndex(x => new { x.ToUserId, x.CreatedAt })
   .HasFilter("[Status] = 'pending'");
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
      e.Property(x => x.UpdatedAt)
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

    // === Conversations ===
    b.Entity<Conversation>(e =>
    {
      e.ToTable("Conversations", "chat");
      e.HasKey(x => x.ConversationId);

      e.Property(x => x.Type).HasMaxLength(16).IsRequired();
      e.Property(x => x.Title).HasMaxLength(128);
      e.Property(x => x.AvatarUrl).HasMaxLength(256);
      e.Property(x => x.Description).HasMaxLength(400);
      e.Property(x => x.Visibility).HasMaxLength(16).IsRequired();

      e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");
      e.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      // DM 唯一（Type='dm' 時套用；A<B 由 CHECK 保證）
      e.HasIndex(x => new { x.DmUserAId, x.DmUserBId })
        .HasFilter("[Type] = 'dm'")
        .IsUnique();

      e.ToTable(tb =>
      {
        tb.HasCheckConstraint("CK_Conversations_Type",
          "[Type] IN ('dm','group')");
        tb.HasCheckConstraint("CK_Conversations_DM_A_LT_B",
          "([Type] <> 'dm') OR ([DmUserAId] IS NOT NULL AND [DmUserBId] IS NOT NULL AND [DmUserAId] < [DmUserBId])");
      });

      e.HasIndex(x => x.UpdatedAt);
      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === ConversationStats ===
    b.Entity<ConversationStats>(e =>
    {
      e.ToTable("ConversationStats", "chat");
      e.HasKey(x => x.ConversationId);

      e.Property(x => x.LastMessagePreview).HasMaxLength(200);
      e.Property(x => x.LastMessageAt).HasColumnType("datetimeoffset(7)");
      e.Property(x => x.MessageCount).HasDefaultValue(0);
      e.Property(x => x.MemberCount).HasDefaultValue(0);
      e.Property(x => x.NextMessageSeq).HasDefaultValue(0);

      e.HasOne<Conversation>()
        .WithOne()
        .HasForeignKey<ConversationStats>(x => x.ConversationId)
        .OnDelete(DeleteBehavior.Cascade);

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === ConversationParticipants ===
    b.Entity<ConversationParticipant>(e =>
    {
      e.ToTable("ConversationParticipants", "chat");
      e.HasKey(x => new { x.ConversationId, x.UserId });

      e.Property(x => x.Status).HasMaxLength(16).IsRequired();
      e.Property(x => x.Role).HasMaxLength(16).IsRequired();
      e.Property(x => x.MuteUntil).HasColumnType("datetimeoffset(7)");
      e.Property(x => x.JoinedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");
      e.Property(x => x.LeftAt).HasColumnType("datetimeoffset(7)");
      e.Property(x => x.LastReadAt).HasColumnType("datetimeoffset(7)");

      e.HasIndex(x => x.UserId)
        .IncludeProperties(x => new { x.ConversationId, x.LastReadAt, x.ReadUpToMessageId, x.Role, x.Status });

      e.HasIndex(x => x.ConversationId)
        .HasFilter("[Status] = 'member'");

      e.ToTable(tb =>
      {
        tb.HasCheckConstraint("CK_Participants_Status",
          "[Status] IN ('pending','member','rejected','left','removed')");
        tb.HasCheckConstraint("CK_Participants_Role",
          "[Role] IN ('owner','admin','member')");
      });

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === Messages ===
    b.Entity<Message>(e =>
    {
      e.ToTable("Messages", "chat");
      e.HasKey(x => x.MessageId);

      e.Property(x => x.Kind).HasMaxLength(16).IsRequired();
      e.Property(x => x.Text).HasMaxLength(4000);
      e.Property(x => x.ClientMessageId).HasMaxLength(50);
      e.Property(x => x.MetaJson);
      e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");
      e.Property(x => x.EditedAt).HasColumnType("datetimeoffset(7)");
      e.Property(x => x.DeletedAt).HasColumnType("datetimeoffset(7)");

      // 查詢/分頁
      e.HasIndex(x => new { x.ConversationId, x.CreatedAt, x.MessageId });

      // 會話內序號（可選）
      e.HasIndex(x => new { x.ConversationId, x.MessageSeq }).IsUnique();

      // 去重（同一會話+同一送訊者 的同 ClientMessageId 只留一筆）
      e.HasIndex(x => new { x.ConversationId, x.SenderId, x.ClientMessageId })
        .IsUnique()
        .HasFilter("[ClientMessageId] IS NOT NULL");

      e.ToTable(tb =>
      {
        tb.HasCheckConstraint("CK_Messages_Kind",
          "[Kind] IN ('text','sticker','image','file','system')");
        tb.HasCheckConstraint("CK_Messages_Text_When_Text",
          "( [Kind] = 'text' AND [Text] IS NOT NULL ) OR ( [Kind] <> 'text' AND [Text] IS NULL )");
        tb.HasCheckConstraint("CK_Messages_Sticker_When_Sticker",
          "( [Kind] = 'sticker' AND [StickerId] IS NOT NULL ) OR ( [Kind] <> 'sticker' AND [StickerId] IS NULL )");
      });

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === Attachments ===
    b.Entity<Attachment>(e =>
    {
      e.ToTable("Attachments", "chat");
      e.HasKey(x => x.AttachmentId);

      e.Property(x => x.BlobKey).HasMaxLength(128).IsRequired();
      e.Property(x => x.OriginalFileName).HasMaxLength(256).IsRequired();
      e.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
      e.Property(x => x.ChecksumSha256).HasMaxLength(64);

      e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      // 可選：內容去重用
      e.HasIndex(x => x.ChecksumSha256).IsUnique()
        .HasFilter("[ChecksumSha256] IS NOT NULL");

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === MessageAttachments ===
    b.Entity<MessageAttachment>(e =>
    {
      e.ToTable("MessageAttachments", "chat");
      e.HasKey(x => new { x.MessageId, x.AttachmentId });

      e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
      e.Property(x => x.SortOrder).HasDefaultValue(0);

      // 同一訊息內顯示名唯一（後端會做 (1) 後再存）
      e.HasIndex(x => new { x.MessageId, x.DisplayName }).IsUnique();

      e.HasOne<Message>().WithMany()
        .HasForeignKey(x => x.MessageId)
        .OnDelete(DeleteBehavior.Cascade);

      e.HasOne<Attachment>().WithMany()
        .HasForeignKey(x => x.AttachmentId)
        .OnDelete(DeleteBehavior.Cascade);

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === MessageReactions ===
    b.Entity<MessageReaction>(e =>
    {
      e.ToTable("MessageReactions", "chat");
      e.HasKey(x => new { x.MessageId, x.UserId, x.Emoji });

      e.Property(x => x.Emoji).HasMaxLength(32).IsRequired();
      e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      e.Property<byte[]>("RowVer").IsRowVersion().IsConcurrencyToken();
    });

    // === ConversationMembershipEvents ===
    b.Entity<ConversationMembershipEvent>(e =>
    {
      e.ToTable("ConversationMembershipEvents", "chat");
      e.HasKey(x => x.EventId);

      e.Property(x => x.EventType).HasMaxLength(32).IsRequired();
      e.Property(x => x.ReasonCode).HasMaxLength(32);
      e.Property(x => x.ReasonText).HasMaxLength(400);
      e.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)")
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");

      e.HasIndex(x => new { x.ConversationId, x.CreatedAt });

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

    foreach (var e in ChangeTracker.Entries<Conversation>()
         .Where(x => x.State is EntityState.Added or EntityState.Modified))
      e.Entity.UpdatedAt = now;

    foreach (var e in ChangeTracker.Entries<ConversationParticipant>()
         .Where(x => x.State is EntityState.Added or EntityState.Modified))
      e.Entity.JoinedAt = e.Entity.JoinedAt == default ? now : e.Entity.JoinedAt;

  }
}
