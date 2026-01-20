using BarClip.Data.Schema;
using BarClip.Data;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Core.Repositories;
public class SessionRepository
{
    private readonly AppDbContext _context;

    public SessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateSessionAsync(Session session)
    {
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }
    public async Task<Session?> GetSessionByIdAsync(Guid sessionId)
    {
        var result = await _context.Sessions.FindAsync(sessionId);
        return result;
    }
    public async Task<List<Session>> GetAllSessionsAsync()
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .ToListAsync();
        return result;
    }
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session != null)
        {
            _context.Sessions.Remove(session);
        }
        else
        {
            throw new InvalidOperationException("No session found");
        }
            await _context.SaveChangesAsync();
    }


}
