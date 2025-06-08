using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarClip.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanedUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlateDetections_DetectionOutputs_DetectionOutputId",
                table: "PlateDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_PlateDetections_VideoConfigurations_VideoConfigurationId",
                table: "PlateDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_TrimmedVideos_VideoConfigurations_VideoConfigurationId",
                table: "TrimmedVideos");

            migrationBuilder.DropForeignKey(
                name: "FK_Videos_VideoConfigurations_VideoConfigurationId",
                table: "Videos");

            migrationBuilder.DropTable(
                name: "DetectionOutputs");

            migrationBuilder.DropTable(
                name: "VideoConfigurations");

            migrationBuilder.DropTable(
                name: "FrameTensors");

            migrationBuilder.DropIndex(
                name: "IX_Videos_VideoConfigurationId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_TrimmedVideos_VideoConfigurationId",
                table: "TrimmedVideos");

            migrationBuilder.DropIndex(
                name: "IX_PlateDetections_DetectionOutputId",
                table: "PlateDetections");

            migrationBuilder.DropIndex(
                name: "IX_PlateDetections_FrameId",
                table: "PlateDetections");

            migrationBuilder.DropIndex(
                name: "IX_PlateDetections_VideoConfigurationId",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "VideoConfigurationId",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "VideoConfigurationId",
                table: "TrimmedVideos");

            migrationBuilder.DropColumn(
                name: "BoundingBox",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "DetectedAt",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "DetectionOutputId",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "PlateText",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "VideoConfigurationId",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Frames");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "Frames");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Frames");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Frames");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Videos",
                newName: "VideoStatus");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Videos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetectionNumber",
                table: "PlateDetections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "PlateDetections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "PlateDetections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "X",
                table: "PlateDetections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Y",
                table: "PlateDetections",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateIndex(
                name: "IX_PlateDetections_FrameId",
                table: "PlateDetections",
                column: "FrameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlateDetections_FrameId",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DetectionNumber",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "X",
                table: "PlateDetections");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "PlateDetections");

            migrationBuilder.RenameColumn(
                name: "VideoStatus",
                table: "Videos",
                newName: "Status");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Videos",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "VideoConfigurationId",
                table: "Videos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VideoConfigurationId",
                table: "TrimmedVideos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "BoundingBox",
                table: "PlateDetections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DetectedAt",
                table: "PlateDetections",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "DetectionOutputId",
                table: "PlateDetections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "PlateDetections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlateText",
                table: "PlateDetections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "PlateDetections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VideoConfigurationId",
                table: "PlateDetections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Frames",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "Frames",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Timestamp",
                table: "Frames",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Frames",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FrameTensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "VideoConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedPlates = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedFrames = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalFrames = table.Column<int>(type: "int", nullable: false)
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
                name: "DetectionOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameTensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputShape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawOutput = table.Column<string>(type: "nvarchar(max)", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Videos_VideoConfigurationId",
                table: "Videos",
                column: "VideoConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimmedVideos_VideoConfigurationId",
                table: "TrimmedVideos",
                column: "VideoConfigurationId");

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
                name: "IX_DetectionOutputs_FrameTensorId",
                table: "DetectionOutputs",
                column: "FrameTensorId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameTensors_FrameId",
                table: "FrameTensors",
                column: "FrameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoConfigurations_UserId",
                table: "VideoConfigurations",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlateDetections_DetectionOutputs_DetectionOutputId",
                table: "PlateDetections",
                column: "DetectionOutputId",
                principalTable: "DetectionOutputs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlateDetections_VideoConfigurations_VideoConfigurationId",
                table: "PlateDetections",
                column: "VideoConfigurationId",
                principalTable: "VideoConfigurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrimmedVideos_VideoConfigurations_VideoConfigurationId",
                table: "TrimmedVideos",
                column: "VideoConfigurationId",
                principalTable: "VideoConfigurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Videos_VideoConfigurations_VideoConfigurationId",
                table: "Videos",
                column: "VideoConfigurationId",
                principalTable: "VideoConfigurations",
                principalColumn: "Id");
        }
    }
}
