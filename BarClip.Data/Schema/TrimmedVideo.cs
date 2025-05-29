using Microsoft.EntityFrameworkCore;
using System;

namespace BarClip.Data.Schema
{
    public class TrimmedVideo
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid OriginalVideoId { get; set; }
        public Video? OriginalVideo { get; set; }

        public Guid VideoConfigurationId { get; set; }
        public VideoConfiguration? VideoConfiguration { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrimmedVideo>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.HasOne(t => t.OriginalVideo)
                    .WithMany(v => v.TrimmedVideos)
                    .HasForeignKey(t => t.OriginalVideoId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(t => t.VideoConfiguration)
                    .WithMany(vs => vs.TrimmedVideos)
                    .HasForeignKey(t => t.VideoConfigurationId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(t => t.User)
                    .WithMany(u => u.TrimmedVideos)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
