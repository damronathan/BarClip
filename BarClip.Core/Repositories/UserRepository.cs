using Azure.Core;
using BarClip.Data;
using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Core.Repositories;

public class UserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<User> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }
    public async Task<User?> GetByNameIdentifierAsync(string nameIdentifier)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EntraId == nameIdentifier);
    }
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        return users;
    }
    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        
    }
    public async Task<User> VerifyOrCreateUser(Guid id)
    {
        var existingUser = await GetUserByIdAsync(id);

        if (existingUser is not null)
        {
            return existingUser;
        }
        else
        {
            var user = new User
            {
                Id = id,
            };
            _context.Users.Add(user);
            return user;
        }
    }
}
