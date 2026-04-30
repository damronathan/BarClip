public interface IAuthService
{
    Task<string> GetTokenAsync();
    Task<string> GetUserIdAsync();
    Task<bool> IsSignedInAsync();
    Task SignOutAsync();
}