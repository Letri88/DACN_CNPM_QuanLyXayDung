using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectMaterialExportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectMaterialRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    RequesterId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApproverId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMaterialRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_ProjectMaterialRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMaterialRequests_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ProjectMaterialRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMaterialRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequested = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    QuantityExported = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMaterialRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMaterialRequestDetails_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "MaterialID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMaterialRequestDetails_ProjectMaterialRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ProjectMaterialRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMaterialRequestDetails_MaterialId",
                table: "ProjectMaterialRequestDetails",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMaterialRequestDetails_RequestId",
                table: "ProjectMaterialRequestDetails",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMaterialRequests_ApproverId",
                table: "ProjectMaterialRequests",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMaterialRequests_ProjectId",
                table: "ProjectMaterialRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMaterialRequests_RequesterId",
                table: "ProjectMaterialRequests",
                column: "RequesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMaterialRequestDetails");

            migrationBuilder.DropTable(
                name: "ProjectMaterialRequests");
        }
    }
}
