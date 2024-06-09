using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerilogHandler
{
    public class CustomSerilogConfigurator
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly string _connectionString;
        private readonly string _query;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

        public CustomSerilogConfigurator(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            _connectionString = configuration.GetConnectionString(configuration["SerilogConfig:DatabaseSettings:ConnectionStringName"]); 
            _query = configuration["SerilogConfig:DatabaseSettings:Query"];
        }

        public async Task InitializeAsync()
        {
            var configJson = await GetCachedSerilogConfigAsync();

            if (!string.IsNullOrEmpty(configJson))
            {
                // Replace the placeholder with the actual connection string
                var serilogConnectionString = _configuration.GetConnectionString("SerilogConnectionString");
                configJson = configJson.Replace("{SerilogConnectionString}", serilogConnectionString);

                var configuration = new ConfigurationBuilder()
                    .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson)))
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }

            Log.Information("Serilog is configured.");
        }

        private async Task<string> GetCachedSerilogConfigAsync()
        {
            if (!_cache.TryGetValue("SerilogConfig", out string configJson))
            {
                configJson = await FetchSerilogConfigFromDatabaseAsync();

                if (!string.IsNullOrEmpty(configJson))
                {
                    _cache.Set("SerilogConfig", configJson, _cacheDuration);
                }
            }

            return configJson;
        }

        private async Task<string> FetchSerilogConfigFromDatabaseAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(_query, connection);
            var result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }
    }
}
