using System.ComponentModel.DataAnnotations;

namespace AeFinder.User.Dto;

public class ResendEmailInput
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; }
}