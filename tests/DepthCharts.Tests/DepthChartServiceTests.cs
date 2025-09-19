using DepthCharts.Application.Abstractions;
using DepthCharts.Application.Common;
using DepthCharts.Application.Services;
using DepthCharts.Domain.Entities;
using DepthCharts.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DepthCharts.Tests
{
    public class DepthChartServiceTests
    {
        private readonly Mock<IDepthChartRepository> _mockRepository;
        private readonly Mock<ILogger<DepthChartService>> _mockLogger;
        private readonly IDepthChartService _service;

        public DepthChartServiceTests()
        {
            _mockRepository = new Mock<IDepthChartRepository>();
            _mockLogger = new Mock<ILogger<DepthChartService>>();
            _service = new DepthChartService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddPlayerAsync_WithValidInputs_CallsRepositoryMethods()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var player = new Player { Number = 12, Name = "Tom Brady" };
            var positionDepth = 0;
            var ct = CancellationToken.None;

            // Act
            await _service.AddPlayerAsync(teamId, position, player, positionDepth, ct);

            // Assert
            _mockRepository.Verify(r => r.RemovePlayerAsync(teamId, "QB", 12, ct), Times.Once);
            _mockRepository.Verify(r => r.AddPlayerAsync(teamId, "QB", 12, positionDepth, ct), Times.Once);
        }

        [Fact]
        public async Task AddPlayerAsync_WithNullPositionDepth_CallsRepositoryWithNull()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var player = new Player { Number = 12, Name = "Tom Brady" };
            int? positionDepth = null;
            var ct = CancellationToken.None;

            // Act
            await _service.AddPlayerAsync(teamId, position, player, positionDepth, ct);

            // Assert
            _mockRepository.Verify(r => r.AddPlayerAsync(teamId, "QB", 12, null, ct), Times.Once);
        }

        [Theory]
        [InlineData("", "teamId")]
        [InlineData(null, "teamId")]
        [InlineData("  ", "teamId")]
        public async Task AddPlayerAsync_WithInvalidTeamId_ThrowsValidationException(string teamId, string expectedParamName)
        {
            // Arrange
            var position = "QB";
            var player = new Player { Number = 12, Name = "Tom Brady" };
            var ct = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddPlayerAsync(teamId, position, player, 0, ct));
            
            Assert.Contains(expectedParamName, exception.Message);
        }

        [Theory]
        [InlineData("", "position")]
        [InlineData(null, "position")]
        [InlineData("  ", "position")]
        public async Task AddPlayerAsync_WithInvalidPosition_ThrowsValidationException(string position, string expectedParamName)
        {
            // Arrange
            var teamId = "TB";
            var player = new Player { Number = 12, Name = "Tom Brady" };
            var ct = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddPlayerAsync(teamId, position, player, 0, ct));
            
            Assert.Contains(expectedParamName, exception.Message);
        }

        [Fact]
        public async Task AddPlayerAsync_WithNegativePlayerNumber_ThrowsValidationException()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var player = new Player { Number = -1, Name = "Tom Brady" };
            var ct = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddPlayerAsync(teamId, position, player, 0, ct));
            
            Assert.Contains("player.Number", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("  ")]
        public async Task AddPlayerAsync_WithInvalidPlayerName_ThrowsValidationException(string playerName)
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var player = new Player { Number = 12, Name = playerName };
            var ct = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddPlayerAsync(teamId, position, player, 0, ct));
            
            Assert.Contains("player.Name", exception.Message);
        }

        [Fact]
        public async Task RemoveAsync_WithValidInputs_ReturnsPlayerFromRepository()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var playerNumber = 12;
            var expectedPlayer = new Player { Number = 12, Name = "Tom Brady" };
            var ct = CancellationToken.None;

            _mockRepository.Setup(r => r.RemovePlayerAsync(teamId, "QB", playerNumber, ct))
                          .ReturnsAsync(expectedPlayer);

            // Act
            var result = await _service.RemoveAsync(teamId, position, playerNumber, ct);

            // Assert
            Assert.Equal(expectedPlayer, result);
            _mockRepository.Verify(r => r.RemovePlayerAsync(teamId, "QB", playerNumber, ct), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_WithPlayerNotFound_ReturnsNull()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var playerNumber = 99;
            var ct = CancellationToken.None;

            _mockRepository.Setup(r => r.RemovePlayerAsync(teamId, "QB", playerNumber, ct))
                          .ReturnsAsync((Player?)null);

            // Act
            var result = await _service.RemoveAsync(teamId, position, playerNumber, ct);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBackupsAsync_WithValidInputs_ReturnsBackupsFromRepository()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var playerNumber = 12;
            var expectedBackups = new List<Player>
            {
                new Player { Number = 11, Name = "Blaine Gabbert" },
                new Player { Number = 2, Name = "Kyle Trask" }
            };
            var ct = CancellationToken.None;

            _mockRepository.Setup(r => r.GetBackupsAsync(teamId, "QB", playerNumber, ct))
                          .ReturnsAsync(expectedBackups);

            // Act
            var result = await _service.GetBackupsAsync(teamId, position, playerNumber, ct);

            // Assert
            Assert.Equal(expectedBackups, result);
            _mockRepository.Verify(r => r.GetBackupsAsync(teamId, "QB", playerNumber, ct), Times.Once);
        }

        [Fact]
        public async Task GetBackupsAsync_WithNoBackups_ReturnsEmptyList()
        {
            // Arrange
            var teamId = "TB";
            var position = "QB";
            var playerNumber = 2;
            var ct = CancellationToken.None;

            _mockRepository.Setup(r => r.GetBackupsAsync(teamId, "QB", playerNumber, ct))
                          .ReturnsAsync(new List<Player>());

            // Act
            var result = await _service.GetBackupsAsync(teamId, position, playerNumber, ct);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFullDepthChartAsync_GroupsPositionsByMetadata()
        {
            // Arrange
            var teamId = "TB";
            var league = "NFL";
            var ct = CancellationToken.None;

            var depthChart = new Dictionary<string, List<Player>>
            {
                {
                    "QB",
                    new List<Player>
                    {
                        new Player { Number = 12, Name = "Tom Brady" }
                    }
                },
                {
                    "LWR",
                    new List<Player>
                    {
                        new Player { Number = 13, Name = "Mike Evans" }
                    }
                },
                {
                    "RWR",
                    new List<Player>
                    {
                        new Player { Number = 14, Name = "Chris Godwin" }
                    }
                }
            };

            var positionsMetadata = new List<PositionMetadata>
            {
                new PositionMetadata { Code = "QB", Name = "Quarterback", Group = "Offense", SortOrder = 1 },
                new PositionMetadata { Code = "LWR", Name = "Left Wide Receiver", Group = "Offense", SortOrder = 10 },
                new PositionMetadata { Code = "RWR", Name = "Right Wide Receiver", Group = "Offense", SortOrder = 11 }
            };

            _mockRepository.Setup(r => r.GetFullDepthChartAsync(teamId, ct))
                          .ReturnsAsync(depthChart);
            _mockRepository.Setup(r => r.GetPositionsAsync(league, ct))
                          .ReturnsAsync(positionsMetadata);

            // Act
            var result = await _service.GetFullDepthChartAsync(teamId, league, ct);

            // Assert
            Assert.Single(result); // One group: "Offense"
            Assert.True(result.ContainsKey("Offense"));
            
            var offenseGroup = result["Offense"];
            Assert.Equal(3, offenseGroup.Count);
            
            // Verify positions are sorted by SortOrder
            Assert.Equal("QB", offenseGroup[0].position);
            Assert.Equal("LWR", offenseGroup[1].position);
            Assert.Equal("RWR", offenseGroup[2].position);
        }

        [Fact]
        public async Task GetFullDepthChartAsync_HandlesUnknownPositions()
        {
            // Arrange
            var teamId = "TB";
            var league = "NFL";
            var ct = CancellationToken.None;

            var depthChart = new Dictionary<string, List<Player>>
            {
                {
                    "UNKNOWN",
                    new List<Player>
                    {
                        new Player { Number = 99, Name = "Unknown Player" }
                    }
                }
            };

            var positionsMetadata = new List<PositionMetadata>();

            _mockRepository.Setup(r => r.GetFullDepthChartAsync(teamId, ct))
                          .ReturnsAsync(depthChart);
            _mockRepository.Setup(r => r.GetPositionsAsync(league, ct))
                          .ReturnsAsync(positionsMetadata);

            // Act
            var result = await _service.GetFullDepthChartAsync(teamId, league, ct);

            // Assert
            Assert.Single(result); // One group: "Other"
            Assert.True(result.ContainsKey("Other"));
            
            var otherGroup = result["Other"];
            Assert.Single(otherGroup);
            Assert.Equal("UNKNOWN", otherGroup[0].position);
        }

        [Theory]
        [InlineData("  qb  ", "QB")]
        [InlineData("lwr", "LWR")]
        public async Task ServiceMethods_NormalizePositionNames(string inputPosition, string expectedNormalizedPosition)
        {
            // Arrange
            var teamId = "TB";
            var player = new Player { Number = 12, Name = "Test Player" };
            var ct = CancellationToken.None;

            // Act
            await _service.AddPlayerAsync(teamId, inputPosition, player, 0, ct);

            // Assert
            _mockRepository.Verify(r => r.AddPlayerAsync(teamId, expectedNormalizedPosition, 12, 0, ct), Times.Once);
        }
    }
}