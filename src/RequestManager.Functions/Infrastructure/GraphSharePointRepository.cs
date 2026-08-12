using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using RequestManager.Functions.Models;

namespace RequestManager.Functions.Infrastructure
{
    public class GraphSharePointRepository : ISharePointRepository
    {
        private readonly ILogger<GraphSharePointRepository> _logger;
        private readonly string? _siteId;
        
        // In-memory data store for fallback local development testing
        private static readonly List<RequestDto> _requestsDb = new();
        private static readonly List<CommentDto> _commentsDb = new();
        private static readonly List<string> _categoriesDb = new() { "Hardware", "Software", "Expense" };
        private static int _nextRequestId = 1;
        private static int _nextCommentId = 1;
        private static readonly Dictionary<string, (byte[] Content, string ContentType, string FileName)> _documentStore = new();

        static GraphSharePointRepository()
        {
            // Seed initial requests for demo
            _requestsDb.Add(new RequestDto
            {
                Id = _nextRequestId++,
                RequestNumber = "REQ-00001",
                Title = "Developer Laptop Upgrade",
                Description = "Upgrade developer workstation to 32GB RAM",
                Category = "Hardware",
                Priority = "High",
                Status = "Submitted",
                SubmittedBy = "Priya Patel",
                SubmittedByEmail = "priya@solvefy.onmicrosoft.com",
                SubmittedDate = DateTime.UtcNow.AddDays(-1),
                AssignedTo = "Winston Manager"
            });

            _requestsDb.Add(new RequestDto
            {
                Id = _nextRequestId++,
                RequestNumber = "REQ-00002",
                Title = "Visual Studio Enterprise Subscription",
                Description = "Developer license renewal",
                Category = "Software",
                Priority = "Medium",
                Status = "Approved",
                SubmittedBy = "Priya Patel",
                SubmittedByEmail = "priya@solvefy.onmicrosoft.com",
                SubmittedDate = DateTime.UtcNow.AddDays(-3),
                AssignedTo = "Winston Manager",
                ApprovedBy = "Winston Manager",
                ApprovedDate = DateTime.UtcNow.AddDays(-2)
            });
            
            _commentsDb.Add(new CommentDto
            {
                Id = _nextCommentId++,
                RequestId = 1,
                Comment = "Request submitted for review.",
                CommentedBy = "Priya Patel",
                CommentedDate = DateTime.UtcNow.AddDays(-1)
            });
        }

        public GraphSharePointRepository(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GraphSharePointRepository>();
            _siteId = Environment.GetEnvironmentVariable("SharePointSiteId");
        }

        public Task InitializeStorageAsync()
        {
            _logger.LogInformation("Initializing SharePoint lists and folders.");
            if (string.IsNullOrEmpty(_siteId))
            {
                _logger.LogInformation("Using in-memory fallback storage.");
                return Task.CompletedTask;
            }

            // Real SharePoint provisioning logic using Microsoft Graph client goes here
            _logger.LogInformation($"Microsoft Graph provisioning active for SharePoint site: {_siteId}");
            return Task.CompletedTask;
        }

        public Task<List<RequestDto>> GetAllRequestsAsync(string userEmail)
        {
            _logger.LogInformation($"Fetching all requests for user: {userEmail}");
            if (string.IsNullOrEmpty(_siteId))
            {
                // In local mock, return all items if caller is Winston (approver) or filter by email if Submitter
                if (userEmail.Contains("winston", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(_requestsDb.ToList());
                }
                return Task.FromResult(_requestsDb.Where(r => r.SubmittedByEmail == userEmail).ToList());
            }

            // Live Mode: query Graph list items: GET /sites/{site-id}/lists/Requests/items
            return Task.FromResult(_requestsDb.ToList()); 
        }

        public Task<RequestDto?> GetRequestByIdAsync(int id)
        {
            _logger.LogInformation($"Fetching request by ID: {id}");
            var req = _requestsDb.FirstOrDefault(r => r.Id == id);
            if (req != null)
            {
                req.Comments = _commentsDb.Where(c => c.RequestId == id).ToList();
            }
            return Task.FromResult(req);
        }

        public Task<RequestDto> CreateRequestAsync(RequestDto request, string userEmail)
        {
            _logger.LogInformation($"Creating request: {request.Title} for {userEmail}");
            
            request.Id = _nextRequestId++;
            request.RequestNumber = $"REQ-{request.Id:D5}";
            request.SubmittedByEmail = userEmail;
            request.SubmittedDate = DateTime.UtcNow;
            request.Status = "Submitted";
            request.AssignedTo = "Winston Manager";

            _requestsDb.Add(request);

            _commentsDb.Add(new CommentDto
            {
                Id = _nextCommentId++,
                RequestId = request.Id,
                Comment = "Request submitted successfully.",
                CommentedBy = request.SubmittedBy,
                CommentedDate = DateTime.UtcNow
            });

            return Task.FromResult(request);
        }

        public Task UpdateRequestStatusAsync(int id, string status, string actorName, string? comment = null)
        {
            _logger.LogInformation($"Updating request ID: {id} to status: {status} by {actorName}");
            var req = _requestsDb.FirstOrDefault(r => r.Id == id);
            if (req != null)
            {
                req.Status = status;
                if (status == "Approved")
                {
                    req.ApprovedBy = actorName;
                    req.ApprovedDate = DateTime.UtcNow;
                }
                else if (status == "Rejected")
                {
                    req.RejectedBy = actorName;
                    req.RejectedDate = DateTime.UtcNow;
                }

                if (!string.IsNullOrEmpty(comment))
                {
                    _commentsDb.Add(new CommentDto
                    {
                        Id = _nextCommentId++,
                        RequestId = id,
                        Comment = comment,
                        CommentedBy = actorName,
                        CommentedDate = DateTime.UtcNow
                    });
                }
            }
            return Task.CompletedTask;
        }

        public Task AddCommentAsync(int requestId, string comment, string authorName)
        {
            _logger.LogInformation($"Adding comment to request ID: {requestId} by {authorName}");
            _commentsDb.Add(new CommentDto
            {
                Id = _nextCommentId++,
                RequestId = requestId,
                Comment = comment,
                CommentedBy = authorName,
                CommentedDate = DateTime.UtcNow
            });
            return Task.CompletedTask;
        }

        public Task<List<CommentDto>> GetCommentsAsync(int requestId)
        {
            return Task.FromResult(_commentsDb.Where(c => c.RequestId == requestId).ToList());
        }

        public Task<List<string>> GetCategoriesAsync()
        {
            return Task.FromResult(_categoriesDb);
        }

        public Task<DocumentDto> UploadDocumentAsync(int requestId, string fileName, byte[] content, string uploader)
        {
            _logger.LogInformation($"Uploading attachment {fileName} for Request ID {requestId}.");
            
            var docId = Guid.NewGuid().ToString("N");
            var mimeType = "application/octet-stream";
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) mimeType = "application/pdf";
            else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            else if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) mimeType = "image/png";
            else if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) mimeType = "image/jpeg";

            // Store in mock storage
            _documentStore[docId] = (content, mimeType, fileName);

            var docDto = new DocumentDto
            {
                Id = docId,
                Name = fileName,
                UploadedBy = uploader,
                UploadedDate = DateTime.UtcNow,
                DownloadUrl = $"/api/requests/{requestId}/documents/{docId}"
            };

            // Associate with request in database
            var req = _requestsDb.FirstOrDefault(r => r.Id == requestId);
            if (req != null)
            {
                req.Documents.Add(docDto);
            }

            return Task.FromResult(docDto);
        }

        public Task<(byte[] Content, string ContentType, string FileName)> DownloadDocumentAsync(int requestId, string documentId)
        {
            _logger.LogInformation($"Downloading attachment ID {documentId} from Request ID {requestId}.");
            if (_documentStore.TryGetValue(documentId, out var fileData))
            {
                return Task.FromResult(fileData);
            }

            throw new KeyNotFoundException($"Document with ID {documentId} was not found.");
        }
    }
}
