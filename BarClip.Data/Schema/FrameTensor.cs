using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class FrameTensor(float[] data)
    {
        public Guid Id { get; set; }
        public Guid FrameId { get; set; }
        public Frame? Frame { get; set; }
        public float[] Data { get; set; } = data;


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FrameTensor>(entity =>
            {
                entity.HasKey(ft => ft.Id);
                entity.HasOne(ft => ft.Frame)
                    .WithOne(f => f.FrameTensor)
                    .HasForeignKey<FrameTensor>(ft => ft.FrameId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
