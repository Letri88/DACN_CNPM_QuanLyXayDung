using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierToMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Materials",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_SupplierId",
                table: "Materials",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Suppliers_SupplierId",
                table: "Materials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Suppliers_SupplierId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_SupplierId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Materials");
        }
    }
}
