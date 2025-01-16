using System;
using Orleans;

namespace AeFinder.User.Dto;

[GenerateSerializer]
public class UserRegisterInfo
{
    [Id(0)]public Guid UserId { get; set; }
    [Id(1)]public string OrganizationName { get; set; }
}