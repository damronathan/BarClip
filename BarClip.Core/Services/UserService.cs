using BarClip.Core.Repositories;
using BarClip.Data.Schema;

namespace BarClip.Core.Services;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> GetOrCreateUserAsync(string nameIdentifier, string? email = null)
    {
        var existingUser = await _userRepository.GetByNameIdentifierAsync(nameIdentifier);

        if (existingUser != null)
        {
            if ((email != null && existingUser.Email != email))
            {
                existingUser.Email = email ?? existingUser.Email;
                await _userRepository.UpdateAsync(existingUser);
            }
            return existingUser;
        }

        var newUser = new User
        {
            Id = nameIdentifier,
            Email = email,          
        };

        await _userRepository.CreateAsync(newUser);
        return newUser;
    }
}
