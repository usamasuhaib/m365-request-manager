using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RequestManager.Functions.Models;
using RequestManager.Functions.Services;
using RequestManager.Functions.Infrastructure;

namespace RequestManager.Functions.Functions
{
    public class RequestsFunctions
    {
        private readonly ILogger<RequestsFunctions> _logger;
        private readonly IRequestService _requestService;
        private readonly ISharePointRepository _repository;

        // Idempotency cache (AD-4: Cache request outcomes for 5 minutes)
        private static readonly ConcurrentDictionary<string, (RequestDto Request, DateTime Timestamp)> _idempotencyCache = new();

        public RequestsFunctions(ILoggerFactory loggerFactory, IRequestService requestService, ISharePointRepository repository)
        {
            _logger = loggerFactory.CreateLogger<RequestsFunctions>();
            _requestService = requestService;
            _repository = repository;
        }

        [Function("GetRequests")]
        public async Task<HttpResponseData> GetRequests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Fetching user dashboard requests.");
            var email = context.Items["UserEmail"] as string ?? "priya@solvefy.onmicrosoft.com";

            var requests = await _requestService.GetUserDashboardRequestsAsync(email);
            
            // Calculate metrics for dashboard view
            var total = requests.Count;
            var pending = requests.Count(r => r.Status == "Submitted" || r.Status == "Pending Approval");
            var approved = requests.Count(r => r.Status == "Approved");
            var rejected = requests.Count(r => r.Status == "Rejected");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                metrics = new { total, pending, approved, rejected },
                data = requests
            });

            return response;
        }

        [Function("GetRequestById")]
        public async Task<HttpResponseData> GetRequestById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{id:int}")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            _logger.LogInformation($"Fetching request details for ID: {id}");
            var email = context.Items["UserEmail"] as string ?? "priya@solvefy.onmicrosoft.com";

            var request = await _requestService.GetRequestDetailsAsync(id, email);
            if (request == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { success = false, message = "Request not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, data = request });
            return response;
        }

        [Function("CreateRequest")]
        public async Task<HttpResponseData> CreateRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Processing create request.");

            // AD-4: Read Idempotency header
            if (!req.Headers.TryGetValues("Client-Request-Id", out var requestIds) || string.IsNullOrEmpty(requestIds.FirstOrDefault()))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = "Client-Request-Id header is required for idempotency protection." });
                return badResponse;
            }

            var requestId = requestIds.First();

            // Check Cache
            CleanIdempotencyCache();
            if (_idempotencyCache.TryGetValue(requestId, out var cachedValue))
            {
                _logger.LogInformation($"Duplicate request detected for key: {requestId}. Returning cached response.");
                var cachedResponse = req.CreateResponse(HttpStatusCode.OK);
                await cachedResponse.WriteAsJsonAsync(new { success = true, isDuplicate = true, data = cachedValue.Request });
                return cachedResponse;
            }

            var email = context.Items["UserEmail"] as string ?? "priya@solvefy.onmicrosoft.com";
            var name = context.Items["UserName"] as string ?? "Priya Patel";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<CreateRequestDto>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = "Invalid request payload." });
                return badResponse;
            }

            try
            {
                var createdRequest = await _requestService.CreateRequestAsync(dto, email, name);

                // Add to Cache
                _idempotencyCache.TryAdd(requestId, (createdRequest, DateTime.UtcNow));

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(new { success = true, data = createdRequest });
                return response;
            }
            catch (ArgumentException ex)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return badResponse;
            }
        }

        [Function("AddComment")]
        public async Task<HttpResponseData> AddComment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{id:int}/comment")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            var name = context.Items["UserName"] as string ?? "Priya Patel";
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            var payload = JsonSerializer.Deserialize<JsonElement>(requestBody);
            if (!payload.TryGetProperty("comment", out var commentProp) || string.IsNullOrWhiteSpace(commentProp.GetString()))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { success = false, message = "Comment is required." });
                return badResponse;
            }

            try
            {
                await _requestService.AddRequestCommentAsync(id, commentProp.GetString()!, name);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { success = true, message = "Comment added." });
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return errorResponse;
            }
        }

        [Function("ApproveRequest")]
        public async Task<HttpResponseData> ApproveRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{id:int}/approve")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            var email = context.Items["UserEmail"] as string ?? "winston@solvefy.onmicrosoft.com";
            var name = context.Items["UserName"] as string ?? "Winston Manager";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<JsonElement>(requestBody);
            var comment = payload.TryGetProperty("comment", out var commentProp) ? commentProp.GetString() ?? "" : "";

            try
            {
                var result = await _requestService.ApproveRequestAsync(id, comment, email, name);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { success = result, message = result ? "Request approved successfully." : "Failed to approve request." });
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return errorResponse;
            }
        }

        [Function("RejectRequest")]
        public async Task<HttpResponseData> RejectRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{id:int}/reject")] HttpRequestData req,
            int id,
            FunctionContext context)
        {
            var email = context.Items["UserEmail"] as string ?? "winston@solvefy.onmicrosoft.com";
            var name = context.Items["UserName"] as string ?? "Winston Manager";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<JsonElement>(requestBody);
            var comment = payload.TryGetProperty("comment", out var commentProp) ? commentProp.GetString() ?? "" : "";

            try
            {
                var result = await _requestService.RejectRequestAsync(id, comment, email, name);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { success = result, message = result ? "Request rejected successfully." : "Failed to reject request." });
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return errorResponse;
            }
        }

        [Function("ProvisionStorage")]
        public async Task<HttpResponseData> ProvisionStorage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "setup")] HttpRequestData req)
        {
            _logger.LogInformation("Triggering storage list setups.");
            await _repository.InitializeStorageAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, message = "SharePoint schema storage lists provisioned successfully." });
            return response;
        }

        private static void CleanIdempotencyCache()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _idempotencyCache)
            {
                if ((now - kvp.Value.Timestamp).TotalMinutes > 5)
                {
                    _idempotencyCache.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
