using System;
using Orleans;

namespace AeFinder.User.Dto;

[GenerateSerializer]
public class RegisterVerificationCodeInfo
{
    [Id(0)]public string Code { get; set; }
    [Id(1)]public DateTime SendingTime { get; set; }
}