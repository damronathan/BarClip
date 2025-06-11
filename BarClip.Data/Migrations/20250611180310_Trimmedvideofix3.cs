using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trimmedvideofix3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentTrimmedVideoId",
                table: "Videos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentTrimmedVideoId",
                table: "Videos");
        }
    }
}
