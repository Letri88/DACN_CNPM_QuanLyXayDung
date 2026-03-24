using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddStageToInventoryTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StageID",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StageID",
                table: "InventoryTransactions",
                column: "StageID");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Stage",
                table: "InventoryTransactions",
                column: "StageID",
                principalTable: "Stages",
                principalColumn: "StageID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Stage",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_StageID",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "StageID",
                table: "InventoryTransactions");
        }
    }
}
