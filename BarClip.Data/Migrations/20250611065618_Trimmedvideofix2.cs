using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trimmedvideofix2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos",
                column: "OriginalVideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos",
                column: "OriginalVideoId",
                unique: true);
        }
    }
}
