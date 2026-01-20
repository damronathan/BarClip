using BarClip.Core.Repositories;
using BarClip.Data.Schema;
using System.Windows.Markup;

namespace BarClip.Core.Services;
public class SessionService
{
    private readonly SessionRepository _repo;
    public SessionService(SessionRepository repo)
    {
        _repo = repo;
    }
    public async Task<Session> CreateSession(User? user, string title)
    {
        var session = new Session
        {
            User = user,
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.CreateSessionAsync(session);
        return session;
    }
    public async Task<Session> CreateSessionWithFolders(User user, string basePath, string title)
    {
        var session = await CreateSession(user, title);

        var sessionPath = Path.Combine(basePath, session.Id.ToString());
        var folders = new[]
        {
        sessionPath,
        Path.Combine(sessionPath, "Thumbnails"),
        Path.Combine(sessionPath, "Processed"),
        Path.Combine(sessionPath, "Original")
    };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);
        }

        return session;
    }
    public async Task<List<Session>> GetAllSessions()
    {
        return await _repo.GetAllSessionsAsync();
    }
    public async Task DeleteSession(Guid sessionId)
    {
        await _repo.DeleteSessionAsync(sessionId);
    }
}
