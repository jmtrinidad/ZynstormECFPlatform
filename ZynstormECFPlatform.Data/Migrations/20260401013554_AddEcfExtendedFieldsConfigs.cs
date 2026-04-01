using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEcfExtendedFieldsConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount1",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount2",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount3",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxRate1",
                table: "EcfDocumentTotal",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxRate2",
                table: "EcfDocumentTotal",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxRate3",
                table: "EcfDocumentTotal",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmountG1",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmountG2",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmountG3",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWithheldIsr",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWithheldItbis",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingIndicator",
                table: "EcfDocumentDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ItemAmount",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "EcfDocumentDetail",
                type: "character varying(80)",
                unicode: false,
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "EcfDocumentDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitOfMeasure",
                table: "EcfDocumentDetail",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingIsr",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingItbis",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalPhone",
                table: "EcfDocument",
                type: "character varying(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllInclusiveServiceIndicator",
                table: "EcfDocument",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerContact",
                table: "EcfDocument",
                type: "character varying(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerMunicipality",
                table: "EcfDocument",
                type: "character varying(6)",
                unicode: false,
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerProvince",
                table: "EcfDocument",
                type: "character varying(6)",
                unicode: false,
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeferredSendIndicator",
                table: "EcfDocument",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "EcfDocument",
                type: "character varying(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryContact",
                table: "EcfDocument",
                type: "character varying(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncomeType",
                table: "EcfDocument",
                type: "character varying(2)",
                unicode: false,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerActivityCode",
                table: "EcfDocument",
                type: "character varying(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerAdditionalInfo",
                table: "EcfDocument",
                type: "character varying(150)",
                unicode: false,
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerBranchCode",
                table: "EcfDocument",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerCommercialName",
                table: "EcfDocument",
                type: "character varying(150)",
                unicode: false,
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerMunicipality",
                table: "EcfDocument",
                type: "character varying(6)",
                unicode: false,
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerProvince",
                table: "EcfDocument",
                type: "character varying(6)",
                unicode: false,
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerSellerCode",
                table: "EcfDocument",
                type: "character varying(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerWebSite",
                table: "EcfDocument",
                type: "character varying(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificationCode",
                table: "EcfDocument",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModificationReason",
                table: "EcfDocument",
                type: "character varying(90)",
                unicode: false,
                maxLength: 90,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedNcf",
                table: "EcfDocument",
                type: "character varying(19)",
                unicode: false,
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedNcfDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "EcfDocument",
                type: "character varying(15)",
                unicode: false,
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "EcfDocument",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseOrderDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderNumber",
                table: "EcfDocument",
                type: "character varying(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SequenceExpirationDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SignatureDateTime",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxIncludedIndicator",
                table: "EcfDocument",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "EcfDocument",
                type: "character varying(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "1.0");

            migrationBuilder.CreateTable(
                name: "DgiiMunicipality",
                columns: table => new
                {
                    DgiiMunicipalityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character(6)", unicode: false, fixedLength: true, maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", unicode: false, maxLength: 150, nullable: false),
                    IsProvince = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DgiiMunicipality", x => x.DgiiMunicipalityId);
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 1, "010000", null, true, null, "DISTRITO NACIONAL" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 2, "010100", null, null, "MUNICIPIO SANTO DOMINGO DE GUZMÁN" },
                    { 3, "010101", null, null, "SANTO DOMINGO DE GUZMÁN (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 10, "020000", null, true, null, "PROVINCIA AZUA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 11, "020100", null, null, "MUNICIPIO AZUA" },
                    { 12, "020101", null, null, "AZUA (D. M.)." },
                    { 13, "020102", null, null, "BARRO ARRIBA (D. M.)." },
                    { 14, "020103", null, null, "LAS BARÍAS-LA ESTANCIA (D. M.)." },
                    { 15, "020104", null, null, "LOS JOVILLOS (D. M.)." },
                    { 16, "020105", null, null, "PUERTO VIEJO (D. M.)." },
                    { 17, "020106", null, null, "BARRERAS (D. M.)." },
                    { 18, "020107", null, null, "DOÑA EMMA BALAGUER VIUDA VALLEJO (D. M.)." },
                    { 19, "020108", null, null, "CLAVELLINA (D. M.)." },
                    { 20, "020109", null, null, "LAS LOMAS (D. M.)." },
                    { 21, "020200", null, null, "MUNICIPIO LAS CHARCAS" },
                    { 22, "020201", null, null, "LAS CHARCAS (D. M.)." },
                    { 23, "020202", null, null, "PALMAR DE OCOA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 50, "030000", null, true, null, "PROVINCIA BAHORUCO" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 51, "030001", null, null, "MUNICIPIO NEIBA" },
                    { 52, "030101", null, null, "NEIBA (D. M.)." },
                    { 53, "030102", null, null, "EL PALMAR  (D. M.)." },
                    { 54, "030200", null, null, "MUNICIPIO GALVÁN" },
                    { 55, "030201", null, null, "GALVÁN (D. M.)." },
                    { 56, "030202", null, null, "EL SALADO (D. M.)." },
                    { 57, "030300", null, null, "MUNICIPIO TAMAYO" },
                    { 58, "030301", null, null, "TAMAYO (D. M.)." },
                    { 59, "030302", null, null, "UVILLA (D. M.)." },
                    { 60, "030303", null, null, "SANTANA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 80, "040000", null, true, null, "PROVINCIA BARAHONA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 81, "040100", null, null, "MUNICIPIO BARAHONA" },
                    { 82, "040101", null, null, "BARAHONA (D. M.)." },
                    { 83, "040102", null, null, "EL CACHÓN (D. M.)." },
                    { 84, "040103", null, null, "LA GUÁZARA (D. M.)." },
                    { 85, "040104", null, null, "VILLA CENTRAL (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 110, "050000", null, true, null, "PROVINCIA DAJABÓN" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 111, "050100", null, null, "MUNICIPIO DAJABÓN" },
                    { 112, "050101", null, null, "DAJABÓN (D. M.)." },
                    { 113, "050102", null, null, "CAÑONGO (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 130, "060000", null, true, null, "PROVINCIA DUARTE" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 131, "060100", null, null, "MUNICIPIO SAN FRANCISCO DE MACORÍS" },
                    { 132, "060101", null, null, "SAN FRANCISCO DE MACORÍS (D. M.)." },
                    { 133, "060102", null, null, "LA PEÑA (D. M.)." },
                    { 134, "060103", null, null, "CENOVÍ (D. M.)." },
                    { 135, "060104", null, null, "JAYA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 150, "070000", null, true, null, "PROVINCIA ELÍAS PIÑA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 151, "070100", null, null, "MUNICIPIO COMENDADOR" },
                    { 152, "070101", null, null, "COMENDADOR (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 170, "080000", null, true, null, "PROVINCIA EL SEIBO" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 171, "080100", null, null, "MUNICIPIO EL SEIBO" },
                    { 172, "080101", null, null, "EL SEIBO (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 190, "090000", null, true, null, "PROVINCIA ESPAILLAT" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 191, "090100", null, null, "MUNICIPIO MOCA" },
                    { 192, "090101", null, null, "MOCA (D. M.)." },
                    { 193, "090200", null, null, "MUNICIPIO CAYETANO GERMOSÉN" },
                    { 194, "090201", null, null, "CAYETANO GERMOSÉN (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 210, "100000", null, true, null, "PROVINCIA INDEPENDENCIA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 211, "100100", null, null, "MUNICIPIO JIMANÍ" },
                    { 212, "100101", null, null, "JIMANÍ (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 230, "110000", null, true, null, "PROVINCIA LA ALTAGRACIA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 231, "110100", null, null, "MUNICIPIO HIGÜEY" },
                    { 232, "110101", null, null, "HIGÜEY (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 250, "120000", null, true, null, "PROVINCIA LA ROMANA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 251, "120100", null, null, "MUNICIPIO LA ROMANA" },
                    { 252, "120101", null, null, "LA ROMANA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 270, "130000", null, true, null, "PROVINCIA LA VEGA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 271, "130100", null, null, "MUNICIPIO LA VEGA" },
                    { 272, "130101", null, null, "LA VEGA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 290, "140000", null, true, null, "PROVINCIA MARÍA TRINIDAD SÁNCHEZ" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 291, "140100", null, null, "MUNICIPIO NAGUA" },
                    { 292, "140101", null, null, "NAGUA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 310, "150000", null, true, null, "PROVINCIA MONTE CRISTI" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 311, "150100", null, null, "MUNICIPIO MONTE CRISTI" },
                    { 312, "150101", null, null, "MONTE CRISTI (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 330, "160000", null, true, null, "PROVINCIA PEDERNALES" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 331, "160100", null, null, "MUNICIPIO PEDERNALES" },
                    { 332, "160101", null, null, "PEDERNALES" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 350, "170000", null, true, null, "PROVINCIA PERAVIA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 351, "170100", null, null, "MUNICIPIO BANÍ" },
                    { 352, "170101", null, null, "BANÍ (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 370, "180000", null, true, null, "PROVINCIA PUERTO PLATA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 371, "180100", null, null, "MUNICIPIO PUERTO PLATA" },
                    { 372, "180101", null, null, "PUERTO PLATA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 390, "190000", null, true, null, "PROVINCIA HERMANAS MIRABAL" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 391, "190100", null, null, "MUNICIPIO SALCEDO" },
                    { 392, "190101", null, null, "SALCEDO" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 410, "200000", null, true, null, "PROVINCIA SAMANÁ" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 411, "200100", null, null, "MUNICIPIO SAMANÁ" },
                    { 412, "200101", null, null, "SAMANÁ" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 430, "210000", null, true, null, "PROVINCIA SAN CRISTÓBAL" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 431, "210100", null, null, "MUNICIPIO SAN CRISTÓBAL" },
                    { 432, "210101", null, null, "SAN CRISTÓBAL (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 450, "220000", null, true, null, "PROVINCIA SAN JUAN" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 451, "220100", null, null, "MUNICIPIO SAN JUAN" },
                    { 452, "220101", null, null, "SAN JUAN" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 470, "230000", null, true, null, "PROVINCIA SAN PEDRO DE MACORÍS" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 471, "230100", null, null, "MUNICIPIO SAN PEDRO DE MACORÍS" },
                    { 472, "230101", null, null, "SAN PEDRO DE MACORÍS" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 490, "240000", null, true, null, "PROVINCIA SANCHEZ RAMÍREZ" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 491, "240100", null, null, "MUNICIPIO COTUÍ" },
                    { 492, "240101", null, null, "COTUÍ" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 510, "250000", null, true, null, "PROVINCIA SANTIAGO" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 511, "250100", null, null, "MUNICIPIO SANTIAGO" },
                    { 512, "250101", null, null, "SANTIAGO" }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 530, "260000", null, true, null, "PROVINCIA SANTIAGO RODRÍGUEZ" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 531, "260100", null, null, "MUNICIPIO SAN IGNACIO DE SABANETA" },
                    { 532, "260101", null, null, "SAN IGNACIO DE SABANETA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 550, "270000", null, true, null, "PROVINCIA VALVERDE" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 551, "270100", null, null, "MUNICIPIO MAO" },
                    { 552, "270101", null, null, "MAO (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 570, "280000", null, true, null, "PROVINCIA MONSEÑOR NOUEL" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 571, "280100", null, null, "MUNICIPIO BONAO" },
                    { 572, "280101", null, null, "BONAO (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 590, "290000", null, true, null, "PROVINCIA MONTE PLATA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 591, "290100", null, null, "MUNICIPIO MONTE PLATA" },
                    { 592, "290101", null, null, "MONTE PLATA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 610, "300000", null, true, null, "PROVINCIA HATO MAYOR" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 611, "300100", null, null, "MUNICIPIO HATO MAYOR" },
                    { 612, "300101", null, null, "HATO MAYOR (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 630, "310000", null, true, null, "PROVINCIA SAN JOSÉ DE OCOA" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 631, "310100", null, null, "MUNICIPIO SAN JOSÉ DE OCOA" },
                    { 632, "310101", null, null, "SAN JOSÉ DE OCOA (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "IsProvince", "LastUpdateUtc", "Name" },
                values: new object[] { 650, "320000", null, true, null, "PROVINCIA SANTO DOMINGO" });

            migrationBuilder.InsertData(
                table: "DgiiMunicipality",
                columns: new[] { "DgiiMunicipalityId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 651, "320100", null, null, "MUNICIPIO SANTO DOMINGO ESTE" },
                    { 652, "320101", null, null, "SANTO DOMINGO ESTE (D. M.)." }
                });

            migrationBuilder.InsertData(
                table: "EcfType",
                columns: new[] { "EcfTypeId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 1, "31", null, null, "Factura de Crédito Fiscal Electrónica" },
                    { 2, "32", null, null, "Factura de Consumo Electrónica" },
                    { 3, "33", null, null, "Nota de Débito Electrónica" },
                    { 4, "34", null, null, "Nota de Crédito Electrónica" },
                    { 5, "41", null, null, "Compras Electrónico" },
                    { 6, "43", null, null, "Gastos Menores Electrónico" },
                    { 7, "44", null, null, "Regímenes Especiales Electrónico" },
                    { 8, "45", null, null, "Gubernamental Electrónico" },
                    { 9, "46", null, null, "Comprobante de Exportaciones Electrónico" },
                    { 10, "47", null, null, "Comprobante para Pagos al Exterior Electrónico" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DgiiMunicipality");

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "TaxAmount1",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxAmount2",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxAmount3",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxRate1",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxRate2",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxRate3",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxableAmountG1",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxableAmountG2",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TaxableAmountG3",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TotalWithheldIsr",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "TotalWithheldItbis",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "BillingIndicator",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "ItemAmount",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "WithholdingIsr",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "WithholdingItbis",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "AdditionalPhone",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "AllInclusiveServiceIndicator",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "CustomerContact",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "CustomerMunicipality",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "CustomerProvince",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "DeferredSendIndicator",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "DeliveryContact",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IncomeType",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerActivityCode",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerAdditionalInfo",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerBranchCode",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerCommercialName",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerMunicipality",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerProvince",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerSellerCode",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerWebSite",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "ModificationCode",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "ModificationReason",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "ModifiedNcf",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "ModifiedNcfDate",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderDate",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderNumber",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "SequenceExpirationDate",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "SignatureDateTime",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "TaxIncludedIndicator",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "EcfDocument");
        }
    }
}
