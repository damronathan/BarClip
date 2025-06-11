using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarClip.Data.Schema
{
    public class TrimmedVideo
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public Guid OriginalVideoId { get; set; }
        public Video? OriginalVideo { get; set; }
        public TimeSpan Duration { get; set; }

        [NotMapped]
        public string? FilePath { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrimmedVideo>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.HasOne(t => t.OriginalVideo)
                    .WithMany(v => v.TrimmedVideos)
                    .HasForeignKey(t => t.OriginalVideoId)
                    .OnDelete(DeleteBehavior.Cascade);

            });
        }
    }
}
