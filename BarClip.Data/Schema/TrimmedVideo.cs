using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarClip.Data.Schema
{
    public class TrimmedVideo
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public required User User { get; set; }
        public Guid OriginalVideoId { get; set; }
        public OriginalVideo? OriginalVideo { get; set; }
        public TimeSpan Duration { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrimmedVideo>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.HasOne(t => t.OriginalVideo)
                    .WithMany(v => v.TrimmedVideos)
                    .HasForeignKey(t => t.OriginalVideoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.User)
                    .WithMany(u => u.TrimmedVideos)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

            });
        }
    }
}
