namespace AeFinder.User.Dto;

public class RegisterUserWithOrganizationInput
{
    public string UserName { get; set; }
    
    public string Password { get; set; }
    
    public string Email { get; set; }
    
    public string OrganizationUnitId { get; set; }
}