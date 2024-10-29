using Volo.Abp.Identity;

namespace AeFinder.User.Dto;

public class IdentityUserExtensionDto : IdentityUserDto
{
    public UserExtensionDto userExtensionInfo { get; set; }
}