using BarClip.Models.Domain;
using Microsoft.ML.OnnxRuntime;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarClip.Models.Domain;

[NotMapped]
public class Frame
{
    
    public List<PlateDetection>? PlateDetections { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int FrameNumber { get; set; }
    public NamedOnnxValue? InputValue { get; set; }
}
