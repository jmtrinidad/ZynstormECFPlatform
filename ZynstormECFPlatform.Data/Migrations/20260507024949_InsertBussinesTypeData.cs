using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class InsertBussinesTypeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSimulationSamples_BusinessTypes_BusinessTypeId",
                table: "BusinessSimulationSamples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessTypes",
                table: "BusinessTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSimulationSamples",
                table: "BusinessSimulationSamples");

            migrationBuilder.RenameTable(
                name: "BusinessTypes",
                newName: "BusinessType");

            migrationBuilder.RenameTable(
                name: "BusinessSimulationSamples",
                newName: "BusinessSimulationSample");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessSimulationSamples_BusinessTypeId",
                table: "BusinessSimulationSample",
                newName: "IX_BusinessSimulationSample_BusinessTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessType",
                table: "BusinessType",
                column: "BusinessTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSimulationSample",
                table: "BusinessSimulationSample",
                column: "BusinessSimulationSampleId");

            migrationBuilder.InsertData(
                table: "BusinessType",
                columns: new[] { "BusinessTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 1, null, "Servicios de transporte de carga y logística.", "7a8b9c0d-1e2f-3g4h-5i6j-7k8l9m0n1o2p", null, "Transporte", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 2, null, "Venta de medicamentos y productos de salud.", "8a9b0c1d-2e3f-4g5h-6i7j-8k9l0m1n2o3p", null, "Farmacia", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 3, null, "Venta de piezas y accesorios para vehículos.", "9a0b1c2d-3e4f-5g6h-7i8j-9k0l1m2n3o4p", null, "Repuesto", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 4, null, "Servicios de mantenimiento y reparación de vehículos.", "0a1b2c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p", null, "Taller de Mecánica", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 5, null, "Venta al por mayor y detalle de productos de consumo.", "1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p", null, "Surtidora", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 6, null, "Venta de libros, útiles escolares y papelería.", "2a3b4c5d-6e7f-8g9h-0i1j-2k3l4m5n6o7p", null, "Librerías", new DateTime(2026, 5, 5, 20, 0, 0, 0, DateTimeKind.Local) }
                });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 1,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "a1b2c3d4-e5f6-4a5b-9c8d-1e2f3a4b5c6d", new DateTime(2026, 4, 30, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 2,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "b2c3d4e5-f6a1-4b6c-0d9e-2f3a4b5c6d7e", new DateTime(2026, 4, 30, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 3,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "c3d4e5f6-a1b2-4c7d-1e0f-3a4b5c6d7e8f", new DateTime(2026, 4, 30, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 4,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "d4e5f6a1-b2c3-4d8e-2f1a-4b5c6d7e8f9a", new DateTime(2026, 4, 30, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSimulationSample_BusinessType_BusinessTypeId",
                table: "BusinessSimulationSample",
                column: "BusinessTypeId",
                principalTable: "BusinessType",
                principalColumn: "BusinessTypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSimulationSample_BusinessType_BusinessTypeId",
                table: "BusinessSimulationSample");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessType",
                table: "BusinessType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSimulationSample",
                table: "BusinessSimulationSample");

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 6);

            migrationBuilder.RenameTable(
                name: "BusinessType",
                newName: "BusinessTypes");

            migrationBuilder.RenameTable(
                name: "BusinessSimulationSample",
                newName: "BusinessSimulationSamples");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessSimulationSample_BusinessTypeId",
                table: "BusinessSimulationSamples",
                newName: "IX_BusinessSimulationSamples_BusinessTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessTypes",
                table: "BusinessTypes",
                column: "BusinessTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSimulationSamples",
                table: "BusinessSimulationSamples",
                column: "BusinessSimulationSampleId");

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 1,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "8353c320-168a-4d3e-b46b-64048f47aa06", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(3530) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 2,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "6a6402d7-72e0-4e0c-99d5-15ad06b31c46", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7478) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 3,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "b5e7ece5-a1e0-4898-8031-6d1fde57089d", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7504) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 4,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "967a7834-7772-483f-a28e-afcc2403f6c5", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7508) });

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSimulationSamples_BusinessTypes_BusinessTypeId",
                table: "BusinessSimulationSamples",
                column: "BusinessTypeId",
                principalTable: "BusinessTypes",
                principalColumn: "BusinessTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
