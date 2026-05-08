using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDgiiApprovedToSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDgiiApproved",
                table: "BusinessSimulationSample",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 6,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 7,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 8,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 9,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 10,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 11,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 12,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 13,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 14,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 15,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 16,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 17,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 18,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 19,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 20,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 21,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 22,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 23,
                column: "IsDgiiApproved",
                value: false);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 24,
                column: "IsDgiiApproved",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDgiiApproved",
                table: "BusinessSimulationSample");
        }
    }
}
