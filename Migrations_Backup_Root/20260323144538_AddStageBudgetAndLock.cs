using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddStageBudgetAndLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Budget",
                table: "Stages",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BudgetLocked",
                table: "Stages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Budget",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "BudgetLocked",
                table: "Stages");
        }
    }
}
