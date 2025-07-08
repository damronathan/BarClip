using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class Removenamefromvideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "TrimmedVideos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TrimmedVideos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
