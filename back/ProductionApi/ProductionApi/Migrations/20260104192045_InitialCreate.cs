using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Details",
                columns: table => new
                {
                    DetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DetailName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Details", x => x.DetailID);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialID);
                });

            migrationBuilder.CreateTable(
                name: "MaterialSizes",
                columns: table => new
                {
                    MaterialSizeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeValue = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSizes", x => x.MaterialSizeID);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    PersonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.PersonID);
                });

            migrationBuilder.CreateTable(
                name: "WorkPlaces",
                columns: table => new
                {
                    WorkPlaceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPlaces", x => x.WorkPlaceID);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMaterialSizes",
                columns: table => new
                {
                    MaterialID = table.Column<int>(type: "int", nullable: false),
                    MaterialSizeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMaterialSizes", x => new { x.MaterialID, x.MaterialSizeID });
                    table.ForeignKey(
                        name: "FK_MaterialMaterialSizes_MaterialSizes_MaterialSizeID",
                        column: x => x.MaterialSizeID,
                        principalTable: "MaterialSizes",
                        principalColumn: "MaterialSizeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialMaterialSizes_Materials_MaterialID",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "MaterialID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftWorkLogs",
                columns: table => new
                {
                    ShiftWorkLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftNumber = table.Column<int>(type: "int", nullable: false),
                    MasterID = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PersonID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftWorkLogs", x => x.ShiftWorkLogID);
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogs_People_MasterID",
                        column: x => x.MasterID,
                        principalTable: "People",
                        principalColumn: "PersonID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogs_People_PersonID",
                        column: x => x.PersonID,
                        principalTable: "People",
                        principalColumn: "PersonID");
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EquipmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EquipmentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkPlaceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentID);
                    table.ForeignKey(
                        name: "FK_Equipment_WorkPlaces_WorkPlaceID",
                        column: x => x.WorkPlaceID,
                        principalTable: "WorkPlaces",
                        principalColumn: "WorkPlaceID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShiftWorkLogSetupPeople",
                columns: table => new
                {
                    ShiftWorkLogID = table.Column<int>(type: "int", nullable: false),
                    PersonID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftWorkLogSetupPeople", x => new { x.ShiftWorkLogID, x.PersonID });
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogSetupPeople_People_PersonID",
                        column: x => x.PersonID,
                        principalTable: "People",
                        principalColumn: "PersonID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogSetupPeople_ShiftWorkLogs_ShiftWorkLogID",
                        column: x => x.ShiftWorkLogID,
                        principalTable: "ShiftWorkLogs",
                        principalColumn: "ShiftWorkLogID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetailToDetailReconfigurationTimes",
                columns: table => new
                {
                    ReconfigurationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentID = table.Column<int>(type: "int", nullable: false),
                    FromDetailID = table.Column<int>(type: "int", nullable: false),
                    ToDetailID = table.Column<int>(type: "int", nullable: false),
                    ReconfigurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailToDetailReconfigurationTimes", x => x.ReconfigurationID);
                    table.ForeignKey(
                        name: "FK_DetailToDetailReconfigurationTimes_Details_FromDetailID",
                        column: x => x.FromDetailID,
                        principalTable: "Details",
                        principalColumn: "DetailID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetailToDetailReconfigurationTimes_Details_ToDetailID",
                        column: x => x.ToDetailID,
                        principalTable: "Details",
                        principalColumn: "DetailID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetailToDetailReconfigurationTimes_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    OperationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentID = table.Column<int>(type: "int", nullable: false),
                    DetailID = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    CompletedQuantity = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.OperationID);
                    table.ForeignKey(
                        name: "FK_Operations_Details_DetailID",
                        column: x => x.DetailID,
                        principalTable: "Details",
                        principalColumn: "DetailID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Operations_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftWorkLogEquipments",
                columns: table => new
                {
                    ShiftWorkLogID = table.Column<int>(type: "int", nullable: false),
                    EquipmentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftWorkLogEquipments", x => new { x.ShiftWorkLogID, x.EquipmentID });
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogEquipments_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftWorkLogEquipments_ShiftWorkLogs_ShiftWorkLogID",
                        column: x => x.ShiftWorkLogID,
                        principalTable: "ShiftWorkLogs",
                        principalColumn: "ShiftWorkLogID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetailToDetailReconfigurationTimes_EquipmentID",
                table: "DetailToDetailReconfigurationTimes",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_DetailToDetailReconfigurationTimes_FromDetailID",
                table: "DetailToDetailReconfigurationTimes",
                column: "FromDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_DetailToDetailReconfigurationTimes_ToDetailID",
                table: "DetailToDetailReconfigurationTimes",
                column: "ToDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_WorkPlaceID",
                table: "Equipment",
                column: "WorkPlaceID");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMaterialSizes_MaterialSizeID",
                table: "MaterialMaterialSizes",
                column: "MaterialSizeID");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_DetailID",
                table: "Operations",
                column: "DetailID");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_EquipmentID",
                table: "Operations",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogEquipments_EquipmentID",
                table: "ShiftWorkLogEquipments",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogs_MasterID",
                table: "ShiftWorkLogs",
                column: "MasterID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogs_PersonID",
                table: "ShiftWorkLogs",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftWorkLogSetupPeople_PersonID",
                table: "ShiftWorkLogSetupPeople",
                column: "PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetailToDetailReconfigurationTimes");

            migrationBuilder.DropTable(
                name: "MaterialMaterialSizes");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "ShiftWorkLogEquipments");

            migrationBuilder.DropTable(
                name: "ShiftWorkLogSetupPeople");

            migrationBuilder.DropTable(
                name: "MaterialSizes");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Details");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "ShiftWorkLogs");

            migrationBuilder.DropTable(
                name: "WorkPlaces");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
