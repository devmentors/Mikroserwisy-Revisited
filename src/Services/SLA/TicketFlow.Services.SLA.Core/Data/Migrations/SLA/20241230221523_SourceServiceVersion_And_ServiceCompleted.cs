using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.SLA.Core.Data.Migrations.SLA
{
    /// <inheritdoc />
    public partial class SourceServiceVersion_And_ServiceCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ServiceCompleted",
                schema: "sla",
                table: "DeadlineReminders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ServiceLastKnownVersion",
                schema: "sla",
                table: "DeadlineReminders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceCompleted",
                schema: "sla",
                table: "DeadlineReminders");

            migrationBuilder.DropColumn(
                name: "ServiceLastKnownVersion",
                schema: "sla",
                table: "DeadlineReminders");
        }
    }
}
