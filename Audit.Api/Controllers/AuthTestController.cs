using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Api.Controllers;

[ApiController]
[Route("api/auth-test")]
public class AuthTestController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new
        {
            message = "Public endpoint works."
        });
    }

    [Authorize]
    [HttpGet("protected")]
    public IActionResult Protected()
    {
        return Ok(new
        {
            message = "Auth0 token accepted.",
            subject = User.FindFirstValue("sub")
        });
    }

    [Authorize(Policy = "QueueWrite")]
    [HttpPost("queue-write")]
    public IActionResult QueueWrite()
    {
        return Ok(new
        {
            message = "Authenticated and queue:write granted"
        });
    }

}