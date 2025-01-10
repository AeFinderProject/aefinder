using System.ComponentModel.DataAnnotations;

namespace AeFinder.User.Dto;

public class RegisterUserInput
{
    [Required]
    [MaxLength(50, ErrorMessage = "UserName cannot exceed 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]*$", ErrorMessage = "UserName can only contain letters, digits, hyphens, and underscores.")]
    public string UserName { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long and not exceed 50 characters.")]
    public string Password { get; set; }
    
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; }
    
    // [Required]
    // [MaxLength(50, ErrorMessage = "Organization name cannot exceed 50 characters.")]
    // //[RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Organization name can only contain letters and digits.")]
    // public string OrganizationName { get; set; }
}