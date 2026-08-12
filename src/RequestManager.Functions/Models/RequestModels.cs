using System;
using System.Collections.Generic;

namespace RequestManager.Functions.Models
{
    public class RequestDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public string SubmittedByEmail { get; set; } = string.Empty;
        public DateTime? SubmittedDate { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedDate { get; set; }
        public string RejectedBy { get; set; } = string.Empty;
        public DateTime? RejectedDate { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
        public List<DocumentDto> Documents { get; set; } = new();
    }

    public class CreateRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? AttachmentName { get; set; }
        public string? AttachmentContent { get; set; } // Base64 string
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CommentedBy { get; set; } = string.Empty;
        public DateTime CommentedDate { get; set; }
    }

    public class ApprovalDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string Approver { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
    }

    public class DocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
