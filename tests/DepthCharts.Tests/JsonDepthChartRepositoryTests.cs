using DepthCharts.Infrastructure.Configuration;
using DepthCharts.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace DepthCharts.Tests
{
    public class JsonDepthChartRepositoryTests : IAsyncLifetime, IDisposable
    {
        private TestDataFixture _fixture;
        private JsonDepthChartRepository _repository;

        public JsonDepthChartRepositoryTests() 
        { 
            
        }

        public async Task InitializeAsync()
        {
            _fixture = new TestDataFixture();

            var dataSettings = new DataSettings
            {
                DataDirectory = _fixture.TestDataDirectory
            };
            var options = Options.Create(dataSettings);

            _repository = new JsonDepthChartRepository(options);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _fixture?.Dispose();
        }

        [Fact]
        public async Task AddPlayerAsync_WithNewPlayer_AddsToEndOfDepthChart()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var playerNumber = 12;
            var ct = CancellationToken.None;

            // Act
            await _repository.AddPlayerAsync(teamId, position, playerNumber, null, ct);
            var depthChart = await _repository.GetFullDepthChartAsync(teamId, ct);

            // Assert            
            Assert.True(depthChart.ContainsKey(position));
            Assert.Single(depthChart[position]);
            Assert.Equal(playerNumber, depthChart[position][0].Number);
        }

        [Fact]
        public async Task AddPlayerAsync_WithSpecificDepth_InsertsAtCorrectPosition()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var ct = CancellationToken.None;

            // Add players in order
            await _repository.AddPlayerAsync(teamId, position, 12, 0, ct);
            await _repository.AddPlayerAsync(teamId, position, 11, 1, ct);
            await _repository.AddPlayerAsync(teamId, position, 2, 2, ct);

            // Act
            await _repository.AddPlayerAsync(teamId, position, 99, 1, ct);

            // Assert
            var depthChart = await _repository.GetFullDepthChartAsync(teamId, ct);
            var qbPlayers = depthChart[position];
            
            Assert.Equal(4, qbPlayers.Count);
            Assert.Equal(12, qbPlayers[0].Number);
            Assert.Equal(99, qbPlayers[1].Number);
            Assert.Equal(11, qbPlayers[2].Number); 
            Assert.Equal(2, qbPlayers[3].Number);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(999)]
        public async Task AddPlayer_WithExtremeDepth_ClampsDepthToValidRange(int requestedDepth)
        {
            // Arrange
            var ct = CancellationToken.None;
            await _repository.AddPlayerAsync("TB", "QB", 12, 0, ct); // starter
            await _repository.AddPlayerAsync("TB", "QB", 11, requestedDepth, ct); // clamp: should append at end (=1)

            // Act
            var chart = await _repository.GetFullDepthChartAsync("TB", ct);
            
            // Assert
            var list = chart["QB"];
            Assert.Equal(2, list.Count);
            Assert.Equal(12, list[0].Number);
            Assert.Equal(11, list[1].Number);
        }

        [Fact]
        public async Task AddPlayer_WhenReAddExistingPlayer_MovesWithoutDuplicate()
        {
            // Arrange
            var ct = CancellationToken.None;
            await _repository.AddPlayerAsync("TB", "QB", 12, 0, ct);
            await _repository.AddPlayerAsync("TB", "QB", 11, 1, ct);

            await _repository.AddPlayerAsync("TB", "QB", 12, 1, ct);

            // Act
            var chart = await _repository.GetFullDepthChartAsync("TB", ct);

            // Assert
            var list = chart["QB"];
            Assert.Equal(2, list.Count); //still only 2 players
        }


        [Fact]
        public async Task RemovePlayerAsync_WithExistingPlayer_ReturnsPlayerAndRemovesFromChart()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var ct = CancellationToken.None;

            await _repository.AddPlayerAsync(teamId, position, 12, 0, ct);
            await _repository.AddPlayerAsync(teamId, position, 11, 1, ct);
            await _repository.AddPlayerAsync(teamId, position, 2, 2, ct);

            // Act
            var removedPlayer = await _repository.RemovePlayerAsync(teamId, position, 11, ct);

            // Assert
            Assert.NotNull(removedPlayer);
            Assert.Equal(11, removedPlayer.Number);

            var depthChart = await _repository.GetFullDepthChartAsync(teamId, ct);
            var qbPlayers = depthChart[position];
            
            Assert.Equal(2, qbPlayers.Count);
            Assert.Equal(12, qbPlayers[0].Number);
            Assert.Equal(2, qbPlayers[1].Number);
        }

        [Fact]
        public async Task RemovePlayerAsync_WithNonExistentPlayer_ReturnsNull()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var ct = CancellationToken.None;

            // Act
            var removedPlayer = await _repository.RemovePlayerAsync(teamId, position, 99, ct);

            // Assert
            Assert.Null(removedPlayer);
        }

        [Fact]
        public async Task GetBackupsAsync_WithValidPlayer_ReturnsCorrectBackups()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var ct = CancellationToken.None;

            await _repository.AddPlayerAsync(teamId, position, 12, 0, ct);
            await _repository.AddPlayerAsync(teamId, position, 11, 1, ct);
            await _repository.AddPlayerAsync(teamId, position, 2, 2, ct); 

            // Act
            var backups = await _repository.GetBackupsAsync(teamId, position, 12, ct);

            // Assert
            Assert.Equal(2, backups.Count);
            Assert.Equal(11, backups[0].Number);
            Assert.Equal(2, backups[1].Number);
        }

        [Fact]
        public async Task GetBackupsAsync_WithLastPlayer_ReturnsEmptyList()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var ct = CancellationToken.None;

            await _repository.AddPlayerAsync(teamId, position, 12, 0, ct);
            await _repository.AddPlayerAsync(teamId, position, 2, 1, ct);

            // Act
            var backups = await _repository.GetBackupsAsync(teamId, position, 2, ct);

            // Assert
            Assert.Empty(backups);
        }

        [Fact]
        public async Task GetBackups_ForUnknownPlayer_ReturnsEmpty()
        {
            // Arrange
            var ct = CancellationToken.None;
            await _repository.AddPlayerAsync("TB", "QB", 12, 0, ct);

            // Act
            var backups = await _repository.GetBackupsAsync("TB", "QB", 99, ct);

            // Assert
            Assert.Empty(backups);
        }

        [Fact]
        public async Task GetPositionsAsync_WithNFLLeague_ReturnsNFLPositions()
        {
            // Arrange
            var league = "NFL";
            var ct = CancellationToken.None;

            // Act
            var positions = await _repository.GetPositionsAsync(league, ct);

            // Assert
            Assert.NotEmpty(positions);
            Assert.All(positions, p => Assert.Equal("NFL", p.League));
            
            Assert.Contains(positions, p => p.Code == "QB");
            Assert.Contains(positions, p => p.Code == "RB");
            Assert.Contains(positions, p => p.Code == "LWR");
        }

        [Fact]
        public async Task GetFullDepthChartAsync_WithMultiplePositions_ReturnsCompleteChart()
        {
            // Arrange
            var teamId = "TB";
            var ct = CancellationToken.None;

            await _repository.AddPlayerAsync(teamId, "QB", 12, 0, ct);
            await _repository.AddPlayerAsync(teamId, "QB", 11, 1, ct); 
            await _repository.AddPlayerAsync(teamId, "LWR", 13, 0, ct);
            await _repository.AddPlayerAsync(teamId, "LWR", 1, 1, ct);

            // Act
            var depthChart = await _repository.GetFullDepthChartAsync(teamId, ct);

            // Assert
            Assert.Equal(2, depthChart.Count);
            Assert.True(depthChart.ContainsKey("QB"));
            Assert.True(depthChart.ContainsKey("LWR"));
            
            Assert.Equal(2, depthChart["QB"].Count);
            Assert.Equal(2, depthChart["LWR"].Count);
        }        
    }
}
