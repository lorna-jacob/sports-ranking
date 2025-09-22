using DepthCharts.Domain.Entities;

namespace DepthCharts.Domain.Abstractions
{
    public interface IDepthChartRepository
    {
        Task AddPlayerAsync(string teamId, string position, int playerNumber, int? positionDepth, CancellationToken ct);
        Task<Player?> RemovePlayerAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct);
        Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct);
        Task AddOrUpdatePlayerAsync(Player player, CancellationToken ct);
        Task<IReadOnlyList<PositionMetadata>> GetPositionsAsync(string league, CancellationToken ct);
        Task<List<Team>> GetTeamsAsync(CancellationToken ct);
    }
}
