using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SerilogHandler
{
    public static class HostBuilderExtensions
    {
        //public static async Task UseSerilogWithConfigurationAsync(this IHostBuilder builder)
        //{
        //    var serilogConfigurator = builder.Services.GetRequiredService<ISerilogConfigurator>();
        //    //var serilogConfigurator = host.Build.GetRequiredService<ISerilogConfigurator>();
        //    await serilogConfigurator.InitializeAsync();
        //    builder.Host.UseSerilog();
        //}

        public static async Task<WebApplicationBuilder> UseCustomSerilogAsync(this WebApplicationBuilder builder)
        {
            // Build the service provider to access the service
            builder.Services.AddSerilogConfiguration();
            var serviceProvider = builder.Services.BuildServiceProvider();

            // Resolve the ISerilogConfigurator service
            var serilogConfigurator = serviceProvider.GetRequiredService<ISerilogConfigurator>();

            // Initialize Serilog
            await serilogConfigurator.InitializeAsync();

            // Use Serilog
            builder.Host.UseSerilog();

            return builder;
        }
    }
}
