using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalFrames = table.Column<int>(type: "int", nullable: false),
                    ProcessedFrames = table.Column<int>(type: "int", nullable: false),
                    DetectedPlates = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    FrameRate = table.Column<double>(type: "float", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VideoConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Videos_VideoConfigurations_VideoConfigurationId",
                        column: x => x.VideoConfigurationId,
                        principalTable: "VideoConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Frames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<TimeSpan>(type: "time", nullable: false),
                    FrameNumber = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Frames_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrimmedVideos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrimmedVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrimmedVideos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrimmedVideos_VideoConfigurations_VideoConfigurationId",
                        column: x => x.VideoConfigurationId,
                        principalTable: "VideoConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrimmedVideos_Videos_OriginalVideoId",
                        column: x => x.OriginalVideoId,
                        principalTable: "Videos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FrameTensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameTensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameTensors_Frames_FrameId",
                        column: x => x.FrameId,
                        principalTable: "Frames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DetectionOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameTensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputShape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectionOutputs_FrameTensors_FrameTensorId",
                        column: x => x.FrameTensorId,
                        principalTable: "FrameTensors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlateDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectionOutputId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    BoundingBox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlateDetections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlateDetections_DetectionOutputs_DetectionOutputId",
                        column: x => x.DetectionOutputId,
                        principalTable: "DetectionOutputs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlateDetections_Frames_FrameId",
                        column: x => x.FrameId,
                        principalTable: "Frames",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlateDetections_VideoConfigurations_VideoConfigurationId",
                        column: x => x.VideoConfigurationId,
                        principalTable: "VideoConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectionOutputs_FrameTensorId",
                table: "DetectionOutputs",
                column: "FrameTensorId");

            migrationBuilder.CreateIndex(
                name: "IX_Frames_VideoId",
                table: "Frames",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameTensors_FrameId",
                table: "FrameTensors",
                column: "FrameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlateDetections_DetectionOutputId",
                table: "PlateDetections",
                column: "DetectionOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_PlateDetections_FrameId",
                table: "PlateDetections",
                column: "FrameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlateDetections_VideoConfigurationId",
                table: "PlateDetections",
                column: "VideoConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_OriginalVideoId",
                table: "TrimmedVideos",
                column: "OriginalVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_UserId",
                table: "TrimmedVideos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_VideoConfigurationId",
                table: "TrimmedVideos",
                column: "VideoConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoConfigurations_UserId",
                table: "VideoConfigurations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_UserId",
                table: "Videos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_VideoConfigurationId",
                table: "Videos",
                column: "VideoConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlateDetections");

            migrationBuilder.DropTable(
                name: "TrimmedVideos");

            migrationBuilder.DropTable(
                name: "DetectionOutputs");

            migrationBuilder.DropTable(
                name: "FrameTensors");

            migrationBuilder.DropTable(
                name: "Frames");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "VideoConfigurations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
