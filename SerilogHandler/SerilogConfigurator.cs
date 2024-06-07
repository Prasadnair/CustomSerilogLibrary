using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.MSSqlServer;
using Serilog;
using System.Collections.ObjectModel;
using System.Data;

namespace SerilogHandler
{
    public class SerilogConfigurator
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private Timer _timer;

        public SerilogConfigurator(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;

            var serilogConfigSection = _configuration.GetSection("SerilogConfig");
            _connectionString = serilogConfigSection.GetSection("DatabaseSettings").GetValue<string>("ConnectionString");
            _query = serilogConfigSection.GetSection("DatabaseSettings").GetValue<string>("Query");
        }

        public async Task InitializeAsync()
        {
            await UpdateCache();
            _timer = new Timer(async _ => await UpdateCache(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            ConfigureSerilog();
        }

        private async Task<string> FetchSerilogConfigFromDatabase()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = new SqlCommand(_query, connection);
            return (string)await command.ExecuteScalarAsync();
        }

        private async Task UpdateCache()
        {
            var serilogConfigJson = await FetchSerilogConfigFromDatabase();
            _cache.Set("SerilogConfig", serilogConfigJson);
        }

        private void ConfigureSerilog()
        {
            if (_cache.TryGetValue("SerilogConfig", out string cachedConfigJson))
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cachedConfigJson)))
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

    }
}
