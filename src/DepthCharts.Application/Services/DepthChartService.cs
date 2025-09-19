using DepthCharts.Application.Abstractions;
using DepthCharts.Application.Common;
using DepthCharts.Domain.Entities;
using DepthCharts.Infrastructure.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DepthCharts.Application.Services
{
    public class DepthChartService : IDepthChartService
    {
        private readonly IDepthChartRepository _depthChartRepository;        

        public DepthChartService(IDepthChartRepository depthChartRepository)
        {
            _depthChartRepository = depthChartRepository;
        }

        public async Task AddPlayerAsync(string teamId, string position, Player player, int? positionDepth, CancellationToken ct)
        {
            Guard.NotEmpty(teamId, nameof(teamId));
            Guard.NotEmpty(position, nameof(position));
            Guard.Positive(player.Number, "player.Number");
            Guard.NotEmpty(player.Name, "player.Name");

            //var valid = await _depthChartRepository.PositionExistsAsync(position.Trim().ToUpperInvariant());

            await _depthChartRepository.RemovePlayerAsync(teamId, position, player.Number, ct);
            await _depthChartRepository.AddPlayerAsync(teamId, position, player.Number, positionDepth, ct);
        }

        public async Task<Player?> RemoveAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            var pos = position.Trim().ToUpperInvariant();
            var result = await _depthChartRepository.RemovePlayerAsync(teamId, pos, playerNumber, ct);
            return result;
        }

        public async Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            var pos = position.Trim().ToUpperInvariant();
            var result = await _depthChartRepository.GetBackupsAsync(teamId, pos, playerNumber, ct);
            return result;
        }

        public async Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct)
        {
            var result = await _depthChartRepository.GetFullDepthChartAsync(teamId, ct);
            return result;
        }

        public async Task<IReadOnlyDictionary<string, List<(string position, List<Player> players)>>> GetFullDepthChartGroupedAsync(string teamId, string league, CancellationToken ct)
        {
            var fullDepthChart = await _depthChartRepository.GetFullDepthChartAsync(teamId, ct);
            var positionsMetadata = await _depthChartRepository.GetPositionsAsync(league, ct);
            var positionsByCode = positionsMetadata.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

            var groups = new Dictionary<string, List<(string position, List<Player> players)>>(StringComparer.OrdinalIgnoreCase);

            foreach(var (position, players) in fullDepthChart)
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

                if(!groups.TryGetValue(metadata.Group, out var list))
                {
                    groups[metadata.Group] = list = new List<(string position, List<Player> players)>();

                    list.Add((position, players));
                }
            }

            foreach(var key in groups.Keys.ToList())
            {
                groups[key] = groups[key]
                    .OrderBy(x => positionsByCode.TryGetValue(x.position, out var m) ? m.SortOrder : int.MaxValue)
                    .ThenBy(x => x.position, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return groups;
        }
    }
}
