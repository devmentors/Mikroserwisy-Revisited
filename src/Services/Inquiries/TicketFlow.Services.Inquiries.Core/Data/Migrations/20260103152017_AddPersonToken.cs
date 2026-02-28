using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Inquiries.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Inquiries");

            migrationBuilder.AddColumn<string>(
                name: "PersonToken",
                table: "Inquiries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_PersonToken",
                table: "Inquiries",
                column: "PersonToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inquiries_PersonToken",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "PersonToken",
                table: "Inquiries");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Inquiries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Inquiries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
