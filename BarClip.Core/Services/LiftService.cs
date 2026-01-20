using BarClip.Core.Repositories;
using BarClip.Data.Schema;
using BarClip.Models.Dto;
using SQLitePCL;

namespace BarClip.Core.Services;
public class LiftService
{
    LiftRepository _repo;
    public LiftService(LiftRepository repo)
    {
        _repo = repo;
    }
    public async Task<Lift> CreateLift(Lift lift)
    {
        await _repo.AddLiftAsync(lift);
        return lift;
    }
    public async Task<Lift?> GetLift(Guid Id)
    {
        var lift = await _repo.GetLiftByIdAsync(Id);
        return lift;
    }
    public async Task<Lift> GetLiftByOriginalVideoId(Guid originalVideoId, Guid sessionId)
    {
        var newLift = new Lift
        {
            OriginalVideoId = originalVideoId,
            SessionId = sessionId
        };
        var lift = await _repo.GetLiftByOriginalVideoIdAsync(originalVideoId);
        if (lift == null)
        {
            await _repo.AddLiftAsync(newLift);
            return newLift;
        }
        return lift;
    }
    public async Task UpdateLift(Lift lift)
    {
        await _repo.UpdateLiftAsync(lift);
    }
}
