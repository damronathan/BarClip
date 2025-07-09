using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BarClip.Api.Hubs;
[Authorize]
public class VideoStatusHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdentifier = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;
        var nameIdentifierClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Log all connected user info
        Console.WriteLine($"NEW CONNECTION:");
        Console.WriteLine($"  Connection ID: {connectionId}");
        Console.WriteLine($"  UserIdentifier: '{userIdentifier}'");
        Console.WriteLine($"  NameIdentifier Claim: '{nameIdentifierClaim}'");
        Console.WriteLine($"  User.Identity.Name: '{Context.User?.Identity?.Name}'");
        Console.WriteLine($"  Is Authenticated: {Context.User?.Identity?.IsAuthenticated}");

        await base.OnConnectedAsync();
    }
}
public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Return the claim value you want to use as user ID
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Or use a different claim:
        // return connection.User?.FindFirst("sub")?.Value;
        // return connection.User?.FindFirst("userId")?.Value;
    }
}
