using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace SerilogHandler
{
    public class SerilogConfigCacheService : ISerilogConfigCacheService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly string _connectionString;
        private readonly string _query;
        private readonly string _checkUpdateQuery;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1); // SemaphoreSlim for cache access


        public SerilogConfigCacheService(IConfiguration configuration, 
                                         IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            _connectionString = configuration.GetConnectionString(configuration["SerilogConfig:DatabaseSettings:ConnectionStringName"]);
            _query = configuration["SerilogConfig:DatabaseSettings:Query"];
            _checkUpdateQuery = configuration["SerilogConfig:DatabaseSettings:CheckUpdateQuery"];
        }
        public async Task<string> GetSerilogConfigAsync()
        { await _cacheLock.WaitAsync();
            try
            {
                if (!_cache.TryGetValue("SerilogConfig", out string configJson))
                {
                    configJson = await FetchSerilogConfigFromDatabaseAsync();

                    if (!string.IsNullOrEmpty(configJson))
                    {
                        _cache.Set("SerilogConfig", configJson, _cacheDuration);
                    }
                }
                else
                {
                    bool isUpdated = await CheckIfConfigUpdatedAsync();
                    if (isUpdated)
                    {
                        configJson = await FetchSerilogConfigFromDatabaseAsync();
                        _cache.Set("SerilogConfig", configJson, _cacheDuration);
                    }
                }

                return configJson;
            }
            finally
            {
                _cacheLock.Release();
            }
        
        }

        private async Task<string> FetchSerilogConfigFromDatabaseAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(_query, connection);
            var result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }

        private async Task<bool> CheckIfConfigUpdatedAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(_checkUpdateQuery, connection);
            var result = await command.ExecuteScalarAsync();

            return result != null && (int)result > 0;
        }
    }
}
