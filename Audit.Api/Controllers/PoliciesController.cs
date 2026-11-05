using System.Security.Claims;
using Audit.Application.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Api.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public sealed class PoliciesController(IPolicyService service) : ControllerBase
{
    [Authorize(Policy = "QueueWrite")]
    [HttpPost("{policyId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid policyId,
        [FromBody] ChangePolicyStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
        var clientId = User.FindFirstValue("client_id");

        if (string.IsNullOrWhiteSpace(actorId) ||
            string.IsNullOrWhiteSpace(clientId))
            return Forbid();

        try
        {
            var result = await service.ChangeStatusAsync(
                policyId,
                clientId,
                actorId,
                User.Identity?.Name,
                HttpContext.TraceIdentifier,
                request.Status,
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}