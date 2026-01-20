using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class CascadeSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntraId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<double>(type: "REAL", nullable: true),
                    RepsMade = table.Column<int>(type: "INTEGER", nullable: true),
                    RepsMissed = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OriginalVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrimStart = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TrimFinish = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CurrentProcessedVideoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginalVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginalVideos_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginalVideos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedVideos_OriginalVideos_OriginalVideoId",
                        column: x => x.OriginalVideoId,
                        principalTable: "OriginalVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessedVideos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: true),
                    Reps = table.Column<int>(type: "INTEGER", nullable: true),
                    Successful = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcessedVideoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExerciseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LifterFilter = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lifts_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lifts_OriginalVideos_OriginalVideoId",
                        column: x => x.OriginalVideoId,
                        principalTable: "OriginalVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lifts_ProcessedVideos_ProcessedVideoId",
                        column: x => x.ProcessedVideoId,
                        principalTable: "ProcessedVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lifts_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lifts_ExerciseId",
                table: "Lifts",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lifts_OriginalVideoId",
                table: "Lifts",
                column: "OriginalVideoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lifts_ProcessedVideoId",
                table: "Lifts",
                column: "ProcessedVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Lifts_SessionId",
                table: "Lifts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginalVideos_SessionId",
                table: "OriginalVideos",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginalVideos_UserId",
                table: "OriginalVideos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedVideos_OriginalVideoId",
                table: "ProcessedVideos",
                column: "OriginalVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedVideos_UserId",
                table: "ProcessedVideos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lifts");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "ProcessedVideos");

            migrationBuilder.DropTable(
                name: "OriginalVideos");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
