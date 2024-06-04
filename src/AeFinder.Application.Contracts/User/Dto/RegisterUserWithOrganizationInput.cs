using System.ComponentModel.DataAnnotations;

namespace AeFinder.User.Dto;

public class RegisterUserWithOrganizationInput
{
    [Required]
    [MaxLength(50, ErrorMessage = "UserName cannot exceed 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]*$", ErrorMessage = "UserName can only contain letters, digits, hyphens, and underscores.")]
    public string UserName { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long and not exceed 50 characters.")]
    public string Password { get; set; }
    
    public string Email { get; set; }
    
    public string OrganizationUnitId { get; set; }
}