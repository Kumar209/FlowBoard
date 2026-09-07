using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notification.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixNotificationRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_EventId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "OccurredOn",
                schema: "notification",
                table: "Notifications",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "ActorId",
                schema: "notification",
                table: "Notifications",
                newName: "RecipientUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_OccurredOn",
                schema: "notification",
                table: "Notifications",
                newName: "IX_Notifications_OccurredOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_ActorId",
                schema: "notification",
                table: "Notifications",
                newName: "IX_Notifications_RecipientUserId");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkspaceId",
                schema: "notification",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                schema: "notification",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorUserId",
                schema: "notification",
                table: "Notifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_EventId",
                schema: "notification",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_ActorUserId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_EventId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "RecipientUserId",
                schema: "notification",
                table: "Notifications",
                newName: "ActorId");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                schema: "notification",
                table: "Notifications",
                newName: "OccurredOn");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_RecipientUserId",
                schema: "notification",
                table: "Notifications",
                newName: "IX_Notifications_ActorId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_OccurredOnUtc",
                schema: "notification",
                table: "Notifications",
                newName: "IX_Notifications_OccurredOn");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventId",
                schema: "notification",
                table: "Notifications",
                column: "EventId",
                unique: true);
        }
    }
}
