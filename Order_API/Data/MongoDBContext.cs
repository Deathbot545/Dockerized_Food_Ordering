﻿using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Order_API.Models;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
            var databaseName = settings?.Value?.DatabaseName;


            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(settings), "MongoDB connection string cannot be null.");
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(settings), "MongoDB database name cannot be null.");
            }

            try
            {
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
                clientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);

                var client = new MongoClient(clientSettings);
                _database = client.GetDatabase(databaseName);
            }
            catch (Exception ex)
            {
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
