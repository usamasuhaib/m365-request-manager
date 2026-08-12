using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using RequestManager.Functions.Middleware;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddSingleton<RequestManager.Functions.Infrastructure.ISharePointRepository, RequestManager.Functions.Infrastructure.GraphSharePointRepository>();
        services.AddScoped<RequestManager.Functions.Services.IRequestService, RequestManager.Functions.Services.RequestService>();
    })
    .Build();

host.Run();
