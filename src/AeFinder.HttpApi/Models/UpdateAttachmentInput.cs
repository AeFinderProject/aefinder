using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.Models;

public class UpdateAttachmentInput
{
    public string AttachmentDeleteFileKeyList { get; set; }
    
    public List<IFormFile> AttachmentList { get; set; }
    // public IFormFile Attachment1 { get; set; }
    // public IFormFile Attachment2 { get; set; }
    // public IFormFile Attachment3 { get; set; }
    // public IFormFile Attachment4 { get; set; }
    // public IFormFile Attachment5 { get; set; }
}