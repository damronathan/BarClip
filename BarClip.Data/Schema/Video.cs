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
        public string? Name { get; set; }
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }
        public string FilePath { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public List<Frame> Frames { get; set; } = new();
        public List<TrimmedVideo> TrimmedVideos { get; set; } = new();
        public VideoStatus VideoStatus { get; set; }

        [NotMapped]
        public required IMediaInfo VideoInfo { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasOne(v => v.User)
                      .WithMany(u => u.Videos)
                      .HasForeignKey(v => v.UserId)
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
