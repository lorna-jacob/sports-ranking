using DepthCharts.Api.Models;
using DepthCharts.Application.Abstractions;
using DepthCharts.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DepthCharts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepthChartController : ControllerBase
    {
        private readonly IDepthChartService _depthChartService;
        private readonly ILogger<DepthChartController> _logger;

        public DepthChartController(IDepthChartService depthChartService, ILogger<DepthChartController> logger)
        {
            _depthChartService = depthChartService;
            _logger = logger;
        }

        /// <summary>
        /// Adds a player to the depth chart at a given position
        /// </summary>
        [HttpPost("{teamId}/players")]
        public async Task<IActionResult> AddPlayer(
            string teamId, 
            [FromBody] AddPlayerRequest request, 
            CancellationToken ct)
        {
            await _depthChartService.AddPlayerAsync(
                teamId, 
                request.Position, 
                request.Player, 
                request.PositionDepth, 
                ct);

            return Ok(new { message = "Player added successfully" });
        }

        /// <summary>
        /// Removes a player from the depth chart for a given position
        /// </summary>
        [HttpDelete("{teamId}/positions/{position}/players/{playerNumber}")]
        public async Task<IActionResult> RemovePlayer(
            string teamId, 
            string position, 
            int playerNumber, 
            CancellationToken ct)
        {
            var removedPlayer = await _depthChartService.RemoveAsync(teamId, position, playerNumber, ct);
            
            if (removedPlayer == null)
            {                
                return NotFound(new { message = "Player not found in depth chart at this position" });
            }

            return Ok(removedPlayer);
        }

        /// <summary>
        /// Gets all backup players for a given player and position
        /// </summary>
        [HttpGet("{teamId}/positions/{position}/players/{playerNumber}/backups")]
        public async Task<IActionResult> GetBackups(
            string teamId, 
            string position, 
            int playerNumber, 
            CancellationToken ct)
        {
            var backups = await _depthChartService.GetBackupsAsync(teamId, position, playerNumber, ct);
            return Ok(backups);
        }

        /// <summary>
        /// Gets the full depth chart for a team
        /// </summary>
        [HttpGet("{teamId}/depthchart")]
        public async Task<IActionResult> GetFullDepthChartGrouped(
            string teamId,
            [FromQuery] string league = "NFL",
            CancellationToken ct = default)
        {
            var chart = await _depthChartService.GetFullDepthChartAsync(teamId, league, ct);

            var result = chart.ToDictionary(
                group => group.Key,
                group => group.Value.Select(g => new {
                    g.position,
                    g.players
                }).ToArray()
            );

            return Ok(result);
        }
    }    
}