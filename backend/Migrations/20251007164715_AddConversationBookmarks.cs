using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationBookmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationBookmarks",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PinnedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    RowVer = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationBookmarks_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "chat",
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationBookmarks_Messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConversationBookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_ConversationId",
                schema: "chat",
                table: "ConversationBookmarks",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_MessageId",
                schema: "chat",
                table: "ConversationBookmarks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_UserId",
                schema: "chat",
                table: "ConversationBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_UserId_ConversationId",
                schema: "chat",
                table: "ConversationBookmarks",
                columns: new[] { "UserId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_UserId_ConversationId_MessageId",
                schema: "chat",
                table: "ConversationBookmarks",
                columns: new[] { "UserId", "ConversationId", "MessageId" },
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_UserId_PinnedAt",
                schema: "chat",
                table: "ConversationBookmarks",
                columns: new[] { "UserId", "PinnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationBookmarks_UserId_SortOrder",
                schema: "chat",
                table: "ConversationBookmarks",
                columns: new[] { "UserId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationBookmarks",
                schema: "chat");
        }
    }
}
