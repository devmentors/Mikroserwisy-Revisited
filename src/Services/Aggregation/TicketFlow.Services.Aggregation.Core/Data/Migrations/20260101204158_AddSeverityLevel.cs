using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Aggregation.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeverityLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeverityLevel",
                schema: "aggregation",
                table: "TicketProjections",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeverityLevel",
                schema: "aggregation",
                table: "TicketProjections");
        }
    }
}
