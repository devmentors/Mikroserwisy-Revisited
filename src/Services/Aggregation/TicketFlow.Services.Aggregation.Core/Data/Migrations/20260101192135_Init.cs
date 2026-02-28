using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Services.Aggregation.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "aggregation");

            migrationBuilder.CreateTable(
                name: "TicketProjections",
                schema: "aggregation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InquiryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AgentAvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SlaDeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SlaBreached = table.Column<bool>(type: "boolean", nullable: true),
                    SlaServiceCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketProjections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketProjections_CreatedAt",
                schema: "aggregation",
                table: "TicketProjections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProjections_SlaBreached",
                schema: "aggregation",
                table: "TicketProjections",
                column: "SlaBreached");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProjections_Status",
                schema: "aggregation",
                table: "TicketProjections",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketProjections",
                schema: "aggregation");
        }
    }
}
