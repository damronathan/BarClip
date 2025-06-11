using System.ComponentModel.DataAnnotations.Schema;

namespace BarClip.Models.Domain;

[NotMapped]

public class PlateDetection
{
    public float Confidence { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int DetectionNumber { get; set; }
}
