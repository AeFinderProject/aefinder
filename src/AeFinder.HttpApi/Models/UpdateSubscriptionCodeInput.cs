using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Models;

public class UpdateSubscriptionCodeInput
{
    public IFormFile Code { get; set; }
    public string AttachmentDeleteFileKeyList { get; set; }
    public List<IFormFile> AttachmentList { get; set; }
}