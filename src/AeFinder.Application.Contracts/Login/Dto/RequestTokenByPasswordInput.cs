using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace AeFinder.Login.Dto;

public class RequestTokenByPasswordInput
{
    /// <summary>
    /// It will be generated randomly if the value is null.
    /// </summary>
    [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxUserNameLength))]
    public string UserName { get; set; }


    [Required]
    [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
    [DisableAuditing]
    public string Password { get; set; }
}