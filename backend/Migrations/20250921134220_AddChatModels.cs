using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "chat",
                columns: table => new
                {
                    AttachmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlobKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    UploaderUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentId);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMembershipEvents",
                schema: "chat",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReasonText = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembershipEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                schema: "chat",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ReadUpToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    MuteUntil = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    InvitedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    LeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => new { x.ConversationId, x.UserId });
                    table.CheckConstraint("CK_Participants_Role", "[Role] IN ('owner','admin','member')");
                    table.CheckConstraint("CK_Participants_Status", "[Status] IN ('pending','member','rejected','left','removed')");
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "chat",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: true),
                    Visibility = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DmUserAId = table.Column<long>(type: "bigint", nullable: true),
                    DmUserBId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationId);
                    table.CheckConstraint("CK_Conversations_DM_A_LT_B", "([Type] <> 'dm') OR ([DmUserAId] IS NOT NULL AND [DmUserBId] IS NOT NULL AND [DmUserAId] < [DmUserBId])");
                    table.CheckConstraint("CK_Conversations_Type", "[Type] IN ('dm','group')");
                });

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                schema: "chat",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Emoji = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => new { x.MessageId, x.UserId, x.Emoji });
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "chat",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StickerId = table.Column<long>(type: "bigint", nullable: true),
                    MetaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    MessageSeq = table.Column<long>(type: "bigint", nullable: false),
                    ClientMessageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    EditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    EditedCount = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.CheckConstraint("CK_Messages_Kind", "[Kind] IN ('text','sticker','image','file','system')");
                    table.CheckConstraint("CK_Messages_Sticker_When_Sticker", "( [Kind] = 'sticker' AND [StickerId] IS NOT NULL ) OR ( [Kind] <> 'sticker' AND [StickerId] IS NULL )");
                    table.CheckConstraint("CK_Messages_Text_When_Text", "( [Kind] = 'text' AND [Text] IS NOT NULL ) OR ( [Kind] <> 'text' AND [Text] IS NULL )");
                });

            migrationBuilder.CreateTable(
                name: "ConversationStats",
                schema: "chat",
                columns: table => new
                {
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    LastMessageId = table.Column<long>(type: "bigint", nullable: true),
                    LastMessagePreview = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MemberCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextMessageSeq = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationStats", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_ConversationStats_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "chat",
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                schema: "chat",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => new { x.MessageId, x.AttachmentId });
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalSchema: "chat",
                        principalTable: "Attachments",
                        principalColumn: "AttachmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ChecksumSha256",
                schema: "chat",
                table: "Attachments",
                column: "ChecksumSha256",
                unique: true,
                filter: "[ChecksumSha256] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembershipEvents_ConversationId_CreatedAt",
                schema: "chat",
                table: "ConversationMembershipEvents",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_ConversationId",
                schema: "chat",
                table: "ConversationParticipants",
                column: "ConversationId",
                filter: "[Status] = 'member'");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_UserId",
                schema: "chat",
                table: "ConversationParticipants",
                column: "UserId")
                .Annotation("SqlServer:Include", new[] { "ConversationId", "LastReadAt", "ReadUpToMessageId", "Role", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_DmUserAId_DmUserBId",
                schema: "chat",
                table: "Conversations",
                columns: new[] { "DmUserAId", "DmUserBId" },
                unique: true,
                filter: "[Type] = 'dm'");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UpdatedAt",
                schema: "chat",
                table: "Conversations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_AttachmentId",
                schema: "chat",
                table: "MessageAttachments",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_MessageId_DisplayName",
                schema: "chat",
                table: "MessageAttachments",
                columns: new[] { "MessageId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_CreatedAt_MessageId",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ConversationId", "CreatedAt", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_MessageSeq",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ConversationId", "MessageSeq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SenderId_ClientMessageId",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ConversationId", "SenderId", "ClientMessageId" },
                unique: true,
                filter: "[ClientMessageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMembershipEvents",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "ConversationParticipants",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "ConversationStats",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "MessageAttachments",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "MessageReactions",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "chat");
        }
    }
}
