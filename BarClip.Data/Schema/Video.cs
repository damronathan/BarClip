using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Xabe.FFmpeg;

namespace BarClip.Data.Schema
{
    public class Video
    {
        public Guid Id { get; set; }

        public User User { get; set; } = null!;
        public Guid UserId { get; set; }

        public string FilePath { get; set; } = null!;
        public string OutputPath { get; set; } = @"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets";
        public TimeSpan Duration { get; set; }
        public double FrameRate { get; set; }
        public DateTime UploadedAt { get; set; }
        public VideoStatus Status { get; set; }

        public Guid VideoConfigurationId { get; set; }
        public VideoConfiguration VideoConfiguration { get; set; } = null!;

        public List<Frame> Frames { get; set; } = new();
        public List<TrimmedVideo> TrimmedVideos { get; set; } = new();
        [NotMapped]
        public IVideoStream? VideoStream { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasOne(v => v.User)
                      .WithMany(u => u.Videos)
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(v => v.VideoConfiguration)
                      .WithMany(vs => vs.Videos)
                      .HasForeignKey(v => v.VideoConfigurationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(v => v.Frames)
                      .WithOne(f => f.Video)
                      .HasForeignKey(f => f.VideoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(v => v.TrimmedVideos)
                      .WithOne(t => t.OriginalVideo)
                      .HasForeignKey(t => t.OriginalVideoId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }

    public enum VideoStatus
    {
        Uploaded,
        Processing,
        Trimmed,
        Failed
    }
}
