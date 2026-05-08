using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewBuissinesType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessType",
                columns: new[] { "BusinessTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 7, null, "Venta de pinturas, barnices y accesorios.", "3a4b5c6d-7e8f-9g0h-1i2j-3k4l5m6n7o8p", null, "Tienda de pintura", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 8, null, "Venta de ropa, calzado y accesorios de moda.", "4a5b6c7d-8e9f-0g1h-2i3j-4k5l6m7n8o9p", null, "Boutique", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 9, null, "Venta de colchones, almohadas y artículos de descanso.", "5a6b7c8d-9e0f-1g2h-3i4j-5k6l7m8n9o0p", null, "Colchoneria", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 10, null, "Servicios de comida y bebidas preparadas.", "6a7b8c9d-0e1f-2g3h-4i5j-6k7l8m9n0o1p", null, "Restaurante", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 11, null, "Venta de café, postres y comidas ligeras.", "7a8b9c0d-1e2f-3g4h-5i6j-7k8l9m0n1o2p", null, "Cafeteria", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "EcfType", "GuidId", "JsonData", "LastUpdateUtc", "RegisteredAt" },
                values: new object[,]
                {
                    { 1, 7, null, "32", "7ea46492-3c59-47fe-a4ff-3933847910c2", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000001\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Pinturas del Este\",\"DireccionEmisor\":\"Av. Independencia 456\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":2500.00,\"MontoGravadoI1\":2500.00,\"ITBIS1\":18,\"TotalITBIS\":450.00,\"TotalITBIS1\":450.00,\"MontoTotal\":2950.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cubeta Pintura Blanca Satinada\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2000.00,\"MontoItem\":2000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Brocha 4 Pulgadas Profesional\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 2, 8, null, "32", "c6d5aa3f-92e5-4878-945d-2a7a486e289e", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000002\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Boutique Elegance\",\"DireccionEmisor\":\"Calle del Sol 789\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":4500.00,\"MontoGravadoI1\":4500.00,\"ITBIS1\":18,\"TotalITBIS\":810.00,\"TotalITBIS1\":810.00,\"MontoTotal\":5310.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Vestido de Gala Azul\",\"CantidadItem\":1,\"PrecioUnitarioItem\":3500.00,\"MontoItem\":3500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Cinturón Cuero Genuino\",\"CantidadItem\":1,\"PrecioUnitarioItem\":1000.00,\"MontoItem\":1000.00}]}}}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 3, 9, null, "32", "c9b4871d-b052-4e6d-bc0c-e3565b4aa230", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000003\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Colchones Confort\",\"DireccionEmisor\":\"Av. Winston Churchill 101\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":15000.00,\"MontoGravadoI1\":15000.00,\"ITBIS1\":18,\"TotalITBIS\":2700.00,\"TotalITBIS1\":2700.00,\"MontoTotal\":17700.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Colchón King Size Ortopédico\",\"CantidadItem\":1,\"PrecioUnitarioItem\":12000.00,\"MontoItem\":12000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Almohada Memory Foam\",\"CantidadItem\":2,\"PrecioUnitarioItem\":1500.00,\"MontoItem\":3000.00}]}}}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 4, 10, null, "32", "2add9e11-37fe-4b02-87da-791495cc7d30", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000004\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Restaurante Sabores\",\"DireccionEmisor\":\"Calle Gourmet 202\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":3200.00,\"MontoGravadoI1\":3200.00,\"ITBIS1\":18,\"TotalITBIS\":576.00,\"TotalITBIS1\":576.00,\"MontoTotal\":3776.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cena Especial del Chef (Dúo)\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2500.00,\"MontoItem\":2500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Botella de Vino Tinto Reserva\",\"CantidadItem\":1,\"PrecioUnitarioItem\":700.00,\"MontoItem\":700.00}]}}}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 5, 11, null, "32", "5b263b7e-6e3b-4a0b-8531-8acbcb181adf", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000005\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Café Aroma\",\"DireccionEmisor\":\"Plaza Central Local 5\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":850.00,\"MontoGravadoI1\":850.00,\"ITBIS1\":18,\"TotalITBIS\":153.00,\"TotalITBIS1\":153.00,\"MontoTotal\":1003.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Café Latte Grande\",\"CantidadItem\":2,\"PrecioUnitarioItem\":175.00,\"MontoItem\":350.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Croissant de Almendras\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 11);
        }
    }
}
