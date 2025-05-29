using System;

namespace BarClip.Data.Schema
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 