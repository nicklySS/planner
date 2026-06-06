using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthRolesAndPersonUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "People");

            migrationBuilder.AddColumn<int>(
                name: "DetailID",
                table: "ShiftWorkLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaterialID",
                table: "ShiftWorkLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ShiftWorkLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkerID",
                table: "ShiftWorkLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "People",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "People",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "People",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkPlaceID",
                table: "People",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaterialSizeID",
                table: "Operations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DetailOperations",
                columns: table => new
                {
                    DetailOperationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DetailID = table.Column<int>(type: "int", nullable: false),
                    EquipmentID = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    OperationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReconfigurationTime = table.Column<int>(type: "int", nullable: true),
                    SetupPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailOperations", x => x.DetailOperationID);
                    table.ForeignKey(
                        name: "FK_DetailOperations_Details_DetailID",
                        column: x => x.DetailID,
                        principalTable: "Details",
                        principalColumn: "DetailID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetailOperations_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoles",
                columns: table => new
                {
                    PersonRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoles", x => x.PersonRoleID);
                    table.ForeignKey(
                        name: "FK_PersonRoles_People_PersonID",
                        column: x => x.PersonID,
                        principalTable: "People",
                        principalColumn: "PersonID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonRoles_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogs_DetailID",
                table: "ShiftWorkLogs",
                column: "DetailID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogs_MaterialID",
                table: "ShiftWorkLogs",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogs_WorkerID",
                table: "ShiftWorkLogs",
                column: "WorkerID");

            migrationBuilder.CreateIndex(
                name: "IX_People_WorkPlaceID",
                table: "People",
                column: "WorkPlaceID",
                unique: true,
                filter: "[WorkPlaceID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_MaterialSizeID",
                table: "Operations",
                column: "MaterialSizeID");

            migrationBuilder.CreateIndex(
                name: "IX_DetailOperations_DetailID",
                table: "DetailOperations",
                column: "DetailID");

            migrationBuilder.CreateIndex(
                name: "IX_DetailOperations_EquipmentID",
                table: "DetailOperations",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoles_PersonID",
                table: "PersonRoles",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoles_RoleID",
                table: "PersonRoles",
                column: "RoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_MaterialSizes_MaterialSizeID",
                table: "Operations",
                column: "MaterialSizeID",
                principalTable: "MaterialSizes",
                principalColumn: "MaterialSizeID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_People_WorkPlaces_WorkPlaceID",
                table: "People",
                column: "WorkPlaceID",
                principalTable: "WorkPlaces",
                principalColumn: "WorkPlaceID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftWorkLogs_Details_DetailID",
                table: "ShiftWorkLogs",
                column: "DetailID",
                principalTable: "Details",
                principalColumn: "DetailID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftWorkLogs_Materials_MaterialID",
                table: "ShiftWorkLogs",
                column: "MaterialID",
                principalTable: "Materials",
                principalColumn: "MaterialID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftWorkLogs_People_WorkerID",
                table: "ShiftWorkLogs",
                column: "WorkerID",
                principalTable: "People",
                principalColumn: "PersonID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operations_MaterialSizes_MaterialSizeID",
                table: "Operations");

            migrationBuilder.DropForeignKey(
                name: "FK_People_WorkPlaces_WorkPlaceID",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftWorkLogs_Details_DetailID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftWorkLogs_Materials_MaterialID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftWorkLogs_People_WorkerID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropTable(
                name: "DetailOperations");

            migrationBuilder.DropTable(
                name: "PersonRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_ShiftWorkLogs_DetailID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_ShiftWorkLogs_MaterialID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_ShiftWorkLogs_WorkerID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_People_WorkPlaceID",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Operations_MaterialSizeID",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "DetailID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropColumn(
                name: "MaterialID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ShiftWorkLogs");

            migrationBuilder.DropColumn(
                name: "WorkerID",
                table: "ShiftWorkLogs");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "People");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "People");

            migrationBuilder.DropColumn(
                name: "WorkPlaceID",
                table: "People");

            migrationBuilder.DropColumn(
                name: "MaterialSizeID",
                table: "Operations");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "People",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "People",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
