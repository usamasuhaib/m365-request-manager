using System.Collections.Generic;
using System.Threading.Tasks;
using RequestManager.Functions.Models;

namespace RequestManager.Functions.Services
{
    public interface IRequestService
    {
        Task<List<RequestDto>> GetUserDashboardRequestsAsync(string userEmail);
        Task<RequestDto?> GetRequestDetailsAsync(int id, string userEmail);
        Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, string userEmail, string userName);
        Task<bool> ApproveRequestAsync(int id, string comment, string userEmail, string userName);
        Task<bool> RejectRequestAsync(int id, string comment, string userEmail, string userName);
        Task AddRequestCommentAsync(int id, string comment, string userName);
        Task<(byte[] Content, string ContentType, string FileName)> DownloadRequestDocumentAsync(int requestId, string documentId, string userEmail);
    }
}
