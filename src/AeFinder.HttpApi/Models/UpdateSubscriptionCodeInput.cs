using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AeFinder.Models;

public class UpdateSubscriptionCodeInput
{
    public IFormFile Code { get; set; }
    public string AttachmentDeleteFileKeyList { get; set; }
    public List<IFormFile> AttachmentList { get; set; }
    // public IFormFile Attachment1 { get; set; }
    // public IFormFile Attachment2 { get; set; }
    // public IFormFile Attachment3 { get; set; }
    // public IFormFile Attachment4 { get; set; }
    // public IFormFile Attachment5 { get; set; }
}