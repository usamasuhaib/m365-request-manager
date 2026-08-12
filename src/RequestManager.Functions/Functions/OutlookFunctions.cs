using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RequestManager.Functions.Models;
using RequestManager.Functions.Services;

namespace RequestManager.Functions.Functions
{
    public class OutlookFunctions
    {
        private readonly ILogger<OutlookFunctions> _logger;
        private readonly IRequestService _requestService;

        public OutlookFunctions(ILoggerFactory loggerFactory, IRequestService requestService)
        {
            _logger = loggerFactory.CreateLogger<OutlookFunctions>();
            _requestService = requestService;
        }

        [Function("CreateRequestFromEmail")]
        public async Task<HttpResponseData> CreateRequestFromEmail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "outlook/create-request")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Processing create request from Outlook email context.");

            var email = context.Items["UserEmail"] as string ?? "priya@solvefy.onmicrosoft.com";
            var name = context.Items["UserName"] as string ?? "Priya Patel";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<EmailRequestPayload>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || string.IsNullOrEmpty(payload.Subject))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = "Email Subject is required." });
                return badResponse;
            }

            try
            {
                // Pre-fill creation data
                var createDto = new CreateRequestDto
                {
                    Title = payload.Subject.Length > 100 ? payload.Subject.Substring(0, 97) + "..." : payload.Subject,
                    Description = $"[Ingested from Outlook email]\nFrom: {payload.Sender}\n\n{payload.BodySnippet}",
                    Category = payload.Category ?? "Software",
                    Priority = payload.Priority ?? "Medium"
                };

                var createdRequest = await _requestService.CreateRequestAsync(createDto, email, name);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { success = true, data = createdRequest });
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return errorResponse;
            }
        }

        private class EmailRequestPayload
        {
            public string Subject { get; set; } = string.Empty;
            public string Sender { get; set; } = string.Empty;
            public string BodySnippet { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
        }
    }
}
