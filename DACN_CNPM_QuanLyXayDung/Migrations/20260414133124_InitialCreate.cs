using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN_CNPM_QuanLyXayDung.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MinStockLevel = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Material__C506131779539DC7", x => x.MaterialID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE3AB04EA8A4", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC6598EAF8", x => x.UserID);
                    table.ForeignKey(
                        name: "FK__Users__RoleID__571DF1D5",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RelatedUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagerID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BudgetLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ContractFileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ContractFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContractFileContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Planned")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Projects__761ABED0C1B18240", x => x.ProjectID);
                    table.ForeignKey(
                        name: "FK_Projects_Manager",
                        column: x => x.ManagerID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "MaterialUsage",
                columns: table => new
                {
                    UsageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    MaterialID = table.Column<int>(type: "int", nullable: false),
                    QuantityUsage = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Material__29B197C0B1BF7FD0", x => x.UsageID);
                    table.ForeignKey(
                        name: "FK_Usage_Material",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID");
                    table.ForeignKey(
                        name: "FK_Usage_Project",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                });

            migrationBuilder.CreateTable(
                name: "Stages",
                columns: table => new
                {
                    StageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignedUserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Planned"),
                    Budget = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    BudgetLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaterialDeclarationFileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MaterialDeclarationFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaterialDeclarationContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaterialDeclarationUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Stages__03EB7AF8309468FE", x => x.StageID);
                    table.ForeignKey(
                        name: "FK_Stages_Project",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                    table.ForeignKey(
                        name: "FK_Stages_User",
                        column: x => x.AssignedUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialID = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: true),
                    WarehouseKeeperID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    StageID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__55433A4B41C394E1", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_Inventory_Material",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID");
                    table.ForeignKey(
                        name: "FK_Inventory_Project",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                    table.ForeignKey(
                        name: "FK_Inventory_Stage",
                        column: x => x.StageID,
                        principalTable: "Stages",
                        principalColumn: "StageID");
                    table.ForeignKey(
                        name: "FK_Inventory_User",
                        column: x => x.WarehouseKeeperID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: true),
                    StageID = table.Column<int>(type: "int", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Pending"),
                    WorkerCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tasks__7C6949D13E927275", x => x.TaskID);
                    table.ForeignKey(
                        name: "FK_Tasks_Project",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                    table.ForeignKey(
                        name: "FK_Tasks_Stage",
                        column: x => x.StageID,
                        principalTable: "Stages",
                        principalColumn: "StageID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_MaterialID",
                table: "InventoryTransactions",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProjectID",
                table: "InventoryTransactions",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StageID",
                table: "InventoryTransactions",
                column: "StageID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseKeeperID",
                table: "InventoryTransactions",
                column: "WarehouseKeeperID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsage_MaterialID",
                table: "MaterialUsage",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsage_ProjectID",
                table: "MaterialUsage",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ManagerID",
                table: "Projects",
                column: "ManagerID");

            migrationBuilder.CreateIndex(
                name: "IX_Stages_AssignedUserID",
                table: "Stages",
                column: "AssignedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Stages_ProjectID",
                table: "Stages",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectID",
                table: "Tasks",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StageID",
                table: "Tasks",
                column: "StageID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534D8332783",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "MaterialUsage");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Stages");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
