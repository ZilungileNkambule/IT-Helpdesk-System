
using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskAPI.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public long? CategoryId { get; set; }

        [Required]
        [RegularExpression("Low|Medium|High|Critical")]
        public string Priority { get; set; } = "Medium";
    }
}
