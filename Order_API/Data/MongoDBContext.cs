using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Order_API.Models;
using System.Security.Cryptography.X509Certificates;

namespace Order_API.Data
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database = null;
        private readonly ILogger<MongoDBContext> _logger;
        private readonly string _pathToCAFile;

        public MongoDBContext(IOptions<MongoDBSettings> settings, ILogger<MongoDBContext> logger)
        {
            _logger = logger;

            var connectionString = settings?.Value?.ConnectionString;
            var databaseName = settings?.Value?.DatabaseName;
            _pathToCAFile = settings?.Value?.PathToCAFile;

            _logger.LogInformation("MongoDB connection string from settings: {ConnectionString}", connectionString);
            _logger.LogInformation("MongoDB database name from settings: {DatabaseName}", databaseName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("MongoDB connection string is null or empty.");
                throw new ArgumentNullException(nameof(settings), "MongoDB connection string cannot be null.");
            }

            try
            {
                // ADD CA certificate to local trust store
                X509Store localTrustStore = new X509Store(StoreName.Root);
                X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
                certificateCollection.Import(_pathToCAFile);
                localTrustStore.Open(OpenFlags.ReadWrite);
                localTrustStore.AddRange(certificateCollection);
                localTrustStore.Close();

                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(databaseName);
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
        public string PathToCAFile { get; set; } // Add this property
    }



}
