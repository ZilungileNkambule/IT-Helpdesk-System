using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskAPI.DTOs;

public class UpdateTicketStatusDto
{
    [Required]
    [RegularExpression("Open|In Progress|Resolved|Closed")]
    public string Status { get; set; } = string.Empty;

    public string? ResolutionNotes { get; set; }
}