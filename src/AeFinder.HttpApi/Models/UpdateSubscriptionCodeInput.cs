using Microsoft.AspNetCore.Http;

namespace AeFinder.Models;

public class UpdateSubscriptionCodeInput
{
    public IFormFile Code { get; set; }
}