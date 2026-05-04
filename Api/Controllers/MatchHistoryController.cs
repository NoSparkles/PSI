using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Entities;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchHistoryController(
    ITournamentService tournamentService, 
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("my-history")]
    public async Task<ActionResult<List<MatchHistory>>> GetMyHistory()
    {
        var user = currentUserAccessor.GetCurrentUser(HttpContext);
        
        if (user == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var history = await tournamentService.GetUserMatchHistoryAsync(user.Id);
        
        return Ok(history);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<MatchHistory>>> GetUserHistory(Guid userId)
    {
        var history = await tournamentService.GetUserMatchHistoryAsync(userId);
        return Ok(history);
    }
}