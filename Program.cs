using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseKestrel();

        // ✅ Configure Kestrel to listen on a specific port
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.ListenLocalhost(5216); // Binding to http://localhost:5216
        });

        // Set up logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        // Register your startup logic
        builder.Services.AddSingleton<IStartupFilter, StartupFilter>();

        var host = builder.Build();

        // ✅ Print bound addresses (e.g., http://localhost:5216)
        var server = host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses != null)
        {
            foreach (var address in addresses)
            {
                Console.WriteLine($"✅ Listening on: {address}");
            }
        }
        else
        {
            Console.WriteLine("⚠️  No addresses reported (may still be listening).");
        }

        await host.RunAsync();
    }
}

// StartupFilter adds middleware and endpoints similar to a function app
public class StartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    logger.LogInformation("Received GET request: {Time}", DateTime.UtcNow);
                    await context.Response.WriteAsync("Hello from Kestrel console app!");
                });

                endpoints.MapPost("/echo", async context =>
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    logger.LogInformation("Received POST body: {Body}", body);
                    await context.Response.WriteAsync($"Echo: {body}");
                });
            });

            next(app);
        };
    }
}