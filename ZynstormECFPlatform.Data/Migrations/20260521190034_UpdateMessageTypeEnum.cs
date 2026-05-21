using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId1",
                table: "ReceivedB2BMessage",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedB2BMessage_ClientId1",
                table: "ReceivedB2BMessage",
                column: "ClientId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedB2BMessage_Client_ClientId1",
                table: "ReceivedB2BMessage",
                column: "ClientId1",
                principalTable: "Client",
                principalColumn: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedB2BMessage_Client_ClientId1",
                table: "ReceivedB2BMessage");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedB2BMessage_ClientId1",
                table: "ReceivedB2BMessage");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "ReceivedB2BMessage");
        }
    }
}
