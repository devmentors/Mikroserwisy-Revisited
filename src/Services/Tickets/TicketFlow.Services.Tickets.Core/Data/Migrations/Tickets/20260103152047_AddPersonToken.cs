using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Tickets.Core.Data.Migrations.Tickets
{
    /// <inheritdoc />
    public partial class AddPersonToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "PersonToken",
                schema: "tickets",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PersonToken",
                schema: "tickets",
                table: "Tickets",
                column: "PersonToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_PersonToken",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PersonToken",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "tickets",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "tickets",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
