namespace BarClip.Models.Requests;

public class ReTrimVideoRequest
{
    public Guid Id { get; set; }
    public bool StartEarlier { get; set; }
    public bool FinishEarlier { get; set; }
    public double TrimStart { get; set; }
    public double TrimFinish { get; set; }
}
