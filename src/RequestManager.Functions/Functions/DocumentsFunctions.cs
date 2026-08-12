using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RequestManager.Functions.Services;

namespace RequestManager.Functions.Functions
{
    public class DocumentsFunctions
    {
        private readonly ILogger<DocumentsFunctions> _logger;
        private readonly IRequestService _requestService;

        public DocumentsFunctions(ILoggerFactory loggerFactory, IRequestService requestService)
        {
            _logger = loggerFactory.CreateLogger<DocumentsFunctions>();
            _requestService = requestService;
        }

        [Function("DownloadDocumentProxy")]
        public async Task<HttpResponseData> DownloadDocumentProxy(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId:int}/documents/{documentId}")] HttpRequestData req,
            int requestId,
            string documentId,
            FunctionContext context)
        {
            _logger.LogInformation($"Proxy download triggered for Request {requestId}, Doc {documentId}");

            var email = context.Items["UserEmail"] as string ?? "priya@solvefy.onmicrosoft.com";

            try
            {
                var (content, contentType, fileName) = await _requestService.DownloadRequestDocumentAsync(requestId, documentId, email);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", contentType);
                response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                
                await response.Body.WriteAsync(content, 0, content.Length);
                return response;
            }
            catch (KeyNotFoundException ex)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return notFoundResponse;
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = ex.Message });
                return errorResponse;
            }
        }
    }
}
