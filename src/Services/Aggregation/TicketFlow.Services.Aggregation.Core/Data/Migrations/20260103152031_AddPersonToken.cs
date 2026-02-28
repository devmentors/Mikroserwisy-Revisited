using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Aggregation.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "aggregation",
                table: "TicketProjections");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "aggregation",
                table: "TicketProjections");

            migrationBuilder.AddColumn<string>(
                name: "PersonToken",
                schema: "aggregation",
                table: "TicketProjections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProjections_PersonToken",
                schema: "aggregation",
                table: "TicketProjections",
                column: "PersonToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketProjections_PersonToken",
                schema: "aggregation",
                table: "TicketProjections");

            migrationBuilder.DropColumn(
                name: "PersonToken",
                schema: "aggregation",
                table: "TicketProjections");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "aggregation",
                table: "TicketProjections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "aggregation",
                table: "TicketProjections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
