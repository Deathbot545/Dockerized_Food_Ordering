using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Order_API.Models;

namespace Order_API.Data
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database = null;
        private readonly ILogger<MongoDBContext> _logger;

        public MongoDBContext(IOptions<MongoDBSettings> settings, ILogger<MongoDBContext> logger)
        {
            _logger = logger;

            var connectionString = settings?.Value?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("MongoDB connection string is null or empty.");
                throw new ArgumentNullException(nameof(settings), "MongoDB connection string cannot be null.");
            }

            try
            {
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(settings.Value.DatabaseName);
                _logger.LogInformation("MongoDB connection successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB connection failed.");
                throw;
            }
        }

        public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
