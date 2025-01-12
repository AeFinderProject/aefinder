using System.Threading.Tasks;
using AeFinder.Email;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Email")]
[Route("api/email")]
public class EmailController: AeFinderController
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    [HttpPost]
    [Route("test")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task TestEmailAsync(string email, string content)
    {
        await _emailService.SendEmailTest(email, content);
    }
}