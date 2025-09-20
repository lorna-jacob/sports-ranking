using DepthCharts.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DepthCharts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly IDepthChartService _depthChartService;

        public TeamsController(IDepthChartService depthChartService)
        {
            _depthChartService = depthChartService;
        }

        /// <summary>
        /// Gets all available teams
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTeams(CancellationToken ct)
        {
            var teams = await _depthChartService.GetTeamsAsync(ct);
            return Ok(teams);
        }
    }
}
