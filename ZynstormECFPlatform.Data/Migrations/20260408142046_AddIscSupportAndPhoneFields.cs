using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIscSupportAndPhoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EcfXmlDocument");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EcfStatusHistory");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ClientCallBack");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ClientBranche");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ApiKey");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "UseClient",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "UseClient",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "UseClient",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "SystemLog",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "SystemLog",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateAtUtc",
                table: "SystemLog",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "SystemLog",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Status",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Status",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Status",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfXmlDocument",
                type: "timestamp without time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfXmlDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfXmlDocument",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfType",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfType",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfType",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAtUtc",
                table: "EcfTransmission",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfTransmission",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfTransmission",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfTransmission",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfStatusHistory",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfStatusHistory",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfStatusHistory",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfStatus",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfStatus",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfStatus",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocumentTotal",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocumentTotal",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalTaxTotal",
                table: "EcfDocumentTotal",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfDocumentTotal",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocumentDetail",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocumentDetail",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalTaxRate",
                table: "EcfDocumentDetail",
                type: "numeric(5,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IscAdvaloremAmount",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IscSpecificAmount",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IscType",
                table: "EcfDocumentDetail",
                type: "character varying(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherAdditionalTaxAmount",
                table: "EcfDocumentDetail",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfDocumentDetail",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignatureDateTime",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SequenceExpirationDate",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PurchaseOrderDate",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDeadline",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedNcfDate",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "IssueDateUtc",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerTelephone",
                table: "EcfDocument",
                type: "character varying(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerPhone",
                table: "EcfDocument",
                type: "character varying(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EcfDocument",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "DGIIUnit",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "DGIIUnit",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "DGIIUnit",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "DgiiMunicipality",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "DgiiMunicipality",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "DgiiMunicipality",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Currency",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Currency",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Currency",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientCertificate",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpirationDateUtc",
                table: "ClientCertificate",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientCertificate",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "ClientCertificate",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientCallBack",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientCallBack",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "ClientCallBack",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientBranche",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientBranche",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "ClientBranche",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "Client",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ApiKey",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ApiKey",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "ApiKey",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "EcfDocumentAdditionalTax",
                columns: table => new
                {
                    EcfDocumentAdditionalTaxId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    TaxTypeCode = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: false),
                    IscSpecificAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IscAdvaloremAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAdditionalTaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfDocumentAdditionalTax", x => x.EcfDocumentAdditionalTaxId);
                    table.ForeignKey(
                        name: "FK_EcfDocumentAdditionalTax_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                });

            migrationBuilder.CreateTable(
                name: "EcfGlobalAdjustment",
                columns: table => new
                {
                    EcfGlobalAdjustmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    AdjustmentType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ValueType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "text", nullable: false),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfGlobalAdjustment", x => x.EcfGlobalAdjustmentId);
                    table.ForeignKey(
                        name: "FK_EcfGlobalAdjustment_EcfDocument_EcfDocumentId",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 1,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 2,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 3,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 10,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 11,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 12,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 13,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 14,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 15,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 16,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 17,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 18,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 19,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 20,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 21,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 22,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 23,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 50,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 51,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 52,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 53,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 54,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 55,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 56,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 57,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 58,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 59,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 60,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 80,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 81,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 82,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 83,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 84,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 85,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 110,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 111,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 112,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 113,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 130,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 131,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 132,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 133,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 134,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 135,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 150,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 151,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 152,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 170,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 171,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 172,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 190,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 191,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 192,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 193,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 194,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 210,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 211,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 212,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 230,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 231,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 232,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 250,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 251,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 252,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 270,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 271,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 272,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 290,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 291,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 292,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 310,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 311,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 312,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 330,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 331,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 332,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 350,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 351,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 352,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 370,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 371,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 372,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 390,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 391,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 392,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 410,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 411,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 412,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 430,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 431,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 432,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 450,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 451,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 452,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 470,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 471,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 472,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 490,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 491,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 492,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 510,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 511,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 512,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 530,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 531,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 532,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 550,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 551,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 552,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 570,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 571,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 572,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 590,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 591,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 592,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 610,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 611,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 612,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 630,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 631,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 632,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 650,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 651,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "DgiiMunicipality",
                keyColumn: "DgiiMunicipalityId",
                keyValue: 652,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 1,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 2,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 3,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 4,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 5,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 6,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 7,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 8,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 9,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 10,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 11,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 12,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfStatus",
                keyColumn: "EcfStatusId",
                keyValue: 13,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 1,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 2,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 3,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 4,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 5,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 6,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 7,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 8,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 9,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "EcfType",
                keyColumn: "EcfTypeId",
                keyValue: 10,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Status",
                keyColumn: "StatusId",
                keyValue: 1,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Status",
                keyColumn: "StatusId",
                keyValue: 2,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Status",
                keyColumn: "StatusId",
                keyValue: 3,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Status",
                keyColumn: "StatusId",
                keyValue: 4,
                column: "RegisteredAt",
                value: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocumentAdditionalTax_EcfDocumentId",
                table: "EcfDocumentAdditionalTax",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfGlobalAdjustment_EcfDocumentId",
                table: "EcfGlobalAdjustment",
                column: "EcfDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcfDocumentAdditionalTax");

            migrationBuilder.DropTable(
                name: "EcfGlobalAdjustment");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "UseClient");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "SystemLog");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Status");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfXmlDocument");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfType");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfTransmission");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfStatusHistory");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfStatus");

            migrationBuilder.DropColumn(
                name: "AdditionalTaxTotal",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfDocumentTotal");

            migrationBuilder.DropColumn(
                name: "AdditionalTaxRate",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "IscAdvaloremAmount",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "IscSpecificAmount",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "IscType",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "OtherAdditionalTaxAmount",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfDocumentDetail");

            migrationBuilder.DropColumn(
                name: "CustomerTelephone",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerPhone",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "DGIIUnit");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "DgiiMunicipality");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Currency");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "ClientCallBack");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "ClientBranche");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "ApiKey");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "UseClient",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "UseClient",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "SystemLog",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "SystemLog",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateAtUtc",
                table: "SystemLog",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Status",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Status",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfXmlDocument",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfXmlDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EcfXmlDocument",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfType",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfType",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAtUtc",
                table: "EcfTransmission",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfTransmission",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfTransmission",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfStatusHistory",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfStatusHistory",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EcfStatusHistory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfStatus",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfStatus",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocumentTotal",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocumentTotal",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocumentDetail",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocumentDetail",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignatureDateTime",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SequenceExpirationDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PurchaseOrderDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDeadline",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedNcfDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "IssueDateUtc",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EcfDocument",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "DGIIUnit",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "DGIIUnit",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "DgiiMunicipality",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "DgiiMunicipality",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Currency",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Currency",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientCertificate",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpirationDateUtc",
                table: "ClientCertificate",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientCertificate",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ClientCertificate",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientCallBack",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientCallBack",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ClientCallBack",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ClientBranche",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ClientBranche",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ClientBranche",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "Client",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "Client",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Client",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdateUtc",
                table: "ApiKey",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedTimeUtc",
                table: "ApiKey",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ApiKey",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
