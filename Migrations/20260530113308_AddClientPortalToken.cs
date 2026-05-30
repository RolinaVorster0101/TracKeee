using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracKeee.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPortalToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalToken",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortalToken",
                table: "Clients");
        }
    }
}
