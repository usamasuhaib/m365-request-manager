using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RequestManager.Functions.Middleware;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<RequestManager.Functions.Infrastructure.ISharePointRepository, RequestManager.Functions.Infrastructure.GraphSharePointRepository>();
        services.AddScoped<RequestManager.Functions.Services.IRequestService, RequestManager.Functions.Services.RequestService>();
    })
    .Build();

host.Run();
