using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.Models;

public class UpdateAttachmentInput
{
    [AllowNull] public IFormFile Attachment1 { get; set; }
    [AllowNull] public IFormFile Attachment2 { get; set; }
    [AllowNull] public IFormFile Attachment3 { get; set; }
    [AllowNull] public IFormFile Attachment4 { get; set; }
    [AllowNull] public IFormFile Attachment5 { get; set; }
    public string AttachmentDeleteFileKeyList { get; set; }
}