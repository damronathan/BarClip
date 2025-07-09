using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedusers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OriginalVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrimStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    TrimFinish = table.Column<TimeSpan>(type: "time", nullable: false),
                    CurrentTrimmedVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginalVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginalVideos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrimmedVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrimmedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrimmedVideos_OriginalVideos_OriginalVideoId",
                        column: x => x.OriginalVideoId,
                        principalTable: "OriginalVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrimmedVideos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OriginalVideos_UserId",
                table: "OriginalVideos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos",
                column: "OriginalVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_UserId",
                table: "TrimmedVideos",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrimmedVideos");

            migrationBuilder.DropTable(
                name: "OriginalVideos");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
