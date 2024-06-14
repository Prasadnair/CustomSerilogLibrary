namespace SerilogHandler
{
    public interface ISerilogConfigCacheService
    {
        Task<string> GetSerilogConfigAsync();
    }
}
