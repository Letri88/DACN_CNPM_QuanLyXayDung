using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteDiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteDiaries",
                columns: table => new
                {
                    DiaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: true),
                    EngineerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Weather = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Temperature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WorkerCount = table.Column<int>(type: "int", nullable: false),
                    MachineryCount = table.Column<int>(type: "int", nullable: false),
                    WorkCompleted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PhotoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteDiaries", x => x.DiaryId);
                    table.ForeignKey(
                        name: "FK_SiteDiaries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteDiaries_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "StageID");
                    table.ForeignKey(
                        name: "FK_SiteDiaries_Users_EngineerId",
                        column: x => x.EngineerId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteDiaries_EngineerId",
                table: "SiteDiaries",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteDiaries_ProjectId",
                table: "SiteDiaries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteDiaries_StageId",
                table: "SiteDiaries",
                column: "StageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteDiaries");
        }
    }
}
