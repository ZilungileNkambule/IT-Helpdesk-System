
namespace ITHelpdeskAPI.Models
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string? ReferenceNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public Guid CreatedBy { get; set; }
        public Guid? AssignedTo { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
