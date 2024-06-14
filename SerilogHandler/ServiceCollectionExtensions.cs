using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerilogHandler
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSerilogConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<ISerilogConfigCacheService, SerilogConfigCacheService>();
            services.AddSingleton<ISerilogConfigurator, SerilogConfigurator>();
            services.AddHostedService<SerilogConfigBackgroundService>();
            // services.AddHostedService<SerilogConfigBackgroundService>();
            return services;
        }
    }
}
