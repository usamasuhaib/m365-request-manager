using System.Collections.Generic;
using System.Threading.Tasks;
using RequestManager.Functions.Models;

namespace RequestManager.Functions.Infrastructure
{
    public interface ISharePointRepository
    {
        Task InitializeStorageAsync();
        Task<List<RequestDto>> GetAllRequestsAsync(string userEmail);
        Task<RequestDto?> GetRequestByIdAsync(int id);
        Task<RequestDto> CreateRequestAsync(RequestDto request, string userEmail);
        Task UpdateRequestStatusAsync(int id, string status, string actorName, string? comment = null);
        Task AddCommentAsync(int requestId, string comment, string authorName);
        Task<List<CommentDto>> GetCommentsAsync(int requestId);
        Task<List<string>> GetCategoriesAsync();
        Task<DocumentDto> UploadDocumentAsync(int requestId, string fileName, byte[] content, string uploader);
        Task<(byte[] Content, string ContentType, string FileName)> DownloadDocumentAsync(int requestId, string documentId);
    }
}
