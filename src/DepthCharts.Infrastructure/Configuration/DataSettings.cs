namespace DepthCharts.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for data storage
    /// </summary>
    public class DataSettings
    {
        /// <summary>
        /// The configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "DataSettings";

        /// <summary>
        /// Directory where data files are stored
        /// </summary>
        public string DataDirectory { get; set; } = "Data";

        /// <summary>
        /// Gets the full path to the data directory
        /// </summary>
        public string GetFullDataPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), DataDirectory);
        }
    }
}