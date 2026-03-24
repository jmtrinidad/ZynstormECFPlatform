using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "text", nullable: false),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    CurrencyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.CurrencyId);
                });

            migrationBuilder.CreateTable(
                name: "DGIIUnit",
                columns: table => new
                {
                    DGIIUnitId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DGIICode = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DGIIUnit", x => x.DGIIUnitId);
                });

            migrationBuilder.CreateTable(
                name: "EcfStatus",
                columns: table => new
                {
                    EcfStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfStatus", x => x.EcfStatusId);
                });

            migrationBuilder.CreateTable(
                name: "EcfType",
                columns: table => new
                {
                    EcfTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfType", x => x.EcfTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Status",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Status", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    RNC = table.Column<string>(type: "character varying(25)", unicode: false, maxLength: 25, nullable: false),
                    Email = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_Client_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "StatusId");
                });

            migrationBuilder.CreateTable(
                name: "ApiKey",
                columns: table => new
                {
                    ApiKeyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ClientBrancheId = table.Column<int>(type: "integer", nullable: true),
                    Apikey = table.Column<string>(type: "text", unicode: false, nullable: false),
                    SecretKey = table.Column<string>(type: "text", unicode: false, nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKey", x => x.ApiKeyId);
                    table.ForeignKey(
                        name: "FK_ApiKey_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_ApiKey_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "StatusId");
                });

            migrationBuilder.CreateTable(
                name: "ClientBranche",
                columns: table => new
                {
                    ClientBrancheId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", unicode: false, maxLength: 80, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", unicode: false, maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(30)", unicode: false, maxLength: 30, nullable: true),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientBranche", x => x.ClientBrancheId);
                    table.ForeignKey(
                        name: "FK_ClientBranche_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_ClientBranche_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "StatusId");
                });

            migrationBuilder.CreateTable(
                name: "ClientCertificate",
                columns: table => new
                {
                    ClientCertificateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Certificate = table.Column<string>(type: "text", unicode: false, nullable: false),
                    Password = table.Column<string>(type: "text", unicode: false, nullable: false),
                    Thumbprint = table.Column<string>(type: "text", unicode: false, nullable: true),
                    ExpirationDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCertificate", x => x.ClientCertificateId);
                    table.ForeignKey(
                        name: "FK_ClientCertificate_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                });

            migrationBuilder.CreateTable(
                name: "UseClient",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseClient", x => new { x.UserId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_UseClient_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_UseClient_User",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientCallBack",
                columns: table => new
                {
                    ClientCallBackId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ApiKeyId = table.Column<int>(type: "integer", nullable: true),
                    ClientBrancheId = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(type: "character varying(300)", unicode: false, maxLength: 300, nullable: false),
                    Secret = table.Column<string>(type: "character varying(200)", unicode: false, maxLength: 200, nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCallBack", x => x.ClientCallBackId);
                    table.ForeignKey(
                        name: "FK_ClientCallBack_ApiKey",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKey",
                        principalColumn: "ApiKeyId");
                    table.ForeignKey(
                        name: "FK_ClientCallBack_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_ClientCallBack_ClientBranche",
                        column: x => x.ClientBrancheId,
                        principalTable: "ClientBranche",
                        principalColumn: "ClientBrancheId");
                    table.ForeignKey(
                        name: "FK_ClientCallBack_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "StatusId");
                });

            migrationBuilder.CreateTable(
                name: "EcfDocument",
                columns: table => new
                {
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ClientBrancheId = table.Column<int>(type: "integer", nullable: false),
                    ApiKeyId = table.Column<int>(type: "integer", nullable: false),
                    EcfTypeId = table.Column<int>(type: "integer", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(70)", unicode: false, maxLength: 70, nullable: false),
                    Ncf = table.Column<string>(type: "character varying(80)", unicode: false, maxLength: 80, nullable: false),
                    CustomerRnc = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", unicode: false, maxLength: 200, nullable: true),
                    CustomerAddress = table.Column<string>(type: "character varying(300)", unicode: false, maxLength: 300, nullable: true),
                    IssueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Itbistotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EcfStatusId = table.Column<int>(type: "integer", nullable: false),
                    HangfireJobId = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfDocument", x => x.EcfDocumentId);
                    table.ForeignKey(
                        name: "FK_EcfDocument_ApiKey",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKey",
                        principalColumn: "ApiKeyId");
                    table.ForeignKey(
                        name: "FK_EcfDocument_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_EcfDocument_ClientBranche",
                        column: x => x.ClientBrancheId,
                        principalTable: "ClientBranche",
                        principalColumn: "ClientBrancheId");
                    table.ForeignKey(
                        name: "FK_EcfDocument_Currency",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId");
                    table.ForeignKey(
                        name: "FK_EcfDocument_EcfStatus",
                        column: x => x.EcfStatusId,
                        principalTable: "EcfStatus",
                        principalColumn: "EcfStatusId");
                    table.ForeignKey(
                        name: "FK_EcfDocument_EcfType",
                        column: x => x.EcfTypeId,
                        principalTable: "EcfType",
                        principalColumn: "EcfTypeId");
                });

            migrationBuilder.CreateTable(
                name: "EcfDocumentDetail",
                columns: table => new
                {
                    EcfDocumentDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", unicode: false, maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCode = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ItbisPercentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ItbisAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfDocumentDetail", x => x.EcfDocumentDetailId);
                    table.ForeignKey(
                        name: "FK_EcfDocumentDetail_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                });

            migrationBuilder.CreateTable(
                name: "EcfDocumentTotal",
                columns: table => new
                {
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    EcfDocumentTotalId = table.Column<int>(type: "integer", nullable: false),
                    TaxableTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExemptTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ITBISTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfDocumentTotal", x => x.EcfDocumentId);
                    table.ForeignKey(
                        name: "FK_EcfDocumentTotal_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                });

            migrationBuilder.CreateTable(
                name: "EcfStatusHistory",
                columns: table => new
                {
                    EcfStatusHistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    EcfStatusId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfStatusHistory", x => x.EcfStatusHistoryId);
                    table.ForeignKey(
                        name: "FK_EcfStatusHistory_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                    table.ForeignKey(
                        name: "FK_EcfStatusHistory_EcfStatus",
                        column: x => x.EcfStatusId,
                        principalTable: "EcfStatus",
                        principalColumn: "EcfStatusId");
                });

            migrationBuilder.CreateTable(
                name: "EcfTransmission",
                columns: table => new
                {
                    EcfTransmissionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    TrackId = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: true),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    EcfStatusId = table.Column<int>(type: "integer", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResponseCode = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    ResponseMessage = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfTransmission", x => x.EcfTransmissionId);
                    table.ForeignKey(
                        name: "FK_EcfTransmission_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                    table.ForeignKey(
                        name: "FK_EcfTransmission_EcfStatus",
                        column: x => x.EcfStatusId,
                        principalTable: "EcfStatus",
                        principalColumn: "EcfStatusId");
                });

            migrationBuilder.CreateTable(
                name: "EcfXmlDocument",
                columns: table => new
                {
                    EcfXmlDocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    XmlUnsigned = table.Column<string>(type: "text", nullable: false),
                    XmlSigned = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcfXmlDocument", x => x.EcfXmlDocumentId);
                    table.ForeignKey(
                        name: "FK_EcfXmlDocument_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                });

            migrationBuilder.CreateTable(
                name: "SystemLog",
                columns: table => new
                {
                    SystemLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    EcfDocumentId = table.Column<int>(type: "integer", nullable: false),
                    LogLevel = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreateAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValue: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLog", x => x.SystemLogId);
                    table.ForeignKey(
                        name: "FK_SystemLog_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_SystemLog_EcfDocument",
                        column: x => x.EcfDocumentId,
                        principalTable: "EcfDocument",
                        principalColumn: "EcfDocumentId");
                });

            migrationBuilder.InsertData(
                table: "EcfStatus",
                columns: new[] { "EcfStatusId", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Created" },
                    { 2, null, null, "Validating" },
                    { 3, null, null, "ValidationFailed" },
                    { 4, null, null, "ReadyToSign" },
                    { 5, null, null, "Signing" },
                    { 6, null, null, "Signed" },
                    { 7, null, null, "SendPending" },
                    { 8, null, null, "Sending" },
                    { 9, null, null, "Sent" },
                    { 10, null, null, "Accepted" },
                    { 11, null, null, "Rejected" },
                    { 12, null, null, "Error" },
                    { 13, null, null, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "Status",
                columns: new[] { "StatusId", "DeletedTimeUtc", "LastUpdateUtc", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Active" },
                    { 2, null, null, "Inactive" },
                    { 3, null, null, "Suspended" },
                    { 4, null, null, "Deleted" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_ClientId",
                table: "ApiKey",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_StatusId",
                table: "ApiKey",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Client_StatusId",
                table: "Client",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientBranche_ClientId",
                table: "ClientBranche",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientBranche_StatusId",
                table: "ClientBranche",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCallBack_ApiKeyId",
                table: "ClientCallBack",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCallBack_ClientBrancheId",
                table: "ClientCallBack",
                column: "ClientBrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCallBack_ClientId",
                table: "ClientCallBack",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCallBack_StatusId",
                table: "ClientCallBack",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCertificate_ClientId",
                table: "ClientCertificate",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_ApiKeyId",
                table: "EcfDocument",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_ClientBrancheId",
                table: "EcfDocument",
                column: "ClientBrancheId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_ClientId",
                table: "EcfDocument",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_CurrencyId",
                table: "EcfDocument",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_EcfStatusId",
                table: "EcfDocument",
                column: "EcfStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocument_EcfTypeId",
                table: "EcfDocument",
                column: "EcfTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfDocumentDetail_EcfDocumentId",
                table: "EcfDocumentDetail",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfStatusHistory_EcfDocumentId",
                table: "EcfStatusHistory",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfStatusHistory_EcfStatusId",
                table: "EcfStatusHistory",
                column: "EcfStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfTransmission_EcfDocumentId",
                table: "EcfTransmission",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfTransmission_EcfStatusId",
                table: "EcfTransmission",
                column: "EcfStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EcfXmlDocument_EcfDocumentId",
                table: "EcfXmlDocument",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLog_ClientId",
                table: "SystemLog",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLog_EcfDocumentId",
                table: "SystemLog",
                column: "EcfDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UseClient_ClientId",
                table: "UseClient",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ClientCallBack");

            migrationBuilder.DropTable(
                name: "ClientCertificate");

            migrationBuilder.DropTable(
                name: "DGIIUnit");

            migrationBuilder.DropTable(
                name: "EcfDocumentDetail");

            migrationBuilder.DropTable(
                name: "EcfDocumentTotal");

            migrationBuilder.DropTable(
                name: "EcfStatusHistory");

            migrationBuilder.DropTable(
                name: "EcfTransmission");

            migrationBuilder.DropTable(
                name: "EcfXmlDocument");

            migrationBuilder.DropTable(
                name: "SystemLog");

            migrationBuilder.DropTable(
                name: "UseClient");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "EcfDocument");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ApiKey");

            migrationBuilder.DropTable(
                name: "ClientBranche");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "EcfStatus");

            migrationBuilder.DropTable(
                name: "EcfType");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "Status");
        }
    }
}
