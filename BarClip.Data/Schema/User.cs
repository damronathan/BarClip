using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class User
    {
        public Guid Id { get; set; }

        public List<Video> Videos { get; set; } = new();
        public List<TrimmedVideo> TrimmedVideos { get; set; } = new();
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasMany(u => u.Videos)
                      .WithOne(v => v.User)
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.TrimmedVideos)
                      .WithOne(t => t.User)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

            });
        }
    }
}
