using Volo.Abp.Identity;

namespace AeFinder.User.Dto;

// public class IdentityUserExtensionDto : IdentityUserDto
public class IdentityUserExtensionDto
{
    public string UserName { get; set; }
    
    public string Email { get; set; }

    public bool EmailConfirmed { get; set; }
    public UserExtensionDto userExtensionInfo { get; set; }
}