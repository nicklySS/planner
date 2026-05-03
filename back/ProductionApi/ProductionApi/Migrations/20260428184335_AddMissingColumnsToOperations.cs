using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                table: "Operations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationType",
                table: "Operations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReconfigurationTime",
                table: "Operations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "Operations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SetupPercentage",
                table: "Operations",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationCode",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ReconfigurationTime",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "SetupPercentage",
                table: "Operations");
        }
    }
}
