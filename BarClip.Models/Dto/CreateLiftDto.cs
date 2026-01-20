using BarClip.Models.Requests;

namespace BarClip.Models.Dto;
public class CreateLiftDto
{
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public bool Successful { get; set; }
    public string ExerciseName { get; set; }
    public LifterFilter LifterFilter { get; set; }
}
