using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.SLA.Core.Data.Migrations.SLA
{
    /// <inheritdoc />
    public partial class UserId_InsteadOf_AgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AgentIdToRemind",
                schema: "sla",
                table: "DeadlineReminders",
                newName: "UserIdToRemind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserIdToRemind",
                schema: "sla",
                table: "DeadlineReminders",
                newName: "AgentIdToRemind");
        }
    }
}
