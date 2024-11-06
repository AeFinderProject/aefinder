using System;
using System.Collections.Generic;

namespace AeFinder.User.Dto;

public class UserExtensionDto
{
    public Guid UserId { get; set; }
    /// <summary>
    /// EOA Address or CA Address
    /// </summary>
    public string WalletAddress { get; set; }
}