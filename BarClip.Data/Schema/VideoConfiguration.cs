using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class VideoConfiguration
    {
        public Guid Id { get; set; }

        public Guid OriginalVideoId { get; set; }

        public List<PlateDetection> PlateDetections { get; set; } = new();

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public List<TrimmedVideo> TrimmedVideos { get; set; } = new();

        public List<Video> Videos { get; set; } = new();

        public AnalysisStatus Status { get; set; }

        public int TotalFrames { get; set; }
        public int ProcessedFrames { get; set; }
        public int DetectedPlates { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VideoConfiguration>(entity =>
            {
                entity.HasKey(vs => vs.Id);

                entity.HasOne(vs => vs.User)
                      .WithOne(u => u.VideoSettings)
                      .HasForeignKey<VideoConfiguration>(vs => vs.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(vs => vs.TrimmedVideos)
                      .WithOne(t => t.VideoConfiguration)
                      .HasForeignKey(t => t.VideoConfigurationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(vs => vs.Videos)
                      .WithOne(v => v.VideoConfiguration)
                      .HasForeignKey(v => v.VideoConfigurationId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }

    public enum AnalysisStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
}
