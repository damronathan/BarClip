using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

public class AuthService
{
    private readonly IPublicClientApplication _pca;
    private readonly string[] _scopes;

    public AuthService(IPublicClientApplication pca, IConfiguration config)
    {
        _pca = pca;
        _scopes = new[] { $"{config["AzureAd:ApiClientId"]}/access-as-user" };
    }

    public async Task<string> GetTokenAsync()
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync();
            var result = await _pca.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            var result = await _pca.AcquireTokenInteractive(_scopes)
                .WithParentActivityOrWindow(Platform.GetCurrentUIViewController())
                .ExecuteAsync();
            return result.AccessToken;
        }
    }

    public async Task SignOutAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        foreach (var account in accounts)
            await _pca.RemoveAsync(account);
    }

    public async Task<bool> IsSignedInAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        return accounts.Any();
    }
}