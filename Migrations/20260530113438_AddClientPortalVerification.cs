using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracKeee.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPortalVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PortalCodeExpiry",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalVerificationCode",
                table: "Clients",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortalCodeExpiry",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PortalVerificationCode",
                table: "Clients");
        }
    }
}
