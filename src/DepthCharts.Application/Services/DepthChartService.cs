using DepthCharts.Application.Abstractions;
using DepthCharts.Application.Common;
using DepthCharts.Domain.Entities;
using DepthCharts.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace DepthCharts.Application.Services
{
    public class DepthChartService : IDepthChartService
    {
        private readonly IDepthChartRepository _depthChartRepository;

        public DepthChartService(IDepthChartRepository depthChartRepository, ILogger<DepthChartService> logger)
        {
            _depthChartRepository = depthChartRepository;
        }

        public async Task AddPlayerAsync(string teamId, string position, Player player, int? positionDepth, CancellationToken ct)
        {
            Guard.NotEmpty(teamId, nameof(teamId));
            Guard.NotEmpty(position, nameof(position));
            Guard.Positive(player.Number, "player.Number");
            Guard.NotEmpty(player.Name, "player.Name");

            await _depthChartRepository.AddOrUpdatePlayerAsync(player, ct);
            await _depthChartRepository.RemovePlayerAsync(teamId, position.Trim().ToUpperInvariant(), player.Number, ct);
            
            await _depthChartRepository.AddPlayerAsync(teamId, position.Trim().ToUpperInvariant(), player.Number, positionDepth, ct);
        }

        public async Task<Player?> RemoveAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            Guard.NotEmpty(teamId, nameof(teamId));
            Guard.NotEmpty(position, nameof(position));
            Guard.Positive(playerNumber, nameof(playerNumber));

            return await _depthChartRepository.RemovePlayerAsync(teamId, position.Trim().ToUpperInvariant(), playerNumber, ct);
        }

        public async Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            Guard.NotEmpty(teamId, nameof(teamId));
            Guard.NotEmpty(position, nameof(position));
            Guard.Positive(playerNumber, nameof(playerNumber));

            return await _depthChartRepository.GetBackupsAsync(teamId, position.Trim().ToUpperInvariant(), playerNumber, ct);
        }

        public async Task<IReadOnlyDictionary<string, List<(string position, List<Player> players)>>> GetFullDepthChartAsync(string teamId, string league, CancellationToken ct)
        {
            Guard.NotEmpty(teamId, nameof(teamId));
            Guard.NotEmpty(league, nameof(league));

            var fullDepthChart = await _depthChartRepository.GetFullDepthChartAsync(teamId, ct);
            var positionsMetadata = await _depthChartRepository.GetPositionsAsync(league, ct);
            var positionsByCode = positionsMetadata.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

            var groups = new Dictionary<string, List<(string position, List<Player> players)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (position, players) in fullDepthChart)
            {
                var metadata = positionsByCode.TryGetValue(position, out var m) ?
                    m :
                    new PositionMetadata
                    {
                        Code = position,
                        Name = position,
                        Group = "Other",
                        SortOrder = int.MaxValue
                    };

                if (!groups.TryGetValue(metadata.Group, out var list))
                {
                    groups[metadata.Group] = list = new List<(string position, List<Player> players)>();
                }

                list.Add((position, players));
            }

            // Sort positions within each group
            foreach (var key in groups.Keys.ToList())
            {
                groups[key] = groups[key]
                    .OrderBy(x => positionsByCode.TryGetValue(x.position, out var m) ? m.SortOrder : int.MaxValue)
                    .ThenBy(x => x.position, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return groups;
        }

        public async Task AddOrUpdatePlayerAsync(Player player, CancellationToken ct)
        {
            Guard.Positive(player.Number, "player.Number");
            Guard.NotEmpty(player.Name, "player.Name");

            await _depthChartRepository.AddOrUpdatePlayerAsync(player, ct);
        }

        public async Task<List<Team>> GetTeamsAsync(CancellationToken ct)
        {
            return await _depthChartRepository.GetTeamsAsync(ct);
        }
    }
}
