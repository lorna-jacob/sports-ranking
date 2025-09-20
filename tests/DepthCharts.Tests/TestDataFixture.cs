using System.Text.Json;
using DepthCharts.Domain.Entities;

namespace DepthCharts.Tests
{
    public class TestDataFixture : IDisposable
    {
        public string TestDataDirectory { get; }

        public TestDataFixture()
        {
            TestDataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            SetupTestData();
        }

        private void SetupTestData()
        {
            Directory.CreateDirectory(TestDataDirectory);
            var positions = new List<PositionMetadata>
            {
                new() { League = "NFL", Code = "QB", Name = "Quarterback", Group = "Offense", SortOrder = 1 },
                new() { League = "NFL", Code = "RB", Name = "Running Back", Group = "Offense", SortOrder = 2 },
                new() { League = "NFL", Code = "LWR", Name = "Left Wide Receiver", Group = "Offense", SortOrder = 10 },
                new() { League = "NFL", Code = "TE", Name = "Tight End", Group = "Offense", SortOrder = 15 },
                new() { League = "NFL", Code = "CB", Name = "Cornerback", Group = "Defense", SortOrder = 40 },
                new() { League = "NFL", Code = "K", Name = "Kicker", Group = "Special Teams", SortOrder = 50 },
                new() { League = "NBA", Code = "PG", Name = "Point Guard", Group = "Backcourt", SortOrder = 1 },
                new() { League = "NBA", Code = "SG", Name = "Shooting Guard", Group = "Backcourt", SortOrder = 2 }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var positionsJson = JsonSerializer.Serialize(positions, jsonOptions);
            var positionsFilePath = Path.Combine(TestDataDirectory, "positions.json");
            File.WriteAllText(positionsFilePath, positionsJson);

            var teamsJson = JsonSerializer.Serialize(new List<Team>(), jsonOptions);
            var teamsFilePath = Path.Combine(TestDataDirectory, "teams.json");
            File.WriteAllText(teamsFilePath, teamsJson);

            var playersJson = JsonSerializer.Serialize(new List<Player>(), jsonOptions);
            var playersFilePath = Path.Combine(TestDataDirectory, "players.json");
            File.WriteAllText(playersFilePath, playersJson);
        }        

        public void Dispose()
        {
            if (Directory.Exists(TestDataDirectory))
            {
                Directory.Delete(TestDataDirectory, true);
            }
        }
    }
}