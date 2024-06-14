using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SerilogHandler
{
    public class SerilogConfigBackgroundService : BackgroundService
    {
        private readonly ISerilogConfigCacheService _serilogConfigCacheService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SerilogConfigBackgroundService> _logger;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public SerilogConfigBackgroundService(ISerilogConfigCacheService serilogConfigCacheService,
                                              IMemoryCache cache, 
                                              ILogger<SerilogConfigBackgroundService> logger)
        {
            _serilogConfigCacheService = serilogConfigCacheService;
            _cache = cache;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Refreshing Serilog configuration from database.");

                await UpdateCache();

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait for 30 seconds before next refresh
            }
        }
        private async Task UpdateCache()
        {
            await _cacheLock.WaitAsync();

            try
            {
                var serilogConfigJson = await _serilogConfigCacheService.GetSerilogConfigAsync();

                if (!string.IsNullOrEmpty(serilogConfigJson))
                {
                    _cache.Set("SerilogConfig", serilogConfigJson, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromSeconds(30) // Set sliding expiration for cache
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Serilog configuration cache.");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
