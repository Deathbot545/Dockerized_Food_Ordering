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

            var client = new MongoClient(settings.Value.ConnectionString);
            if (client != null)
            {
                _logger.LogInformation("MongoDB connection successful.");
                _database = client.GetDatabase(settings.Value.DatabaseName);
            }
            else
            {
                _logger.LogError("MongoDB connection failed.");
            }
        }

        public IMongoCollection<Order> Orders
        {
            get
            {
                return _database.GetCollection<Order>("Orders");
            }
        }
    }
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

}
