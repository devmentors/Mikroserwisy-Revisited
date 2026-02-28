using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.SLA.Core.Data.Migrations.SLA
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sla");

            migrationBuilder.CreateTable(
                name: "DeadlineReminders",
                schema: "sla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<byte>(type: "smallint", nullable: false),
                    ServiceSourceId = table.Column<string>(type: "text", nullable: false),
                    AgentIdToRemind = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstReminderDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstReminderSent = table.Column<bool>(type: "boolean", nullable: false),
                    SecondReminderDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SecondReminderSent = table.Column<bool>(type: "boolean", nullable: false),
                    FinalReminderDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalReminderSent = table.Column<bool>(type: "boolean", nullable: false),
                    DeadlineMet = table.Column<bool>(type: "boolean", nullable: true),
                    DeadlineDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastDeadlineBreachedAlertSentDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineReminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignedSLAs",
                schema: "sla",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    ClientTier = table.Column<int>(type: "integer", nullable: false),
                    _agreedResponseDeadlines = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignedSLAs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadlineReminders",
                schema: "sla");

            migrationBuilder.DropTable(
                name: "SignedSLAs",
                schema: "sla");
        }
    }
}
