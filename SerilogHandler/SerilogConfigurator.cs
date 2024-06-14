using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace SerilogHandler
{
    public class SerilogConfigurator:ISerilogConfigurator
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private Timer _timer;
        private readonly ISerilogConfigCacheService _serilogConfigCacheService;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1); // SemaphoreSlim for cache access

        public SerilogConfigurator(IConfiguration configuration, 
                                   IMemoryCache cache,
                                   ISerilogConfigCacheService serilogConfigCacheService)
        {
            _configuration = configuration;
            _cache = cache;
            var serilogConfigSection = _configuration.GetSection("SerilogConfig");
            _connectionString = configuration.GetConnectionString(configuration["SerilogConfig:DatabaseSettings:ConnectionStringName"]);
            _serilogConfigCacheService = serilogConfigCacheService;
        }

        public async Task InitializeAsync()
        {
            await ConfigureSerilog();
            //_timer = new Timer(async _ => await UpdateCache(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            //ConfigureSerilog();
        }

        //private async Task<string> FetchSerilogConfigFromDatabase()
        //{
        //    using var connection = new SqlConnection(_connectionString);
        //    await connection.OpenAsync();
        //    var command = new SqlCommand(_query, connection);
        //    return (string)await command.ExecuteScalarAsync();
        //}

        //private async Task UpdateCache()
        //{
        //    var serilogConfigJson = await FetchSerilogConfigFromDatabase();
        //    _cache.Set("SerilogConfig", serilogConfigJson);
        //}

        private async Task ConfigureSerilog()
        {
            var serilogConfigJson = await GetSerilogConfigAsync();

            if (!string.IsNullOrEmpty(serilogConfigJson))
            {
                serilogConfigJson = serilogConfigJson.Replace("{SerilogConnectionString}", _connectionString);
                var configuration = new ConfigurationBuilder()
                    .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serilogConfigJson)))
                    .Build();

                var columnOptions = new ColumnOptions();
                columnOptions.Store.Remove(StandardColumn.Properties);
                columnOptions.Store.Add(StandardColumn.LogEvent);

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration.GetSection("Serilog"))
                    .WriteTo.MSSqlServer(
                        connectionString: _connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "Logs",
                            AutoCreateSqlTable = true,
                            SchemaName = "dbo"
                        },
                        columnOptions: new ColumnOptions
                        {
                            AdditionalColumns = new Collection<SqlColumn> // Fix the using statement and add the generic type argument
                            {
                                    new SqlColumn { ColumnName = "LogEvent", DataType = SqlDbType.NVarChar, DataLength = -1 }
                            }
                        })
                    .CreateLogger();

            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }


        private async Task<string> GetSerilogConfigAsync()
        {
            await _cacheLock.WaitAsync(); // Wait asynchronously to acquire the semaphore
            try
            {
                return await _cache.GetOrCreateAsync("SerilogConfig", async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromSeconds(30);
                    //var serilogConfigCacheService = new SerilogConfigCacheService(_configuration, _cache);
                    return await _serilogConfigCacheService.GetSerilogConfigAsync();
                });
            }
            finally
            {
                _cacheLock.Release(); // Release the semaphore
            }
        }

    }
}
