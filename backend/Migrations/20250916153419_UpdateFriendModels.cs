using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFriendModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Friendships_UserId_Status_UpdatedAt",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_FriendRequests_FromUserId_ToUserId_Status",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships",
                sql: "[Status] IN ('accepted','blocked')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_User_Distinct",
                schema: "social",
                table: "Friendships",
                sql: "[UserId] <> [FriendUserId]");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_FromUserId_ToUserId",
                schema: "social",
                table: "FriendRequests",
                columns: new[] { "FromUserId", "ToUserId" },
                unique: true,
                filter: "[Status] = 'pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_User_Distinct",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_FriendRequests_FromUserId_ToUserId",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserId_Status_UpdatedAt",
                schema: "social",
                table: "Friendships",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships",
                sql: "[Status] IN ('pending','accepted','blocked')");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_FromUserId_ToUserId_Status",
                schema: "social",
                table: "FriendRequests",
                columns: new[] { "FromUserId", "ToUserId", "Status" },
                unique: true);
        }
    }
}
