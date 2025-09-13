using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_ModelProps_RowVer_UpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                schema: "notify",
                table: "Notifications");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVer",
                schema: "core",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVer",
                schema: "core",
                table: "UserDevices",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVer",
                schema: "notify",
                table: "Notifications",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "notify",
                table: "Notifications",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVer",
                schema: "social",
                table: "Friendships",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVer",
                schema: "social",
                table: "FriendRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "social",
                table: "FriendRequests",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                schema: "notify",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                schema: "notify",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" })
                .Annotation("SqlServer:Include", new[] { "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserId_UpdatedAt",
                schema: "social",
                table: "Friendships",
                columns: new[] { "UserId", "UpdatedAt" },
                filter: "[Status] = 'accepted'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships",
                sql: "[Status] IN ('pending','accepted','blocked')");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_ToUserId_CreatedAt",
                schema: "social",
                table: "FriendRequests",
                columns: new[] { "ToUserId", "CreatedAt" },
                filter: "[Status] = 'pending'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FriendRequests_From_To",
                schema: "social",
                table: "FriendRequests",
                sql: "[FromUserId] <> [ToUserId]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FriendRequests_Status",
                schema: "social",
                table: "FriendRequests",
                sql: "[Status] IN ('pending','accepted','rejected','canceled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                schema: "notify",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                schema: "notify",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_UserId_UpdatedAt",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_Status",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_FriendRequests_ToUserId_CreatedAt",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FriendRequests_From_To",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FriendRequests_Status",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.DropColumn(
                name: "RowVer",
                schema: "core",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVer",
                schema: "core",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "RowVer",
                schema: "notify",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "notify",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVer",
                schema: "social",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "RowVer",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "social",
                table: "FriendRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                schema: "notify",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });
        }
    }
}
