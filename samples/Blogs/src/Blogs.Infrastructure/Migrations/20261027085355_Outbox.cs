using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Outbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows left by RootBlocks 0.5 were already published in-process when they were committed, and their
            // Event column cannot be read back reliably: drop them rather than publish them again.
            migrationBuilder.Sql("DELETE FROM [dbo].[Outbox];");

            migrationBuilder.RenameColumn(
                name: "Event",
                schema: "dbo",
                table: "Outbox",
                newName: "Payload");

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "dbo",
                table: "Outbox",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                schema: "dbo",
                table: "Outbox",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                schema: "dbo",
                table: "Outbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredOn",
                schema: "dbo",
                table: "Outbox",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedOn",
                schema: "dbo",
                table: "Outbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "dbo",
                table: "Outbox",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_ProcessedOn_OccurredOn",
                schema: "dbo",
                table: "Outbox",
                columns: new[] { "ProcessedOn", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // RootBlocks 0.5 cannot read the rows of the new outbox.
            migrationBuilder.Sql("DELETE FROM [dbo].[Outbox];");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_ProcessedOn_OccurredOn",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LastError",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "OccurredOn",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "ProcessedOn",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "dbo",
                table: "Outbox");

            migrationBuilder.RenameColumn(
                name: "Payload",
                schema: "dbo",
                table: "Outbox",
                newName: "Event");
        }
    }
}
