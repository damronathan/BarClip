using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BarClip.Api.Hubs;
[Authorize]
public class VideoStatusHub : Hub
{
}
public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Return the claim value you want to use as user ID
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    }
}
