using DepthCharts.Domain.Entities;
using DepthCharts.Infrastructure.Abstractions;
using DepthCharts.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DepthCharts.Infrastructure.Repositories
{
    public class JsonDepthChartRepository : IDepthChartRepository
    {
        private readonly string _dataDirectory;
        private readonly string _positionsFilePath;
        private readonly string _playersFilePath;
        private readonly string _teamsFilePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public JsonDepthChartRepository(IOptions<DataSettings> dataSettings)
        {
            _dataDirectory = dataSettings.Value.GetFullDataPath();

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _positionsFilePath = Path.Combine(_dataDirectory, "positions.json");
            _playersFilePath = Path.Combine(_dataDirectory, "players.json");
            _teamsFilePath = Path.Combine(_dataDirectory, "teams.json");
        }

        private string GetTeamDepthChartFilePath(string teamId)
        {
            return Path.Combine(_dataDirectory, $"depthcharts-{teamId.ToLowerInvariant()}.json");
        }

        public async Task AddPlayerAsync(string teamId, string position, int playerNumber, int? positionDepth, CancellationToken ct)
        {
            await _fileLock.WaitAsync(ct);
            try
            {
                var data = await LoadTeamDataAsync(teamId, ct);
                var positionEntries = GetOrCreatePositionEntries(data, position);

                positionEntries.RemoveAll(p => p.PlayerNumber == playerNumber);

                int targetDepth = positionDepth ?? positionEntries.Count;

                foreach (var entry in positionEntries.Where(p => p.PositionDepth >= targetDepth))
                {
                    entry.PositionDepth++;
                }

                var newEntry = new DepthChartEntry
                {
                    Id = GetNextId(data),
                    TeamId = teamId,
                    Position = position,
                    PlayerNumber = playerNumber,
                    PositionDepth = targetDepth
                };

                positionEntries.Add(newEntry);
                positionEntries.Sort((a, b) => a.PositionDepth.CompareTo(b.PositionDepth));

                await SaveTeamDataAsync(teamId, data, ct);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<Player?> RemovePlayerAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            await _fileLock.WaitAsync(ct);
            try
            {
                var data = await LoadTeamDataAsync(teamId, ct);
                var playersData = await LoadPlayersDataAsync(ct);

                if (!data.TryGetValue(position, out var positionEntries))
                {
                    return null;
                }

                var entryToRemove = positionEntries.FirstOrDefault(p => p.PlayerNumber == playerNumber);
                if (entryToRemove == null)
                {
                    return null;
                }

                var removedDepth = entryToRemove.PositionDepth;
                positionEntries.Remove(entryToRemove);

                foreach (var entry in positionEntries.Where(p => p.PositionDepth > removedDepth))
                {
                    entry.PositionDepth--;
                }

                await SaveTeamDataAsync(teamId, data, ct);

                var playerKey = $"{teamId}_{playerNumber}";
                return playersData.TryGetValue(playerKey, out var player) ? player :
                       new Player { TeamId = teamId, Number = playerNumber, Name = $"Player #{playerNumber}" };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            var data = await LoadTeamDataAsync(teamId, ct);
            var playersData = await LoadPlayersDataAsync(ct);

            if (!data.TryGetValue(position, out var positionEntries))
            {
                return new List<Player>();
            }

            var playerEntry = positionEntries.FirstOrDefault(p => p.PlayerNumber == playerNumber);
            if (playerEntry == null)
            {
                return new List<Player>();
            }

            var backups = positionEntries
                .Where(p => p.PositionDepth > playerEntry.PositionDepth)
                .OrderBy(p => p.PositionDepth)
                .Select(p => {
                    var playerKey = $"{teamId}_{p.PlayerNumber}";
                    return playersData.TryGetValue(playerKey, out var player) ? player :
                           new Player { TeamId = teamId, Number = p.PlayerNumber, Name = $"Player #{p.PlayerNumber}" };
                })
                .ToList();

            return backups;
        }

        public async Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct)
        {
            var data = await LoadTeamDataAsync(teamId, ct);
            var playersData = await LoadPlayersDataAsync(ct);

            var result = new Dictionary<string, List<Player>>();

            foreach (var (position, entries) in data)
            {
                var players = entries
                    .OrderBy(e => e.PositionDepth)
                    .Select(e => {
                        var playerKey = $"{teamId}_{e.PlayerNumber}";
                        return playersData.TryGetValue(playerKey, out var player) ? player :
                               new Player { TeamId = teamId, Number = e.PlayerNumber, Name = $"Player #{e.PlayerNumber}" };
                    })
                    .ToList();

                result[position] = players;
            }

            return result;
        }

        public async Task<IReadOnlyList<PositionMetadata>> GetPositionsAsync(string league, CancellationToken ct)
        {
            var positions = await LoadPositionsAsync(ct);
            return positions.Where(p => string.Equals(p.League, league, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task AddOrUpdatePlayerAsync(Player player, CancellationToken ct)
        {
            await _fileLock.WaitAsync(ct);
            try
            {
                var playersData = await LoadPlayersDataAsync(ct);
                var playerKey = player.GetUniqueId();
                playersData[playerKey] = player;
                await SavePlayersDataAsync(playersData, ct);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<Team>> GetTeamsAsync(CancellationToken ct)
        {
            if (!File.Exists(_teamsFilePath))
            {
                return new List<Team>();
            }

            var json = await File.ReadAllTextAsync(_teamsFilePath, ct);
            return JsonSerializer.Deserialize<List<Team>>(json, _jsonOptions) ?? new List<Team>();
        }

        private async Task<Dictionary<string, List<DepthChartEntry>>> LoadTeamDataAsync(string teamId, CancellationToken ct)
        {
            var filePath = GetTeamDepthChartFilePath(teamId);
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, List<DepthChartEntry>>();
            }

            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<Dictionary<string, List<DepthChartEntry>>>(json, _jsonOptions)
                   ?? new Dictionary<string, List<DepthChartEntry>>();
        }

        private async Task SaveTeamDataAsync(string teamId, Dictionary<string, List<DepthChartEntry>> data, CancellationToken ct)
        {
            var filePath = GetTeamDepthChartFilePath(teamId);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);
        }

        private async Task<Dictionary<string, Player>> LoadPlayersDataAsync(CancellationToken ct)
        {
            if (!File.Exists(_playersFilePath))
            {
                return new Dictionary<string, Player>();
            }

            var json = await File.ReadAllTextAsync(_playersFilePath, ct);
            var playersList = JsonSerializer.Deserialize<List<Player>>(json, _jsonOptions) ?? new List<Player>();

            return playersList.ToDictionary(p => p.GetUniqueId());
        }

        private async Task<List<PositionMetadata>> LoadPositionsAsync(CancellationToken ct)
        {
            if (!File.Exists(_positionsFilePath))
            {
                return new List<PositionMetadata>();
            }

            var json = await File.ReadAllTextAsync(_positionsFilePath, ct);
            return JsonSerializer.Deserialize<List<PositionMetadata>>(json, _jsonOptions) ?? new List<PositionMetadata>();
        }

        private async Task SavePlayersDataAsync(Dictionary<string, Player> playersData, CancellationToken ct)
        {
            var playersList = playersData.Values.OrderBy(p => p.TeamId).ThenBy(p => p.Number).ToList();
            var json = JsonSerializer.Serialize(playersList, _jsonOptions);
            await File.WriteAllTextAsync(_playersFilePath, json, ct);
        }

        private static List<DepthChartEntry> GetOrCreatePositionEntries(
            Dictionary<string, List<DepthChartEntry>> teamData,
            string position)
        {
            if (!teamData.TryGetValue(position, out var positionEntries))
            {
                positionEntries = new List<DepthChartEntry>();
                teamData[position] = positionEntries;
            }
            return positionEntries;
        }

        private static int GetNextId(Dictionary<string, List<DepthChartEntry>> data)
        {
            var maxId = 0;
            foreach (var positionEntries in data.Values)
            {
                if (positionEntries.Any())
                {
                    maxId = Math.Max(maxId, positionEntries.Max(e => e.Id));
                }
            }
            return maxId + 1;
        }
    }
}