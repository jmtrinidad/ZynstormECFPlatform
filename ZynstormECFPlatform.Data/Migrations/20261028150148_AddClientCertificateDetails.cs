using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCertificateDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuerName",
                table: "ClientCertificate",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotBeforeUtc",
                table: "ClientCertificate",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationIdentifier",
                table: "ClientCertificate",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "ClientCertificate",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "ClientCertificate",
                type: "character varying(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectAddress",
                table: "ClientCertificate",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectEmail",
                table: "ClientCertificate",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectIdentification",
                table: "ClientCertificate",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectName",
                table: "ClientCertificate",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuerName",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "NotBeforeUtc",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "OrganizationIdentifier",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "SubjectAddress",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "SubjectEmail",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "SubjectIdentification",
                table: "ClientCertificate");

            migrationBuilder.DropColumn(
                name: "SubjectName",
                table: "ClientCertificate");
        }
    }
}
