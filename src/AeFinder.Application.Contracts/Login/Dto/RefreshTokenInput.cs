using System;
using System.ComponentModel.DataAnnotations;

namespace EAeFinder.Login.Dto;

public class RefreshTokenInput
{
    [Required] public string RefreshToken { get; set; }
}