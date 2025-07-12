using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data.Schema;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BarClip.Api.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly StorageService _storageService;
    private readonly UserRepository _repo;
    public UserController(StorageService storageService, UserRepository repo)
    {
        _storageService = storageService;
        _repo = repo;
    }

    //[HttpGet("me")]
    //public async Task<IActionResult> GetOrCreateCurrentUser()
    //{

    //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
    //        ?? User.FindFirst("sub")?.Value;
    //    var email = User.FindFirst("emails")?.Value ?? User.FindFirst("email")?.Value;

    //    if (string.IsNullOrEmpty(userId))
    //    {
    //        return Unauthorized("User ID claim not found");
    //    }

    //    //var user = await _userService.GetOrCreateUserAsync(userId, email);
    //    return Ok(new
    //    //{
    //    //    user.Id,
    //    //    user.Email
    //    //});
    //}

    
}
