using DepthCharts.Domain.Entities;

namespace DepthCharts.Application.Abstractions
{
    public interface IDepthChartService
    {
        Task AddPlayerAsync(string teamId, string position, Player player, int? positionDepth, CancellationToken ct);
        Task<Player?> RemoveAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct);
        Task<IReadOnlyDictionary<string, List<(string position, List<Player> players)>>> GetFullDepthChartGroupedAsync(string teamId, string league, CancellationToken ct);
    }
}