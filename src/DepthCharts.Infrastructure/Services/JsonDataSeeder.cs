using DepthCharts.Infrastructure.Abstractions;
using DepthCharts.Infrastructure.Configuration;
using DepthCharts.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DepthCharts.Domain.Abstractions;

namespace DepthCharts.Infrastructure.Services
{
    public class JsonDataSeeder : IDataSeeder
    {
        private readonly IDepthChartRepository _repository;
        private readonly ILogger<JsonDataSeeder> _logger;
        private readonly string _dataDirectory;
        private readonly string _teamsFilePath;
        private readonly string _positionsFilePath;
        private readonly string _playersFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public JsonDataSeeder(
            IDepthChartRepository repository,
            ILogger<JsonDataSeeder> logger,
            IOptions<DataSettings> dataSettings)
        {
            _repository = repository;
            _logger = logger;
            _dataDirectory = dataSettings.Value.GetFullDataPath();

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _teamsFilePath = Path.Combine(_dataDirectory, "teams.json");
            _positionsFilePath = Path.Combine(_dataDirectory, "positions.json");
            _playersFilePath = Path.Combine(_dataDirectory, "players.json");
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");

                await InitializeDataFilesAsync();

                var teams = await LoadTeamsAsync();
                foreach (var team in teams)
                {
                    await SeedTeamAsync(team);
                }

                _logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed depth chart data");
                throw;
            }
        }

        private async Task InitializeDataFilesAsync()
        {
            // Initialize positions file
            if (!File.Exists(_positionsFilePath))
            {
                _logger.LogInformation("Creating positions file...");
                var defaultPositions = GetDefaultPositions();
                var json = JsonSerializer.Serialize(defaultPositions, _jsonOptions);
                await File.WriteAllTextAsync(_positionsFilePath, json);
            }

            // Initialize players file
            if (!File.Exists(_playersFilePath))
            {
                _logger.LogInformation("Creating players file...");
                var defaultPlayers = GetDefaultPlayers().Values.OrderBy(p => p.Number).ToList();
                var json = JsonSerializer.Serialize(defaultPlayers, _jsonOptions);
                await File.WriteAllTextAsync(_playersFilePath, json);
            }

            // Initialize teams file
            if (!File.Exists(_teamsFilePath))
            {
                _logger.LogInformation("Creating teams file...");
                var defaultTeams = GetDefaultTeams();
                await SaveTeamsAsync(defaultTeams);
            }
        }

        private async Task<List<Team>> LoadTeamsAsync()
        {
            var json = await File.ReadAllTextAsync(_teamsFilePath);
            return JsonSerializer.Deserialize<List<Team>>(json, _jsonOptions) ?? GetDefaultTeams();
        }

        private async Task SaveTeamsAsync(List<Team> teams)
        {
            var json = JsonSerializer.Serialize(teams, _jsonOptions);
            await File.WriteAllTextAsync(_teamsFilePath, json);
        }

        private async Task SeedTeamAsync(Team team)
        {
            var existingChart = await _repository.GetFullDepthChartAsync(team.Id, CancellationToken.None);
            if (existingChart.Any())
            {
                _logger.LogInformation("{TeamName} depth chart already exists, skipping seed", team.Name);
                return;
            }

            _logger.LogInformation("Seeding {TeamName} depth chart...", team.Name);

            if (team.Id == "TB")
            {
                await SeedTampaBayAsync(team.Id);
            }
            else
            {
                await _repository.AddPlayerAsync(team.Id, "QB", 1, 0, CancellationToken.None);
                _logger.LogInformation("Seeded placeholder data for {TeamName}", team.Name);
            }
        }

        private async Task SeedTampaBayAsync(string teamId)
        {
            // Quarterbacks
            await _repository.AddPlayerAsync(teamId, "QB", 12, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "QB", 11, 1, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "QB", 2, 2, CancellationToken.None);

            // Wide Receivers
            await _repository.AddPlayerAsync(teamId, "LWR", 13, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "LWR", 1, 1, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "LWR", 10, 2, CancellationToken.None);

            // Running Backs
            await _repository.AddPlayerAsync(teamId, "RB", 7, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "RB", 27, 1, CancellationToken.None);

            // Tight Ends
            await _repository.AddPlayerAsync(teamId, "TE", 87, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "TE", 84, 1, CancellationToken.None);

            // Linebackers
            await _repository.AddPlayerAsync(teamId, "LB", 45, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "LB", 54, 1, CancellationToken.None);

            // Cornerbacks
            await _repository.AddPlayerAsync(teamId, "CB", 24, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "CB", 23, 1, CancellationToken.None);

            // Special Teams
            await _repository.AddPlayerAsync(teamId, "K", 3, 0, CancellationToken.None);
            await _repository.AddPlayerAsync(teamId, "P", 8, 0, CancellationToken.None);

            _logger.LogInformation("Successfully seeded Tampa Bay Buccaneers depth chart");
        }

        private static List<Team> GetDefaultTeams()
        {
            return new List<Team>
            {
                new() { Id = "TB", Name = "Tampa Bay Buccaneers", League = "NFL" },
                new() { Id = "NE", Name = "New England Patriots", League = "NFL" },
                new() { Id = "KC", Name = "Kansas City Chiefs", League = "NFL" },
                new() { Id = "GB", Name = "Green Bay Packers", League = "NFL" },
                new() { Id = "BUF", Name = "Buffalo Bills", League = "NFL" },
                new() { Id = "LAR", Name = "Los Angeles Rams", League = "NFL" },
                new() { Id = "DAL", Name = "Dallas Cowboys", League = "NFL" },
                new() { Id = "SF", Name = "San Francisco 49ers", League = "NFL" }
            };
        }

        private static Dictionary<string, Player> GetDefaultPlayers()
        {
            var players = new List<Player>
            {
                // Tampa Bay Buccaneers (TB)
                new() { TeamId = "TB", Number = 12, Name = "Tom Brady" },
                new() { TeamId = "TB", Number = 11, Name = "Blaine Gabbert" },
                new() { TeamId = "TB", Number = 2, Name = "Kyle Trask" },
                new() { TeamId = "TB", Number = 13, Name = "Mike Evans" },
                new() { TeamId = "TB", Number = 14, Name = "Chris Godwin" },
                new() { TeamId = "TB", Number = 1, Name = "Jaelon Darden" },
                new() { TeamId = "TB", Number = 10, Name = "Scott Miller" },
                new() { TeamId = "TB", Number = 7, Name = "Leonard Fournette" },
                new() { TeamId = "TB", Number = 27, Name = "Ronald Jones II" },
                new() { TeamId = "TB", Number = 87, Name = "Rob Gronkowski" },
                new() { TeamId = "TB", Number = 84, Name = "Cameron Brate" },
                new() { TeamId = "TB", Number = 45, Name = "Devin White" },
                new() { TeamId = "TB", Number = 54, Name = "Lavonte David" },
                new() { TeamId = "TB", Number = 24, Name = "Carlton Davis" },
                new() { TeamId = "TB", Number = 23, Name = "Sean Murphy-Bunting" },
                new() { TeamId = "TB", Number = 3, Name = "Ryan Succop" },
                new() { TeamId = "TB", Number = 8, Name = "Bradley Pinion" },

                // New England Patriots (NE)
                new() { TeamId = "NE", Number = 12, Name = "Mac Jones" }, 
                new() { TeamId = "NE", Number = 1, Name = "Cam Newton" },
                new() { TeamId = "NE", Number = 87, Name = "Rob Gronkowski" },
        
                // Kansas City Chiefs (KC)
                new() { TeamId = "KC", Number = 15, Name = "Patrick Mahomes" },
                new() { TeamId = "KC", Number = 87, Name = "Travis Kelce" }
            };

            return players.ToDictionary(p => p.GetUniqueId());
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
    }
}