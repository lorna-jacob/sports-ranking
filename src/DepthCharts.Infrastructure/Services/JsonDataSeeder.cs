using DepthCharts.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace DepthCharts.Infrastructure.Services
{
    public class JsonDataSeeder : IDataSeeder
    {
        private readonly IDepthChartRepository _repository;
        private readonly ILogger<JsonDataSeeder> _logger;

        public JsonDataSeeder(IDepthChartRepository repository, ILogger<JsonDataSeeder> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");

                var existingChart = await _repository.GetFullDepthChartAsync("TB", CancellationToken.None);
                if (existingChart.Any())
                {
                    _logger.LogInformation("Tampa Bay depth chart already exists, skipping seed");
                    return;
                }

                _logger.LogInformation("Seeding Tampa Bay Buccaneers depth chart...");

                await _repository.AddPlayerAsync("TB", "QB", 12, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "QB", 11, 1, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "QB", 2, 2, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "LWR", 13, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "LWR", 1, 1, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "LWR", 10, 2, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "RB", 7, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "RB", 27, 1, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "TE", 87, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "TE", 84, 1, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "LB", 45, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "LB", 54, 1, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "CB", 24, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "CB", 23, 1, CancellationToken.None);

                await _repository.AddPlayerAsync("TB", "K", 3, 0, CancellationToken.None);
                await _repository.AddPlayerAsync("TB", "P", 8, 0, CancellationToken.None);

                _logger.LogInformation("Successfully seeded Tampa Bay Buccaneers depth chart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed depth chart data");
                throw;
            }
        }
    }
}
