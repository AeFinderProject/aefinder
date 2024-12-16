using System.ComponentModel.DataAnnotations;

namespace AeFinder.User.Dto;

public class CreateOrganizationUnitInput
{
    [Required]
    [MaxLength(50, ErrorMessage = "DisplayName cannot exceed 50 characters.")]
    //[RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "DisplayName can only contain letters and digits.")]
    public string DisplayName { get; set; }
}