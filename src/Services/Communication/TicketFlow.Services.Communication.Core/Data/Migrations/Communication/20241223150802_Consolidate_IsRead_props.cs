using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Communication.Core.Data.Migrations.Communication
{
    /// <inheritdoc />
    public partial class Consolidate_IsRead_props : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsUnread",
                schema: "communication",
                table: "Messages",
                newName: "IsRead");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsRead",
                schema: "communication",
                table: "Messages",
                newName: "IsUnread");
        }
    }
}
