using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCertificationSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""CertificationStep"" (""CertificationStepId"", ""Name"", ""Order"", ""IsRequired"", ""IsDeleted"", ""GuidId"", ""RegisteredAt"")
                VALUES 
                (1, 'Registrado', 1, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (2, 'Pruebas de Datos e-CF', 2, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (3, 'Pruebas de Datos Aprobación Comercial', 3, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (4, 'Pruebas Simulación e-CF', 4, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (5, 'Pruebas Simulación Representación Impresa', 5, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (6, 'Validación Representación Impresa', 6, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (7, 'URL Servicios Prueba', 7, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (8, 'Inicio Prueba Recepción e-CF', 8, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (9, 'Recepción e-CF', 9, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (10, 'Inicio Prueba Recepción Aprobación Comercial', 10, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (11, 'Recepción Aprobación Comercial', 11, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (12, 'URL Servicios Producción', 12, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (13, 'Declaración Jurada', 13, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (14, 'Verificación Estatus', 14, true, false, gen_random_uuid(), CURRENT_TIMESTAMP),
                (15, 'Finalizado', 15, true, false, gen_random_uuid(), CURRENT_TIMESTAMP)
                ON CONFLICT (""CertificationStepId"") 
                DO UPDATE SET ""Name"" = EXCLUDED.""Name"", ""Order"" = EXCLUDED.""Order"";

                SELECT setval(pg_get_serial_sequence('""CertificationStep""', 'CertificationStepId'), COALESCE(MAX(""CertificationStepId""), 1)) FROM ""CertificationStep"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "CertificationStep",
                keyColumn: "CertificationStepId",
                keyValue: 15);
        }
    }
}
