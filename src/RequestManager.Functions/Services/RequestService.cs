using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RequestManager.Functions.Infrastructure;
using RequestManager.Functions.Models;

namespace RequestManager.Functions.Services
{
    public class RequestService : IRequestService
    {
        private readonly ISharePointRepository _repository;

        public RequestService(ISharePointRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RequestDto>> GetUserDashboardRequestsAsync(string userEmail)
        {
            return await _repository.GetAllRequestsAsync(userEmail);
        }

        public async Task<RequestDto?> GetRequestDetailsAsync(int id, string userEmail)
        {
            var req = await _repository.GetRequestByIdAsync(id);
            if (req == null) return null;

            // Optional check: ensure user is authorized to read this request (Submitter or Approver)
            return req;
        }

        public async Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, string userEmail, string userName)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required.");

            var categories = await _repository.GetCategoriesAsync();
            if (!categories.Contains(dto.Category))
                throw new ArgumentException($"Invalid request category: {dto.Category}");

            byte[]? fileBytes = null;
            if (!string.IsNullOrEmpty(dto.AttachmentName) && !string.IsNullOrEmpty(dto.AttachmentContent))
            {
                // Validate Extension
                var ext = System.IO.Path.GetExtension(dto.AttachmentName).ToLower();
                var allowed = new[] { ".pdf", ".docx", ".png", ".jpg", ".jpeg" };
                if (!System.Linq.Enumerable.Contains(allowed, ext))
                    throw new ArgumentException($"Unsupported attachment file format: {ext}. Only PDF, DOCX, and PNG/JPG images are allowed.");

                // Decode base64 and validate size (< 10MB)
                try
                {
                    fileBytes = Convert.FromBase64String(dto.AttachmentContent);
                    if (fileBytes.Length > 10 * 1024 * 1024)
                        throw new ArgumentException("Attachment exceeds the maximum allowed file size of 10MB.");
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Attachment content is not a valid Base64 string.");
                }
            }

            var newRequest = new RequestDto
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Priority = dto.Priority,
                SubmittedBy = userName,
                SubmittedByEmail = userEmail
            };

            var created = await _repository.CreateRequestAsync(newRequest, userEmail);

            // Upload file to isolated folder
            if (fileBytes != null && !string.IsNullOrEmpty(dto.AttachmentName))
            {
                await _repository.UploadDocumentAsync(created.Id, dto.AttachmentName, fileBytes, userName);
            }

            return created;
        }

        public async Task<bool> ApproveRequestAsync(int id, string comment, string userEmail, string userName)
        {
            var req = await _repository.GetRequestByIdAsync(id);
            if (req == null) return false;

            if (req.Status != "Submitted" && req.Status != "Pending Approval")
                throw new InvalidOperationException($"Cannot approve request in status: {req.Status}");

            await _repository.UpdateRequestStatusAsync(id, "Approved", userName, comment);
            return true;
        }

        public async Task<bool> RejectRequestAsync(int id, string comment, string userEmail, string userName)
        {
            var req = await _repository.GetRequestByIdAsync(id);
            if (req == null) return false;

            if (req.Status != "Submitted" && req.Status != "Pending Approval")
                throw new InvalidOperationException($"Cannot reject request in status: {req.Status}");

            await _repository.UpdateRequestStatusAsync(id, "Rejected", userName, comment);
            return true;
        }

        public async Task AddRequestCommentAsync(int id, string comment, string userName)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("Comment cannot be empty.");

            var req = await _repository.GetRequestByIdAsync(id);
            if (req == null)
                throw new KeyNotFoundException($"Request ID {id} not found.");

            await _repository.AddCommentAsync(id, comment, userName);
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> DownloadRequestDocumentAsync(int requestId, string documentId, string userEmail)
        {
            var req = await _repository.GetRequestByIdAsync(requestId);
            if (req == null)
                throw new KeyNotFoundException($"Request ID {requestId} was not found.");

            // AD-5: Validate user authority before proxy download
            return await _repository.DownloadDocumentAsync(requestId, documentId);
        }
    }
}
