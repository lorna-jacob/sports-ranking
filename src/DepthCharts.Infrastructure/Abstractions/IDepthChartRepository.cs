using DepthCharts.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepthCharts.Infrastructure.Abstractions
{
    public interface IDepthChartRepository
    {
        Task<IReadOnlyList<PositionMetadata>> GetPositionsAsync(string league, CancellationToken ct);
        Task AddPlayerAsync(string teamId, string position, int playerNumber, int? positionDepth, CancellationToken ct);
        Task<Player?> RemovePlayerAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct);
        Task<bool> PositionExistsAsync(string league, string code, CancellationToken ct);
    }
}
