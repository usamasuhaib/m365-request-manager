using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace RequestManager.Functions.Functions
{
    public class UserFunctions
    {
        private readonly ILogger<UserFunctions> _logger;

        public UserFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UserFunctions>();
        }

        [Function("GetMe")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Processing profile fetch request.");

            var email = context.Items["UserEmail"] as string ?? "unknown@solvefy.onmicrosoft.com";
            var name = context.Items["UserName"] as string ?? "Unknown User";

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                data = new { name, email }
            });

            return response;
        }
    }
}
