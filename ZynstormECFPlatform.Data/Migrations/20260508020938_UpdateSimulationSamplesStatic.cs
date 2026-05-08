using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSimulationSamplesStatic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                column: "GuidId",
                value: "98765432-1234-5678-90ab-cdef12345671");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                column: "GuidId",
                value: "98765432-1234-5678-90ab-cdef12345672");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                column: "GuidId",
                value: "98765432-1234-5678-90ab-cdef12345673");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                column: "GuidId",
                value: "98765432-1234-5678-90ab-cdef12345674");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                column: "GuidId",
                value: "98765432-1234-5678-90ab-cdef12345675");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                column: "GuidId",
                value: "d0bd3362-54a8-42ed-bf2b-cd0fb0c39db0");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                column: "GuidId",
                value: "5ca120a5-253b-4b6a-bdbd-12fade5ec2bb");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                column: "GuidId",
                value: "1d68f593-3b2b-480e-8c2a-b3f729a4bf14");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                column: "GuidId",
                value: "f23cbe89-2c93-45af-872d-7fdb029faba8");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                column: "GuidId",
                value: "84043e4c-644a-441c-9d8f-c50690d53ba7");
        }
    }
}
