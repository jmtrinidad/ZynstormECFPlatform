using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSimulationSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "d0bd3362-54a8-42ed-bf2b-cd0fb0c39db0", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Pinturas del Este\",\"IssuerAddress\":\"Av. Independencia 456\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cubeta Pintura Blanca Satinada\",\"Quantity\":1,\"UnitPrice\":2000.00,\"TaxPercentage\":18},{\"Name\":\"Brocha 4 Pulgadas Profesional\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "5ca120a5-253b-4b6a-bdbd-12fade5ec2bb", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Boutique Elegance\",\"IssuerAddress\":\"Calle del Sol 789\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Vestido de Gala Azul\",\"Quantity\":1,\"UnitPrice\":3500.00,\"TaxPercentage\":18},{\"Name\":\"Cinturón Cuero Genuino\",\"Quantity\":1,\"UnitPrice\":1000.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "1d68f593-3b2b-480e-8c2a-b3f729a4bf14", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Colchones Confort\",\"IssuerAddress\":\"Av. Winston Churchill 101\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Colchón King Size Ortopédico\",\"Quantity\":1,\"UnitPrice\":12000.00,\"TaxPercentage\":18},{\"Name\":\"Almohada Memory Foam\",\"Quantity\":2,\"UnitPrice\":1500.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "f23cbe89-2c93-45af-872d-7fdb029faba8", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Restaurante Sabores\",\"IssuerAddress\":\"Calle Gourmet 202\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cena Especial del Chef (Dúo)\",\"Quantity\":1,\"UnitPrice\":2500.00,\"TaxPercentage\":18},{\"Name\":\"Botella de Vino Tinto Reserva\",\"Quantity\":1,\"UnitPrice\":700.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "84043e4c-644a-441c-9d8f-c50690d53ba7", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Café Aroma\",\"IssuerAddress\":\"Plaza Central Local 5\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Café Latte Grande\",\"Quantity\":2,\"UnitPrice\":175.00,\"TaxPercentage\":18},{\"Name\":\"Croissant de Almendras\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "7ea46492-3c59-47fe-a4ff-3933847910c2", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000001\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Pinturas del Este\",\"DireccionEmisor\":\"Av. Independencia 456\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":2500.00,\"MontoGravadoI1\":2500.00,\"ITBIS1\":18,\"TotalITBIS\":450.00,\"TotalITBIS1\":450.00,\"MontoTotal\":2950.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cubeta Pintura Blanca Satinada\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2000.00,\"MontoItem\":2000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Brocha 4 Pulgadas Profesional\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "c6d5aa3f-92e5-4878-945d-2a7a486e289e", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000002\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Boutique Elegance\",\"DireccionEmisor\":\"Calle del Sol 789\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":4500.00,\"MontoGravadoI1\":4500.00,\"ITBIS1\":18,\"TotalITBIS\":810.00,\"TotalITBIS1\":810.00,\"MontoTotal\":5310.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Vestido de Gala Azul\",\"CantidadItem\":1,\"PrecioUnitarioItem\":3500.00,\"MontoItem\":3500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Cinturón Cuero Genuino\",\"CantidadItem\":1,\"PrecioUnitarioItem\":1000.00,\"MontoItem\":1000.00}]}}}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "c9b4871d-b052-4e6d-bc0c-e3565b4aa230", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000003\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Colchones Confort\",\"DireccionEmisor\":\"Av. Winston Churchill 101\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":15000.00,\"MontoGravadoI1\":15000.00,\"ITBIS1\":18,\"TotalITBIS\":2700.00,\"TotalITBIS1\":2700.00,\"MontoTotal\":17700.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Colchón King Size Ortopédico\",\"CantidadItem\":1,\"PrecioUnitarioItem\":12000.00,\"MontoItem\":12000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Almohada Memory Foam\",\"CantidadItem\":2,\"PrecioUnitarioItem\":1500.00,\"MontoItem\":3000.00}]}}}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "2add9e11-37fe-4b02-87da-791495cc7d30", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000004\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Restaurante Sabores\",\"DireccionEmisor\":\"Calle Gourmet 202\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":3200.00,\"MontoGravadoI1\":3200.00,\"ITBIS1\":18,\"TotalITBIS\":576.00,\"TotalITBIS1\":576.00,\"MontoTotal\":3776.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cena Especial del Chef (Dúo)\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2500.00,\"MontoItem\":2500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Botella de Vino Tinto Reserva\",\"CantidadItem\":1,\"PrecioUnitarioItem\":700.00,\"MontoItem\":700.00}]}}}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                columns: new[] { "GuidId", "JsonData" },
                values: new object[] { "5b263b7e-6e3b-4a0b-8531-8acbcb181adf", "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000005\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Café Aroma\",\"DireccionEmisor\":\"Plaza Central Local 5\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":850.00,\"MontoGravadoI1\":850.00,\"ITBIS1\":18,\"TotalITBIS\":153.00,\"TotalITBIS1\":153.00,\"MontoTotal\":1003.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Café Latte Grande\",\"CantidadItem\":2,\"PrecioUnitarioItem\":175.00,\"MontoItem\":350.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Croissant de Almendras\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}" });
        }
    }
}
