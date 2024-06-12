using System;
using Volo.Abp.Identity;

namespace AeFinder.User.Dto;

public class RegisterUserWithOrganizationDto: IdentityUserCreateDto
{
    public Guid? OrganizationUnitId { get; set; }
}