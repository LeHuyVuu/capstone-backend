using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace capstone_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueLocationImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_image",
                table: "venue_locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "interior_image",
                table: "venue_locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "full_page_menu_image",
                table: "venue_locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_owner_verified",
                table: "venue_locations",
                type: "boolean",
                nullable: true,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_image",
                table: "venue_locations");

            migrationBuilder.DropColumn(
                name: "interior_image",
                table: "venue_locations");

            migrationBuilder.DropColumn(
                name: "full_page_menu_image",
                table: "venue_locations");

            migrationBuilder.DropColumn(
                name: "is_owner_verified",
                table: "venue_locations");
        }
    }
}
