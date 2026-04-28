using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsumptionRate",
                table: "Details",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailCode",
                table: "Details",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailShortCode",
                table: "Details",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MainMaterial",
                table: "Details",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipmentTimeSheet",
                columns: table => new
                {
                    EquipmentTimeSheetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentID = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTimeSheet", x => x.EquipmentTimeSheetID);
                    table.ForeignKey(
                        name: "FK_EquipmentTimeSheet_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialStocks",
                columns: table => new
                {
                    MaterialStockID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialID = table.Column<int>(type: "int", nullable: false),
                    MaterialSizeID = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialStocks", x => x.MaterialStockID);
                    table.ForeignKey(
                        name: "FK_MaterialStocks_MaterialSizes_MaterialSizeID",
                        column: x => x.MaterialSizeID,
                        principalTable: "MaterialSizes",
                        principalColumn: "MaterialSizeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialStocks_Materials_MaterialID",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialID = table.Column<int>(type: "int", nullable: false),
                    MaterialSizeID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_MaterialTransactions_MaterialSizes_MaterialSizeID",
                        column: x => x.MaterialSizeID,
                        principalTable: "MaterialSizes",
                        principalColumn: "MaterialSizeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialTransactions_Materials_MaterialID",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeSheet",
                columns: table => new
                {
                    TimeSheetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonID = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSheet", x => x.TimeSheetID);
                    table.ForeignKey(
                        name: "FK_TimeSheet_People_PersonID",
                        column: x => x.PersonID,
                        principalTable: "People",
                        principalColumn: "PersonID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Details_MainMaterial",
                table: "Details",
                column: "MainMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTimeSheet_EquipmentID",
                table: "EquipmentTimeSheet",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStocks_MaterialID_MaterialSizeID",
                table: "MaterialStocks",
                columns: new[] { "MaterialID", "MaterialSizeID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialStocks_MaterialSizeID",
                table: "MaterialStocks",
                column: "MaterialSizeID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTransactions_MaterialID",
                table: "MaterialTransactions",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTransactions_MaterialSizeID",
                table: "MaterialTransactions",
                column: "MaterialSizeID");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSheet_PersonID",
                table: "TimeSheet",
                column: "PersonID");

            migrationBuilder.AddForeignKey(
                name: "FK_Details_Materials_MainMaterial",
                table: "Details",
                column: "MainMaterial",
                principalTable: "Materials",
                principalColumn: "MaterialID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Details_Materials_MainMaterial",
                table: "Details");

            migrationBuilder.DropTable(
                name: "EquipmentTimeSheet");

            migrationBuilder.DropTable(
                name: "MaterialStocks");

            migrationBuilder.DropTable(
                name: "MaterialTransactions");

            migrationBuilder.DropTable(
                name: "TimeSheet");

            migrationBuilder.DropIndex(
                name: "IX_Details_MainMaterial",
                table: "Details");

            migrationBuilder.DropColumn(
                name: "ConsumptionRate",
                table: "Details");

            migrationBuilder.DropColumn(
                name: "DetailCode",
                table: "Details");

            migrationBuilder.DropColumn(
                name: "DetailShortCode",
                table: "Details");

            migrationBuilder.DropColumn(
                name: "MainMaterial",
                table: "Details");
        }
    }
}
