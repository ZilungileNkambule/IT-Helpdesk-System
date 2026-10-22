
using System.ComponentModel.DataAnnotations;

namespace ITHelpdeskAPI.Models;

public class ProfileViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class ChangeProfilePasswordViewModel
{
    [Required]
    public string CurrentPassword { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = "";

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}
