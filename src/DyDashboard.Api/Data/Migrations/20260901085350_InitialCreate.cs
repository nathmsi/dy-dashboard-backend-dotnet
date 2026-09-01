using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DyDashboard.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "campaigns",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                name = table.Column<string>(type: "TEXT", nullable: false),
                status = table.Column<string>(type: "TEXT", nullable: false),
                channel = table.Column<string>(type: "TEXT", nullable: false),
                conversionRate = table.Column<double>(type: "REAL", nullable: false),
                visitors = table.Column<int>(type: "INTEGER", nullable: false),
                startDate = table.Column<string>(type: "TEXT", nullable: false),
                createdAt = table.Column<string>(type: "TEXT", nullable: false),
                updatedAt = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaigns", x => x.id);
                table.CheckConstraint("ck_campaigns_status", "status IN ('active', 'paused', 'ended')");
            });

        migrationBuilder.CreateIndex(
            name: "idx_campaigns_startDate",
            table: "campaigns",
            column: "startDate");

        migrationBuilder.CreateIndex(
            name: "idx_campaigns_status",
            table: "campaigns",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "campaigns");
    }
}
