using DepthCharts.Domain.Entities;
using DepthCharts.Infrastructure.Abstractions;
using System.Text.Json;

namespace DepthCharts.Infrastructure.Repositories
{
    public class JsonDepthChartRepository : IDepthChartRepository
    {
        private readonly string _dataFilePath;
        private readonly string _positionsFilePath;
        private readonly string _playersFilePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public JsonDepthChartRepository(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            _dataFilePath = Path.Combine(dataDirectory, "depthcharts.json");
            _positionsFilePath = Path.Combine(dataDirectory, "positions.json");
            _playersFilePath = Path.Combine(dataDirectory, "players.json");

            InitializeDataFiles();
        }

        public async Task AddPlayerAsync(string teamId, string position, int playerNumber, int? positionDepth, CancellationToken ct)
        {
            await _fileLock.WaitAsync(ct);
            try
            {
                var data = await LoadDataAsync(ct);
                var teamData = GetOrCreateTeamData(data, teamId);
                var positionEntries = GetOrCreatePositionEntries(teamData, position);

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

                await SaveDataAsync(data, ct);
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
                var data = await LoadDataAsync(ct);
                var playersData = await LoadPlayersDataAsync(ct);

                if (!data.TryGetValue(teamId, out var teamData) ||
                    !teamData.TryGetValue(position, out var positionEntries))
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

                await SaveDataAsync(data, ct);

                return playersData.TryGetValue(playerNumber, out var player) ? player :
                       new Player { Number = playerNumber, Name = $"Player #{playerNumber}" };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<Player>> GetBackupsAsync(string teamId, string position, int playerNumber, CancellationToken ct)
        {
            var data = await LoadDataAsync(ct);
            var playersData = await LoadPlayersDataAsync(ct);

            if (!data.TryGetValue(teamId, out var teamData) ||
                !teamData.TryGetValue(position, out var positionEntries))
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
                .Select(p => playersData.TryGetValue(p.PlayerNumber, out var player) ? player :
                           new Player { Number = p.PlayerNumber, Name = $"Player #{p.PlayerNumber}" })
                .ToList();

            return backups;
        }

        public async Task<Dictionary<string, List<Player>>> GetFullDepthChartAsync(string teamId, CancellationToken ct)
        {
            var data = await LoadDataAsync(ct);
            var playersData = await LoadPlayersDataAsync(ct);

            if (!data.TryGetValue(teamId, out var teamData))
            {
                return new Dictionary<string, List<Player>>();
            }

            var result = new Dictionary<string, List<Player>>();

            foreach (var (position, entries) in teamData)
            {
                var players = entries
                    .OrderBy(e => e.PositionDepth)
                    .Select(e => playersData.TryGetValue(e.PlayerNumber, out var player) ? player :
                               new Player { Number = e.PlayerNumber, Name = $"Player #{e.PlayerNumber}" })
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
                playersData[player.Number] = player;
                await SavePlayersDataAsync(playersData, ct);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<Dictionary<string, Dictionary<string, List<DepthChartEntry>>>> LoadDataAsync(CancellationToken ct)
        {
            if (!File.Exists(_dataFilePath))
            {
                return new Dictionary<string, Dictionary<string, List<DepthChartEntry>>>();
            }

            var json = await File.ReadAllTextAsync(_dataFilePath, ct);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<DepthChartEntry>>>>(json, _jsonOptions)
                   ?? new Dictionary<string, Dictionary<string, List<DepthChartEntry>>>();
        }

        private async Task<Dictionary<int, Player>> LoadPlayersDataAsync(CancellationToken ct)
        {
            if (!File.Exists(_playersFilePath))
            {
                return GetDefaultPlayers();
            }

            var json = await File.ReadAllTextAsync(_playersFilePath, ct);
            var playersList = JsonSerializer.Deserialize<List<Player>>(json, _jsonOptions) ?? new List<Player>();
            return playersList.ToDictionary(p => p.Number);
        }

        private async Task<List<PositionMetadata>> LoadPositionsAsync(CancellationToken ct)
        {
            if (!File.Exists(_positionsFilePath))
            {
                return GetDefaultPositions();
            }

            var json = await File.ReadAllTextAsync(_positionsFilePath, ct);
            return JsonSerializer.Deserialize<List<PositionMetadata>>(json, _jsonOptions) ?? GetDefaultPositions();
        }

        private async Task SaveDataAsync(Dictionary<string, Dictionary<string, List<DepthChartEntry>>> data, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, json, ct);
        }

        private async Task SavePlayersDataAsync(Dictionary<int, Player> playersData, CancellationToken ct)
        {
            var playersList = playersData.Values.OrderBy(p => p.Number).ToList();
            var json = JsonSerializer.Serialize(playersList, _jsonOptions);
            await File.WriteAllTextAsync(_playersFilePath, json, ct);
        }

        private static Dictionary<string, List<DepthChartEntry>> GetOrCreateTeamData(
            Dictionary<string, Dictionary<string, List<DepthChartEntry>>> data,
            string teamId)
        {
            if (!data.TryGetValue(teamId, out var teamData))
            {
                teamData = new Dictionary<string, List<DepthChartEntry>>();
                data[teamId] = teamData;
            }
            return teamData;
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

        private static int GetNextId(Dictionary<string, Dictionary<string, List<DepthChartEntry>>> data)
        {
            var maxId = 0;
            foreach (var teamData in data.Values)
            {
                foreach (var positionEntries in teamData.Values)
                {
                    if (positionEntries.Any())
                    {
                        maxId = Math.Max(maxId, positionEntries.Max(e => e.Id));
                    }
                }
            }
            return maxId + 1;
        }

        private static Dictionary<int, Player> GetDefaultPlayers()
        {
            // Based on the Tampa Bay Buccaneers depth chart
            var players = new List<Player>
            {
                // Quarterbacks
                new() { Number = 12, Name = "Tom Brady" },
                new() { Number = 11, Name = "Blaine Gabbert" },
                new() { Number = 2, Name = "Kyle Trask" },
                
                // Wide Receivers
                new() { Number = 13, Name = "Mike Evans" },
                new() { Number = 14, Name = "Chris Godwin" },
                new() { Number = 1, Name = "Jaelon Darden" },
                new() { Number = 10, Name = "Scott Miller" },
                new() { Number = 18, Name = "Tyler Johnson" },
                new() { Number = 16, Name = "Breshad Perriman" },
                
                // Running Backs
                new() { Number = 7, Name = "Leonard Fournette" },
                new() { Number = 27, Name = "Ronald Jones II" },
                new() { Number = 21, Name = "Ke'Shawn Vaughn" },
                new() { Number = 25, Name = "Giovani Bernard" },
                
                // Tight Ends
                new() { Number = 87, Name = "Rob Gronkowski" },
                new() { Number = 80, Name = "OJ Howard" },
                new() { Number = 84, Name = "Cameron Brate" },
                
                // Offensive Line
                new() { Number = 76, Name = "Donovan Smith" },
                new() { Number = 74, Name = "Ali Marpet" },
                new() { Number = 66, Name = "Ryan Jensen" },
                new() { Number = 65, Name = "Alex Cappa" },
                new() { Number = 78, Name = "Tristan Wirfs" },
                new() { Number = 72, Name = "Josh Wells" },
                
                // Defense
                new() { Number = 93, Name = "Ndamukong Suh" },
                new() { Number = 50, Name = "Vita Vea" },
                new() { Number = 90, Name = "Jason Pierre-Paul" },
                new() { Number = 45, Name = "Devin White" },
                new() { Number = 54, Name = "Lavonte David" },
                new() { Number = 58, Name = "Shaquil Barrett" },
                new() { Number = 24, Name = "Carlton Davis" },
                new() { Number = 31, Name = "Antoine Winfield Jr." },
                new() { Number = 33, Name = "Jordan Whitehead" },
                new() { Number = 23, Name = "Sean Murphy-Bunting" },
                
                // Special Teams
                new() { Number = 8, Name = "Bradley Pinion" },
                new() { Number = 3, Name = "Ryan Succop" },
                new() { Number = 97, Name = "Zach Triner" }
            };

            return players.ToDictionary(p => p.Number);
        }

        private static List<PositionMetadata> GetDefaultPositions()
        {
            return new List<PositionMetadata>
            {
                // Offense
                new() { League = "NFL", Code = "QB", Name = "Quarterback", Group = "Offense", SortOrder = 1 },
                new() { League = "NFL", Code = "RB", Name = "Running Back", Group = "Offense", SortOrder = 2 },
                new() { League = "NFL", Code = "FB", Name = "Fullback", Group = "Offense", SortOrder = 3 },
                new() { League = "NFL", Code = "LWR", Name = "Left Wide Receiver", Group = "Offense", SortOrder = 10 },
                new() { League = "NFL", Code = "RWR", Name = "Right Wide Receiver", Group = "Offense", SortOrder = 11 },
                new() { League = "NFL", Code = "SWR", Name = "Slot Wide Receiver", Group = "Offense", SortOrder = 12 },
                new() { League = "NFL", Code = "TE", Name = "Tight End", Group = "Offense", SortOrder = 15 },
                new() { League = "NFL", Code = "LT", Name = "Left Tackle", Group = "Offense", SortOrder = 20 },
                new() { League = "NFL", Code = "LG", Name = "Left Guard", Group = "Offense", SortOrder = 21 },
                new() { League = "NFL", Code = "C", Name = "Center", Group = "Offense", SortOrder = 22 },
                new() { League = "NFL", Code = "RG", Name = "Right Guard", Group = "Offense", SortOrder = 23 },
                new() { League = "NFL", Code = "RT", Name = "Right Tackle", Group = "Offense", SortOrder = 24 },
                
                // Defense
                new() { League = "NFL", Code = "DE", Name = "Defensive End", Group = "Defense", SortOrder = 30 },
                new() { League = "NFL", Code = "DT", Name = "Defensive Tackle", Group = "Defense", SortOrder = 31 },
                new() { League = "NFL", Code = "NT", Name = "Nose Tackle", Group = "Defense", SortOrder = 32 },
                new() { League = "NFL", Code = "OLB", Name = "Outside Linebacker", Group = "Defense", SortOrder = 35 },
                new() { League = "NFL", Code = "ILB", Name = "Inside Linebacker", Group = "Defense", SortOrder = 36 },
                new() { League = "NFL", Code = "LB", Name = "Linebacker", Group = "Defense", SortOrder = 37 },
                new() { League = "NFL", Code = "CB", Name = "Cornerback", Group = "Defense", SortOrder = 40 },
                new() { League = "NFL", Code = "RCB", Name = "Right Cornerback", Group = "Defense", SortOrder = 41 },
                new() { League = "NFL", Code = "FS", Name = "Free Safety", Group = "Defense", SortOrder = 45 },
                new() { League = "NFL", Code = "SS", Name = "Strong Safety", Group = "Defense", SortOrder = 46 },
                
                // Special Teams
                new() { League = "NFL", Code = "K", Name = "Kicker", Group = "Special Teams", SortOrder = 50 },
                new() { League = "NFL", Code = "PK", Name = "Place Kicker", Group = "Special Teams", SortOrder = 50 },
                new() { League = "NFL", Code = "P", Name = "Punter", Group = "Special Teams", SortOrder = 51 },
                new() { League = "NFL", Code = "PT", Name = "Punter", Group = "Special Teams", SortOrder = 51 },
                new() { League = "NFL", Code = "LS", Name = "Long Snapper", Group = "Special Teams", SortOrder = 52 },
                new() { League = "NFL", Code = "KR", Name = "Kick Returner", Group = "Special Teams", SortOrder = 53 },
                new() { League = "NFL", Code = "PR", Name = "Punt Returner", Group = "Special Teams", SortOrder = 54 },
                new() { League = "NFL", Code = "H", Name = "Holder", Group = "Special Teams", SortOrder = 55 },
                new() { League = "NFL", Code = "KO", Name = "Kickoff Specialist", Group = "Special Teams", SortOrder = 56 }
            };
        }

        private void InitializeDataFiles()
        {
            if (!File.Exists(_positionsFilePath))
            {
                var defaultPositions = GetDefaultPositions();
                var json = JsonSerializer.Serialize(defaultPositions, _jsonOptions);
                File.WriteAllText(_positionsFilePath, json);
            }

            if (!File.Exists(_playersFilePath))
            {
                var defaultPlayers = GetDefaultPlayers().Values.OrderBy(p => p.Number).ToList();
                var json = JsonSerializer.Serialize(defaultPlayers, _jsonOptions);
                File.WriteAllText(_playersFilePath, json);
            }
        }
    }
}