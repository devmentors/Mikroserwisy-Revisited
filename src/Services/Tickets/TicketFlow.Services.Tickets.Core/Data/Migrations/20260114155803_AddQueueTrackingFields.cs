using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Tickets.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscalatedAt",
                schema: "tickets",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EscalatedToSupervisor",
                schema: "tickets",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                schema: "tickets",
                table: "Tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueuePosition",
                schema: "tickets",
                table: "Tickets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalatedToSupervisor",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "QueuePosition",
                schema: "tickets",
                table: "Tickets");
        }
    }
}
