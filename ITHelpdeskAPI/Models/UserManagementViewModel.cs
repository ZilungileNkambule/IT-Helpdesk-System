
namespace ITHelpdeskAPI.Models;

public class UserManagementViewModel
{
    public List<ManagedUser> Users { get; set; } = new();
}

public class ManagedUser
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Role { get; set; } = "";

    public bool IsActive { get; set; }
}

public class CreateManagedUserRequest
{
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    public string Role { get; set; } = "staff";
}
