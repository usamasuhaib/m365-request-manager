using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace RequestManager.Functions.Functions
{
    public class HealthFunction
    {
        private readonly ILogger _logger;

        public HealthFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HealthFunction>();
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Processing health check request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { status = "healthy" });

            return response;
        }
    }
}
