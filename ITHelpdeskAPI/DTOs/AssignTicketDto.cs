using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskAPI.DTOs;

public class AssignTicketDto
{
    [Required]
    public Guid? TechnicianId { get; set; }
}
