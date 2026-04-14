using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectAndStageFileStorageAndBudgetLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaterialDeclarationContentType",
                table: "Stages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "MaterialDeclarationFileContent",
                table: "Stages",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialDeclarationFileName",
                table: "Stages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MaterialDeclarationUploadedAt",
                table: "Stages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BudgetLocked",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "ContractFileContent",
                table: "Projects",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractFileContentType",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractFileName",
                table: "Projects",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractUploadedAt",
                table: "Projects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaterialDeclarationContentType",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "MaterialDeclarationFileContent",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "MaterialDeclarationFileName",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "MaterialDeclarationUploadedAt",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "BudgetLocked",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ContractFileContent",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ContractFileContentType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ContractFileName",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ContractUploadedAt",
                table: "Projects");
        }
    }
}
